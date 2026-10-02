using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Penghou.IO.Abstractions;

namespace Penghou.IO.Local;

/// <summary>Trusted host ceilings for conditional writes and their verification read.</summary>
public sealed record LocalPatchOptions(int MaxOriginalBytes = 16 * 1024 * 1024,
    int MaxReadBytes = 32 * 1024 * 1024, int TimeoutMilliseconds = 30_000,
    LocalPatchNamespace Namespace = LocalPatchNamespace.Unspecified);

/// <summary>The trusted host's alias and directory-mutation profile for a local patch operation.</summary>
public enum LocalPatchNamespace { Unspecified, HostControlled }

/// <summary>
/// Conditionally persists bounded bytes through one exact, single-link NTFS locked file handle.
/// This provider does not implement whole-workspace transactions or confinement
/// against privileged processes.
/// </summary>
public sealed class LocalWorkspaceWriter : IWorkspaceConditionalWriter
{
    public const string ProviderProfile = "local-windows-ntfs-controlled-write-v1";

    private const int MaxFileBytes = 16 * 1024 * 1024;
    private const int MaxOutputBytes = 16 * 1024 * 1024;
    private const int MaxTextTokenCharacters = 256;
    private const int MaxEvidenceCharacters = 256;
    private const int MutationCompletionTimeoutMilliseconds = 5000;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileReadAttributes = 0x00000080;
    private const uint ShareRead = 0x00000001;
    private const uint OpenExisting = 3;
    private const uint OpenReparsePoint = 0x00200000;
    private const uint BackupSemantics = 0x02000000;
    private const uint Overlapped = 0x40000000;
    private const uint WriteThrough = 0x80000000;
    private const uint FileAttributeDirectory = 0x00000010;
    private const uint FileAttributeReparsePoint = 0x00000400;
    private const int FileAttributeTagInfoClass = 9;
    private const int FileIdInfoClass = 18;
    private const int FileCaseSensitiveInfoClass = 23;
    private const uint DriveFixed = 3;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly WorkspaceId _workspace;
    private readonly string _root;
    private readonly string _driveRoot;
    private readonly IResourceAuthorizer _authorizer;
    private readonly IResourceMutationJournal _journal;
    private readonly LocalPatchOptions _options;

    public LocalWorkspaceWriter(WorkspaceId workspace, string root, IResourceAuthorizer authorizer,
        IResourceMutationJournal journal, LocalPatchOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(workspace.Value) || workspace.Value.Length > MaxTextTokenCharacters ||
            workspace.Value.Any(char.IsControl))
            throw new ArgumentException("A bounded workspace ID is required.", nameof(workspace));
        ArgumentNullException.ThrowIfNull(root);
        _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _options = options ?? new LocalPatchOptions();
        if (_options.MaxOriginalBytes is < 0 or > MaxFileBytes || _options.MaxReadBytes is < 0 or > 2 * MaxFileBytes ||
            _options.TimeoutMilliseconds is < 1 or > 30_000)
            throw new ArgumentOutOfRangeException(nameof(options));

        if (root.StartsWith("\\\\", StringComparison.Ordinal) || root.Length < 3 ||
            !char.IsAsciiLetter(root[0]) || root[1] != ':' || root[2] is not ('\\' or '/'))
            throw new ArgumentException("The patch profile requires an absolute fixed-drive root.", nameof(root));

        var fullRoot = Path.GetFullPath(root);
        _root = fullRoot.Length > 3 ? fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : fullRoot;
        _driveRoot = Path.GetPathRoot(_root) ?? throw new ArgumentException("The root must be on a drive letter.", nameof(root));
        if (_driveRoot.Length != 3 || !char.IsAsciiLetter(_driveRoot[0]) || _driveRoot[1] != ':' || _driveRoot[2] != '\\' ||
            _root.StartsWith("\\\\", StringComparison.Ordinal) || _root.Length > WindowsWorkspacePath.MaximumLength)
            throw new ArgumentException("The patch profile does not support UNC or device paths.", nameof(root));

        _workspace = workspace;
    }

    public async ValueTask<ResourceResult<ResourceVersion>> WriteFileAsync(FileWriteRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.Unsupported);
        if (_options.Namespace != LocalPatchNamespace.HostControlled)
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.Unsupported);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMilliseconds(_options.TimeoutMilliseconds));
        try
        {
            return await WriteCoreAsync(request, deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.ProviderFailure, "Operation deadline exceeded.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PatchFailure failure)
        {
            return ResourceResult<ResourceVersion>.Failed(failure.Kind);
        }
        catch (FileNotFoundException)
        {
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.NotFound);
        }
        catch (DirectoryNotFoundException)
        {
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.NotFound);
        }
        catch (UnauthorizedAccessException)
        {
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.AccessDenied);
        }
        catch (IOException)
        {
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.ProviderFailure);
        }
        catch (ArgumentException)
        {
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.InvalidRequest);
        }
        catch (NotSupportedException)
        {
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.Unsupported);
        }
        catch
        {
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.ProviderFailure);
        }
    }

    private async ValueTask<ResourceResult<ResourceVersion>> WriteCoreAsync(FileWriteRequest request, CancellationToken ct)
    {
        var snapshot = Snapshot(request);
        var path = snapshot.Path;
        var identity = ResourceRequestIdentity.Compute(snapshot.Request);
        if (snapshot.Request.Invocation.RequestIdentity != identity)
            throw new PatchFailure(ResourceFailureKind.AuthorizationDenied);

        // Admit the concrete operations before probing the root, ancestors, or target.
        await Demand(snapshot.Request.Invocation, identity, ResourceAction.WriteFile,
            new ResourceBinding.WorkspaceFile(_workspace, path), ct).ConfigureAwait(false);
        await Demand(snapshot.Request.Invocation, identity, ResourceAction.ReadFile,
            new ResourceBinding.WorkspaceFile(_workspace, path), ct).ConfigureAwait(false);
        foreach (var ancestor in Ancestors(path))
            await Demand(snapshot.Request.Invocation, identity, ResourceAction.ReadMetadata,
                new ResourceBinding.WorkspaceEntry(_workspace, ancestor), ct).ConfigureAwait(false);
        await Demand(snapshot.Request.Invocation, identity, ResourceAction.ReadMetadata,
            new ResourceBinding.WorkspaceEntry(_workspace, path), ct).ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();
        EnsureFixedNtfsDrive();
        using var pins = PinDirectories(path, ct);
        var expectedPath = JoinFinal(pins.TargetDirectoryFinalPath, LastSegment(path));
        using var fileHandle = CreateFileW(Full(path), GenericRead | GenericWrite, 0, IntPtr.Zero,
            OpenExisting, OpenReparsePoint | Overlapped | WriteThrough, IntPtr.Zero);
        if (fileHandle.IsInvalid) ThrowLastError();
        var targetInfo = Attributes(fileHandle);
        if ((targetInfo.Attributes & FileAttributeReparsePoint) != 0)
            throw new PatchFailure(ResourceFailureKind.AccessDenied);
        if ((targetInfo.Attributes & FileAttributeDirectory) != 0)
            throw new PatchFailure(ResourceFailureKind.InvalidRequest);
        if (!FinalPathEquals(FinalPath(fileHandle), expectedPath))
            throw new PatchFailure(ResourceFailureKind.AccessDenied);

        if (!GetFileInformationByHandle(fileHandle, out var standard)) ThrowLastError();
        if (standard.NumberOfLinks != 1)
            throw new PatchFailure(ResourceFailureKind.Unsupported);
        var fileId = FileIdentity(fileHandle);
        if (fileId.VolumeSerial != pins.VolumeSerial)
            throw new PatchFailure(ResourceFailureKind.AccessDenied);

        // The stream wrapper is non-owning: disposing it cannot release the exclusive
        // file handle before the mutation journal has recorded the outcome.
        var streamHandle = new SafeFileHandle(fileHandle.DangerousGetHandle(), ownsHandle: false);
        var stream = new FileStream(streamHandle, FileAccess.ReadWrite, 16 * 1024, isAsync: true);
        try
        {
        var original = await ReadBoundedAsync(stream, Math.Min(_options.MaxOriginalBytes, _options.MaxReadBytes), ct).ConfigureAwait(false);
        var originalVersion = VersionOf(original);
        if (snapshot.Request.Precondition.Version != originalVersion)
            throw new PatchFailure(ResourceFailureKind.PreconditionFailed);

        var proposed = snapshot.Content;
        if ((long)original.Length + proposed.Length > _options.MaxReadBytes)
            throw new PatchFailure(ResourceFailureKind.TooLarge);
        var proposedVersion = VersionOf(proposed);

        // Final resource authorization is immediately adjacent to the journal start boundary.
        await Demand(snapshot.Request.Invocation, identity, ResourceAction.WriteFile,
            new ResourceBinding.WorkspaceFile(_workspace, path), ct).ConfigureAwait(false);
        var start = new MutationStartRequest(snapshot.Request.Invocation, identity, _workspace, path, ProviderProfile,
            fileId.Canonical, originalVersion, proposedVersion, original.Length, proposed.Length);

        MutationStartDecision? decision;
        try
        {
            decision = await _journal.StartAsync(start, ct).AsTask().WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            throw new PatchFailure(ResourceFailureKind.AuthorizationUnavailable);
        }

        if (decision?.Status != MutationStartStatus.Started)
            throw new PatchFailure(decision?.Status switch
            {
                MutationStartStatus.Deny => ResourceFailureKind.AuthorizationDenied,
                MutationStartStatus.AlreadyStarted => ResourceFailureKind.AmbiguousOutcome,
                _ => ResourceFailureKind.AuthorizationUnavailable
            });

        // A returned Started result means even a malformed evidence token must be closed out.
        var evidence = decision.EvidenceId;
        var mutationStarted = true;
        var completionAttempted = false;
        var firstWriteMayHaveStarted = false;
        try
        {
            if (!ValidEvidence(evidence))
            {
                completionAttempted = true;
                var recorded = await Complete(start, evidence ?? string.Empty, MutationOutcome.NoMutation,
                    originalVersion).ConfigureAwait(false);
                mutationStarted = false;
                throw new PatchFailure(recorded ? ResourceFailureKind.ProviderFailure : ResourceFailureKind.AmbiguousOutcome);
            }

            ct.ThrowIfCancellationRequested();
            if (!GetFileInformationByHandle(fileHandle, out var beforeWriteInfo)) ThrowLastError();
            if (beforeWriteInfo.NumberOfLinks != 1)
                throw new PatchFailure(ResourceFailureKind.Unsupported);
            firstWriteMayHaveStarted = true; // SetLength is the first mutating call.
            stream.Position = 0;
            stream.SetLength(0);
            await stream.WriteAsync(proposed, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);

            var verified = await ReadBoundedAsync(stream, Math.Min(MaxOutputBytes, _options.MaxReadBytes - original.Length), ct).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(verified), SHA256.HashData(proposed)) ||
                verified.Length != proposed.Length)
                throw new PatchFailure(ResourceFailureKind.AmbiguousOutcome);
            var observedVersion = VersionOf(verified);

            // Surface any stream-disposal failure while the Started journal entry can
            // still be completed as Ambiguous. The original exclusive handle remains pinned.
            await stream.DisposeAsync().ConfigureAwait(false);

            completionAttempted = true;
            var completed = await Complete(start, evidence!, MutationOutcome.Completed, observedVersion).ConfigureAwait(false);
            mutationStarted = false;
            if (!completed) throw new PatchFailure(ResourceFailureKind.AmbiguousOutcome);
            return ResourceResult<ResourceVersion>.Success(observedVersion);
        }
        catch (OperationCanceledException)
        {
            if (mutationStarted && !completionAttempted)
            {
                completionAttempted = true;
                var outcome = firstWriteMayHaveStarted ? MutationOutcome.Ambiguous : MutationOutcome.NoMutation;
                var recorded = await Complete(start, evidence!, outcome, outcome == MutationOutcome.NoMutation ? originalVersion : null)
                    .ConfigureAwait(false);
                mutationStarted = false;
                if (!recorded || outcome == MutationOutcome.Ambiguous)
                    return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.AmbiguousOutcome);
            }
            throw;
        }
        catch (Exception ex)
        {
            if (mutationStarted && !completionAttempted)
            {
                completionAttempted = true;
                var outcome = firstWriteMayHaveStarted ? MutationOutcome.Ambiguous : MutationOutcome.NoMutation;
                var recorded = await Complete(start, evidence!, outcome, outcome == MutationOutcome.NoMutation ? originalVersion : null)
                    .ConfigureAwait(false);
                mutationStarted = false;
                if (!recorded || outcome == MutationOutcome.Ambiguous)
                    return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.AmbiguousOutcome);
            }
            if (ex is PatchFailure failure) return ResourceResult<ResourceVersion>.Failed(failure.Kind);
            if (ex is FileNotFoundException or DirectoryNotFoundException)
                return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.NotFound);
            if (ex is UnauthorizedAccessException) return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.AccessDenied);
            return ResourceResult<ResourceVersion>.Failed(ResourceFailureKind.ProviderFailure);
        }
        }
        finally
        {
            try { await stream.DisposeAsync().ConfigureAwait(false); }
            catch { /* The journal boundary above classifies disposal failure before success. */ }
        }
    }

    private SnapshotData Snapshot(FileWriteRequest request)
    {
        if (request is null || request.Limits is null || request.Precondition is null || request.Invocation is null)
            throw new PatchFailure(ResourceFailureKind.InvalidRequest);
        if (request.Workspace != _workspace)
            throw new PatchFailure(ResourceFailureKind.AuthorizationDenied);
        if (!ValidInvocation(request.Invocation))
            throw new PatchFailure(ResourceFailureKind.InvalidRequest);
        if (request.Precondition.Kind != WritePreconditionKind.MustMatchVersion)
            throw new PatchFailure(ResourceFailureKind.Unsupported);
        if (request.Precondition.Version is null || !ValidToken(request.Precondition.Version.Value.Value))
            throw new PatchFailure(ResourceFailureKind.InvalidRequest);
        if (request.Limits.MaxBytes is < 0 or > MaxOutputBytes || request.Content.Length > request.Limits.MaxBytes)
            throw new PatchFailure(ResourceFailureKind.TooLarge);
        WorkspacePath path;
        try { path = WindowsWorkspacePath.Normalize(request.Path, allowRoot: false); }
        catch (ArgumentException) { throw new PatchFailure(ResourceFailureKind.InvalidPath); }
        var content = request.Content.ToArray();
        var snapshotRequest = request with { Path = path, Content = content, Limits = request.Limits with { }, Precondition = request.Precondition with { } };
        return new(snapshotRequest, path, content);
    }
    private async ValueTask Demand(HostInvocation invocation, RequestIdentity identity, ResourceAction action,
        ResourceBinding binding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ResourceAuthorizationDecision? decision;
        try
        {
            decision = await _authorizer.AuthorizeAsync(new(invocation, identity, action, binding), ct)
                .AsTask().WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { throw new PatchFailure(ResourceFailureKind.AuthorizationUnavailable); }
        ct.ThrowIfCancellationRequested();
        if (decision?.Status == AuthorizationStatus.Deny)
            throw new PatchFailure(ResourceFailureKind.AuthorizationDenied);
        if (decision?.Status != AuthorizationStatus.Permit)
            throw new PatchFailure(ResourceFailureKind.AuthorizationUnavailable);
    }

    private async ValueTask<bool> Complete(MutationStartRequest start, string evidence, MutationOutcome outcome,
        ResourceVersion? observed)
    {
        using var independent = new CancellationTokenSource(MutationCompletionTimeoutMilliseconds);
        try
        {
            return await _journal.CompleteAsync(new(start, evidence, outcome, observed), independent.Token)
                .AsTask().WaitAsync(independent.Token).ConfigureAwait(false);
        }
        catch { return false; }
    }

    private void EnsureFixedNtfsDrive()
    {
        if (GetDriveTypeW(_driveRoot) != DriveFixed)
            throw new PatchFailure(ResourceFailureKind.Unsupported);
        var fileSystem = new StringBuilder(64);
        if (!GetVolumeInformationW(_driveRoot, null, 0, out _, out _, out _, fileSystem, (uint)fileSystem.Capacity))
            ThrowLastError();
        if (!string.Equals(fileSystem.ToString(), "NTFS", StringComparison.OrdinalIgnoreCase))
            throw new PatchFailure(ResourceFailureKind.Unsupported);
    }

    private PinnedDirectories PinDirectories(WorkspacePath path, CancellationToken ct)
    {
        var handles = new List<SafeFileHandle>();
        try
        {
            var driveHandle = OpenDirectory(_driveRoot);
            handles.Add(driveHandle);
            var driveIdentity = FileIdentity(driveHandle);
            ValidatePinnedDirectory(driveHandle, _driveRoot, driveIdentity.VolumeSerial);
            var actualDrive = FinalPath(driveHandle);

            var current = _driveRoot;
            var currentFinal = actualDrive;
            foreach (var component in RelativeComponents(_root, _driveRoot))
            {
                ct.ThrowIfCancellationRequested();
                current = Path.Combine(current, component);
                var handle = OpenDirectory(current);
                handles.Add(handle);
                ValidatePinnedDirectory(handle, current, driveIdentity.VolumeSerial);
                currentFinal = FinalPath(handle);
            }
            foreach (var component in path.Value.Split('/').SkipLast(1))
            {
                ct.ThrowIfCancellationRequested();
                current = Path.Combine(current, component);
                var handle = OpenDirectory(current);
                handles.Add(handle);
                ValidatePinnedDirectory(handle, current, driveIdentity.VolumeSerial);
                currentFinal = FinalPath(handle);
            }
            return new(handles, driveIdentity.VolumeSerial, currentFinal);
        }
        catch
        {
            foreach (var handle in handles) handle.Dispose();
            throw;
        }
    }

    private static void ValidatePinnedDirectory(SafeFileHandle handle, string expectedPath, ulong volumeSerial)
    {
        var attributes = Attributes(handle);
        if ((attributes.Attributes & FileAttributeReparsePoint) != 0)
            throw new PatchFailure(ResourceFailureKind.AccessDenied);
        if ((attributes.Attributes & FileAttributeDirectory) == 0)
            throw new PatchFailure(ResourceFailureKind.NotFound);
        var identity = FileIdentity(handle);
        if (identity.VolumeSerial != volumeSerial)
            throw new PatchFailure(ResourceFailureKind.AccessDenied);
        if (!FinalPathEquals(FinalPath(handle), FinalDisplayPath(expectedPath)))
            throw new PatchFailure(ResourceFailureKind.AccessDenied);
        var caseBuffer = Marshal.AllocHGlobal(sizeof(uint));
        try
        {
            if (!GetFileInformationByHandleEx(handle, FileCaseSensitiveInfoClass, caseBuffer, (uint)sizeof(uint))) ThrowLastError();
            if ((unchecked((uint)Marshal.ReadInt32(caseBuffer)) & 1) != 0)
                throw new PatchFailure(ResourceFailureKind.Unsupported);
        }
        finally { Marshal.FreeHGlobal(caseBuffer); }
    }

    private static SafeFileHandle OpenDirectory(string path)
    {
        var handle = CreateFileW(path, FileReadAttributes, ShareRead, IntPtr.Zero, OpenExisting,
            OpenReparsePoint | BackupSemantics, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            ThrowLastError();
        }
        return handle;
    }

    private static FileAttributeTag Attributes(SafeFileHandle handle)
    {
        var buffer = Marshal.AllocHGlobal(8);
        try
        {
            if (!GetFileInformationByHandleEx(handle, FileAttributeTagInfoClass, buffer, 8)) ThrowLastError();
            return new(unchecked((uint)Marshal.ReadInt32(buffer)), unchecked((uint)Marshal.ReadInt32(buffer, 4)));
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static FileIdentityValue FileIdentity(SafeFileHandle handle)
    {
        var buffer = Marshal.AllocHGlobal(24);
        try
        {
            if (!GetFileInformationByHandleEx(handle, FileIdInfoClass, buffer, 24)) ThrowLastError();
            var volume = unchecked((ulong)Marshal.ReadInt64(buffer));
            var id = new byte[16];
            Marshal.Copy(IntPtr.Add(buffer, 8), id, 0, id.Length);
            return new(volume, Convert.ToHexString(id));
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static string FinalPath(SafeFileHandle handle)
    {
        var capacity = 512;
        while (capacity <= 32768)
        {
            var buffer = new StringBuilder(capacity);
            var length = GetFinalPathNameByHandleW(handle, buffer, (uint)capacity, 0);
            if (length == 0) ThrowLastError();
            if (length < capacity) return NormalizeFinalPath(buffer.ToString());
            capacity = checked((int)length + 1);
        }
        throw new PatchFailure(ResourceFailureKind.InvalidPath);
    }

    private static string FinalDisplayPath(string path)
    {
        var full = Path.GetFullPath(path);
        if (full.Length > 3) full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return NormalizeFinalPath("\\\\?\\" + full);
    }

    private static string NormalizeFinalPath(string path)
    {
        if (path.Length > 7) return path.TrimEnd('\\');
        return path;
    }

    private static bool FinalPathEquals(string left, string right) =>
        string.Equals(FoldAscii(left), FoldAscii(right), StringComparison.Ordinal);

    private static string FoldAscii(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (chars[i] is >= 'a' and <= 'z') chars[i] = (char)(chars[i] - ('a' - 'A'));
        return new string(chars);
    }

    private static string JoinFinal(string directory, string leaf) => directory.TrimEnd('\\') + "\\" + leaf;
    private string Full(WorkspacePath path) => Path.Combine(_root, path.Value.Replace('/', Path.DirectorySeparatorChar));

    private static IEnumerable<string> RelativeComponents(string fullRoot, string driveRoot)
    {
        var relative = fullRoot[driveRoot.Length..];
        return relative.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static IEnumerable<WorkspacePath> Ancestors(WorkspacePath path)
    {
        yield return WorkspacePath.Root;
        var parts = path.Value.Split('/');
        var current = "";
        for (var i = 0; i < parts.Length - 1; i++)
        {
            current = current.Length == 0 ? parts[i] : current + "/" + parts[i];
            yield return new WorkspacePath(current);
        }
    }

    private static string LastSegment(WorkspacePath path) => path.Value[(path.Value.LastIndexOf('/') + 1)..];

    private static async ValueTask<byte[]> ReadBoundedAsync(FileStream stream, int maximum, CancellationToken ct)
    {
        stream.Position = 0;
        var length = stream.Length;
        if (length < 0 || length > maximum) throw new PatchFailure(ResourceFailureKind.TooLarge);
        var bytes = new byte[(int)length];
        var read = 0;
        while (read < bytes.Length)
        {
            ct.ThrowIfCancellationRequested();
            var n = await stream.ReadAsync(bytes.AsMemory(read), ct).ConfigureAwait(false);
            if (n == 0) throw new PatchFailure(ResourceFailureKind.ProviderFailure);
            read += n;
        }
        // Detect growth beyond the length snapshot without allocating beyond the cap.
        var one = new byte[1];
        if (await stream.ReadAsync(one, ct).ConfigureAwait(false) != 0)
            throw new PatchFailure(ResourceFailureKind.TooLarge);
        return bytes;
    }

    private static ResourceVersion VersionOf(ReadOnlySpan<byte> bytes) =>
        new("local-read-v1:sha256:" + Convert.ToHexString(SHA256.HashData(bytes)));

    private static bool ValidInvocation(HostInvocation? invocation) => invocation is not null && ValidToken(invocation.InvocationId) &&
        ValidToken(invocation.SubjectId) && ValidToken(invocation.EffectId) && ValidToken(invocation.AttemptId) &&
        ValidToken(invocation.EffectScopeId) && (invocation.ParentEffectScopeId is null || ValidToken(invocation.ParentEffectScopeId)) &&
        ValidToken(invocation.RequestIdentity.Value);

    private static bool ValidToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxTextTokenCharacters || value.Any(char.IsControl)) return false;
        try { return StrictUtf8.GetByteCount(value) <= 1024; }
        catch (EncoderFallbackException) { return false; }
    }

    private static bool ValidEvidence(string? evidence)
    {
        if (!ValidToken(evidence)) return false;
        return evidence!.Length <= MaxEvidenceCharacters;
    }

    private static void ThrowLastError()
    {
        var code = Marshal.GetLastWin32Error();
        if (code is 2 or 3) throw new FileNotFoundException();
        if (code is 5 or 32 or 33) throw new UnauthorizedAccessException();
        throw new IOException("A native file operation failed.", Marshal.GetExceptionForHR(unchecked((int)(0x80070000 | (uint)code))));
    }
    private sealed record SnapshotData(FileWriteRequest Request, WorkspacePath Path, byte[] Content);
    private sealed record FileAttributeTag(uint Attributes, uint ReparseTag);
    private sealed record FileIdentityValue(ulong VolumeSerial, string FileId)
    { internal string Canonical => $"ntfs:{VolumeSerial:x16}:{FileId}"; }
    private sealed record PinnedDirectories(List<SafeFileHandle> Handles, ulong VolumeSerial, string TargetDirectoryFinalPath) : IDisposable
    { public void Dispose() { foreach (var handle in Handles) handle.Dispose(); } }
    private sealed class PatchFailure(ResourceFailureKind kind) : Exception
    { internal ResourceFailureKind Kind { get; } = kind; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int informationClass, IntPtr information, uint bufferSize);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out ByHandleFileInformation information);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder path, uint pathLength, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint GetDriveTypeW(string rootPathName);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetVolumeInformationW(string rootPathName, StringBuilder? volumeNameBuffer, uint volumeNameSize,
        out uint volumeSerialNumber, out uint maximumComponentLength, out uint fileSystemFlags,
        StringBuilder fileSystemNameBuffer, uint fileSystemNameSize);
}
