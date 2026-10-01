using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Buffers;
using Penghou.IO.Abstractions;

namespace Penghou.IO.Local;

public sealed record LocalReaderOptions
{
    public int MaxFileBytes { get; init; } = 16 * 1024 * 1024;
    public int MaxEntries { get; init; } = 10_000;
    public int MaxCandidatesScanned { get; init; } = 100_000;
    public int MaxTotalCandidatesScanned { get; init; } = 100_000;
    public int MaxOutputBytes { get; init; } = 4 * 1024 * 1024;
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan ContinuationLifetime { get; init; } = TimeSpan.FromMinutes(2);
    public int MaxContinuations { get; init; } = 128;
}

/// <summary>Windows path-based, bounded reader. Requires a trusted host authorizer; does not provide confinement.</summary>
public sealed class LocalWorkspaceReader : IWorkspaceReader, IDisposable
{
    private readonly WorkspaceId _workspace;
    private readonly string _root;
    private readonly IResourceAuthorizer _authorizer;
    private readonly LocalReaderOptions _options;
    private readonly object _gate = new();
    private readonly Dictionary<string, Cursor> _cursors = new(StringComparer.Ordinal);
    private bool _disposed;
    private int _remainingCandidates;

    public LocalWorkspaceReader(WorkspaceId workspace, string root, IResourceAuthorizer authorizer, LocalReaderOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(workspace.Value) || workspace.Value.Length > 256) throw new ArgumentException("A bounded workspace ID is required.", nameof(workspace));
        ArgumentNullException.ThrowIfNull(root);
        _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
        _workspace = workspace;
        _root = Path.GetFullPath(root);
        _options = options ?? new();
        _remainingCandidates = _options.MaxTotalCandidatesScanned;
        if (_options.MaxFileBytes is < 1 or > 16 * 1024 * 1024 || _options.MaxEntries is < 1 or > 100_000 ||
            _options.MaxCandidatesScanned is < 1 or > 100_000 || _options.MaxOutputBytes is < 1 or > 4 * 1024 * 1024 ||
            _options.MaxTotalCandidatesScanned is < 1 or > 100_000 ||
            _options.MaxContinuations is < 1 or > 1024 || _options.OperationTimeout <= TimeSpan.Zero || _options.OperationTimeout > TimeSpan.FromMinutes(5) ||
            _options.ContinuationLifetime <= TimeSpan.Zero || _options.ContinuationLifetime > TimeSpan.FromMinutes(10))
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    public ValueTask<ResourceResult<FileReadResult>> ReadFileAsync(FileReadRequest request, CancellationToken cancellationToken = default) =>
        Run(async ct =>
        {
            if (request is null || request.Limits is null || request.Limits.MaxBytes < 1 || request.Limits.MaxBytes > _options.MaxFileBytes)
                throw new Failure(ResourceFailureKind.InvalidRequest);
            var path = Validate(request.Invocation, request.Workspace, request.Path, false);
            var identity = ResourceRequestIdentity.Compute(request);
            Verify(request.Invocation, identity);
            await Demand(request.Invocation, identity, ResourceAction.ReadFile, new ResourceBinding.WorkspaceFile(_workspace, path), ct).ConfigureAwait(false);
            await Qualify(request.Invocation, identity, path, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            await using var stream = new FileStream(Full(path), FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length > request.Limits.MaxBytes) throw new Failure(ResourceFailureKind.TooLarge);
            using var output = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(request.Limits.MaxBytes + 1, 16 * 1024));
            try
            {
                while (true)
                {
                    var allowance = request.Limits.MaxBytes - checked((int)output.Length) + 1;
                    var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, allowance)), ct).ConfigureAwait(false);
                    if (read == 0) break;
                    if (read > request.Limits.MaxBytes - output.Length) throw new Failure(ResourceFailureKind.TooLarge);
                    output.Write(buffer, 0, read);
                }
            }
            finally { ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
            var content = output.ToArray();
            return new FileReadResult(content, new("local-read-v1:sha256:" + Convert.ToHexString(SHA256.HashData(content))));
        }, cancellationToken);

    public ValueTask<ResourceResult<FileMetadata>> GetFileMetadataAsync(FileMetadataRequest request, CancellationToken cancellationToken = default) =>
        Run(async ct =>
        {
            if (request is null) throw new Failure(ResourceFailureKind.InvalidRequest);
            var path = Validate(request.Invocation, request.Workspace, request.Path, false);
            var identity = ResourceRequestIdentity.Compute(request);
            Verify(request.Invocation, identity);
            await Demand(request.Invocation, identity, ResourceAction.ReadMetadata, new ResourceBinding.WorkspaceFile(_workspace, path), ct).ConfigureAwait(false);
            try
            {
                var attributes = await Qualify(request.Invocation, identity, path, ct).ConfigureAwait(false);
                if ((attributes & FileAttributes.Directory) != 0) throw new Failure(ResourceFailureKind.InvalidRequest);
                ct.ThrowIfCancellationRequested();
                return new FileMetadata(true, new FileInfo(Full(path)).Length, null);
            }
            catch (FileNotFoundException) { return new FileMetadata(false, null, null); }
            catch (DirectoryNotFoundException) { return new FileMetadata(false, null, null); }
        }, cancellationToken);

    public ValueTask<ResourceResult<DirectoryPage>> ListDirectoryAsync(DirectoryListRequest request, CancellationToken cancellationToken = default) =>
        Run(async ct =>
        {
            if (request is null || request.MaxEntries < 1 || request.MaxEntries > _options.MaxEntries ||
                request.MaxCandidatesScanned < 1 || request.MaxCandidatesScanned > _options.MaxCandidatesScanned ||
                request.MaxOutputBytes < 1 || request.MaxOutputBytes > _options.MaxOutputBytes)
                throw new Failure(ResourceFailureKind.InvalidRequest);
            var path = Validate(request.Invocation, request.Workspace, request.Path, true);
            var identity = ResourceRequestIdentity.Compute(request);
            Verify(request.Invocation, identity);
            // Retained random tokens authenticate opaque state; they convey no resource permission.
            Cursor? cursor = null;
            var retained = false;
            try
            {
                if (request.Continuation is { } token)
                {
                    lock (_gate)
                    {
                        ReclaimExpired();
                        if (token.Value is null || !_cursors.TryGetValue(token.Value, out var existing) || !existing.Matches(request, path))
                            throw new Failure(ResourceFailureKind.AuthorizationDenied);
                        cursor = existing;
                        _cursors.Remove(token.Value);
                    }
                }
                await Demand(request.Invocation, identity, ResourceAction.ListDirectory, new ResourceBinding.WorkspaceDirectory(_workspace, path), ct).ConfigureAwait(false);
                lock (_gate) { if (_remainingCandidates <= 0) throw new Failure(ResourceFailureKind.TooLarge); }
                var attributes = await Qualify(request.Invocation, identity, path, ct).ConfigureAwait(false);
                if ((attributes & FileAttributes.Directory) == 0) throw new Failure(ResourceFailureKind.NotFound);
                ct.ThrowIfCancellationRequested();
                cursor ??= new Cursor(request, path, Directory.EnumerateFileSystemEntries(Full(path), "*", new EnumerationOptions
                {
                    AttributesToSkip = 0,
                    IgnoreInaccessible = false,
                    RecurseSubdirectories = false,
                    ReturnSpecialDirectories = false
                }).GetEnumerator());
                var entries = new List<DirectoryEntry>();
                var scanned = 0;
                var output = 0;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (scanned >= request.MaxCandidatesScanned) return Incomplete(DirectoryTruncationReason.CandidateScanLimit);
                    if (entries.Count >= request.MaxEntries) return Incomplete(DirectoryTruncationReason.EntryLimit);
                    lock (_gate)
                    {
                        if (_remainingCandidates <= 0) return Incomplete(DirectoryTruncationReason.CandidateScanLimit);
                        _remainingCandidates--;
                    }
                    scanned++;
                    var candidate = cursor.Pending;
                    if (candidate is null)
                    {
                        if (!cursor.Enumerator.MoveNext()) return new DirectoryPage(entries.ToArray(), true, null, null);
                        candidate = cursor.Enumerator.Current;
                    }
                    cursor.Pending = null;
                    var name = Path.GetFileName(candidate);
                    WorkspacePath child;
                    try { child = WindowsWorkspacePath.Normalize(new(path.Value.Length == 0 ? name : path.Value + "/" + name), false); }
                    catch (ArgumentException) { continue; }
                    var allowed = await Check(request.Invocation, identity, ResourceAction.ReadMetadata, new ResourceBinding.WorkspaceEntry(_workspace, child), ct).ConfigureAwait(false);
                    if (!allowed) continue;
                    FileAttributes childAttributes;
                    long? length;
                    try
                    {
                        ct.ThrowIfCancellationRequested();
                        childAttributes = File.GetAttributes(candidate);
                        if ((childAttributes & FileAttributes.ReparsePoint) != 0) continue;
                        if ((childAttributes & FileAttributes.Directory) != 0) RejectCaseSensitiveDirectory(candidate);
                        length = (childAttributes & FileAttributes.Directory) == 0 ? new FileInfo(candidate).Length : null;
                    }
                    catch (FileNotFoundException) { continue; }
                    catch (DirectoryNotFoundException) { continue; }
                    var cost = Encoding.UTF8.GetByteCount(name) + 32; // pinned logical entry encoding, including metadata overhead
                    if (cost > request.MaxOutputBytes - output)
                    {
                        if (entries.Count == 0) throw new Failure(ResourceFailureKind.TooLarge);
                        cursor.Pending = candidate;
                        return Incomplete(DirectoryTruncationReason.OutputByteLimit);
                    }
                    entries.Add(new(name, (childAttributes & FileAttributes.Directory) != 0, length, null));
                    output += cost;
                }

                DirectoryPage Incomplete(DirectoryTruncationReason reason)
                {
                    ct.ThrowIfCancellationRequested();
                    lock (_gate)
                    {
                        if (_disposed) throw new Failure(ResourceFailureKind.ProviderFailure);
                        ReclaimExpired();
                        if (_cursors.Count >= _options.MaxContinuations) throw new Failure(ResourceFailureKind.TooLarge);
                        var value = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                        cursor.Expires = Environment.TickCount64 + (long)_options.ContinuationLifetime.TotalMilliseconds;
                        _cursors.Add(value, cursor);
                        retained = true;
                        return new(entries.ToArray(), false, new(value), reason);
                    }
                }
            }
            finally { if (!retained) cursor?.Dispose(); }
        }, cancellationToken);

    private WorkspacePath Validate(HostInvocation invocation, WorkspaceId workspace, WorkspacePath path, bool root)
    {
        if (!ValidInvocation(invocation)) throw new Failure(ResourceFailureKind.InvalidRequest);
        if (workspace != _workspace) throw new Failure(ResourceFailureKind.AuthorizationDenied);
        try { return WindowsWorkspacePath.Normalize(path, root); }
        catch (ArgumentException) { throw new Failure(ResourceFailureKind.InvalidPath); }
    }

    private static bool ValidInvocation(HostInvocation? i) => i is not null && ValidId(i.InvocationId) && ValidId(i.SubjectId) &&
        ValidId(i.EffectId) && ValidId(i.AttemptId) && ValidId(i.EffectScopeId) && (i.ParentEffectScopeId is null || ValidId(i.ParentEffectScopeId)) && ValidId(i.RequestIdentity.Value);
    private static bool ValidId(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !value.Any(char.IsControl);
    private static void Verify(HostInvocation invocation, RequestIdentity computed)
    {
        if (invocation.RequestIdentity != computed) throw new Failure(ResourceFailureKind.AuthorizationDenied);
    }

    private async ValueTask<bool> Check(HostInvocation invocation, RequestIdentity identity, ResourceAction action, ResourceBinding binding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ResourceAuthorizationDecision? decision;
        try { decision = await _authorizer.AuthorizeAsync(new(invocation, identity, action, binding), ct).AsTask().WaitAsync(ct).ConfigureAwait(false); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { throw new Failure(ResourceFailureKind.AuthorizationUnavailable); }
        ct.ThrowIfCancellationRequested();
        return decision?.Status switch
        {
            AuthorizationStatus.Permit => true,
            AuthorizationStatus.Deny => false,
            _ => throw new Failure(ResourceFailureKind.AuthorizationUnavailable)
        };
    }

    private async ValueTask Demand(HostInvocation invocation, RequestIdentity identity, ResourceAction action, ResourceBinding binding, CancellationToken ct)
    {
        if (!await Check(invocation, identity, action, binding, ct).ConfigureAwait(false)) throw new Failure(ResourceFailureKind.AuthorizationDenied);
    }

    // Each ancestor metadata probe needs explicit permission too. Leaf permission was checked by the caller.
    private async ValueTask<FileAttributes> Qualify(HostInvocation invocation, RequestIdentity identity, WorkspacePath path, CancellationToken ct)
    {
        if (path.Value.Length != 0)
            await Demand(invocation, identity, ResourceAction.ReadMetadata, new ResourceBinding.WorkspaceEntry(_workspace, WorkspacePath.Root), ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        var attributes = File.GetAttributes(_root);
        RejectLink(attributes);
        if ((attributes & FileAttributes.Directory) != 0) RejectCaseSensitiveDirectory(_root);
        if (path.Value.Length == 0) return attributes;
        var parts = path.Value.Split('/');
        var current = "";
        for (var i = 0; i < parts.Length; i++)
        {
            current = current.Length == 0 ? parts[i] : current + "/" + parts[i];
            var component = new WorkspacePath(current);
            if (i < parts.Length - 1)
                await Demand(invocation, identity, ResourceAction.ReadMetadata, new ResourceBinding.WorkspaceEntry(_workspace, component), ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            attributes = File.GetAttributes(Full(component));
            RejectLink(attributes);
            if ((attributes & FileAttributes.Directory) != 0) RejectCaseSensitiveDirectory(Full(component));
        }
        return attributes;
    }

    private static void RejectLink(FileAttributes attributes)
    {
        if ((attributes & FileAttributes.ReparsePoint) != 0) throw new Failure(ResourceFailureKind.AccessDenied);
    }
    private static void RejectCaseSensitiveDirectory(string path)
    {
        // Case-folded request identity is sound only for the qualified insensitive profile.
        using var handle = CreateFileW(path, 0x80, 7, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid) throw new Failure(ResourceFailureKind.AccessDenied);
        if (!GetFileInformationByHandleEx(handle, 23, out var info, sizeof(uint)))
            throw new Failure(ResourceFailureKind.Unsupported);
        if ((info.Flags & 1) != 0) throw new Failure(ResourceFailureKind.Unsupported);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct CaseSensitiveInfo { internal uint Flags; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string fileName, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int infoClass, out CaseSensitiveInfo info, uint size);
    private string Full(WorkspacePath path) => path.Value.Length == 0 ? _root : Path.Combine(_root, path.Value.Replace('/', Path.DirectorySeparatorChar));

    private async ValueTask<ResourceResult<T>> Run<T>(Func<CancellationToken, ValueTask<T>> body, CancellationToken caller)
    {
        caller.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return ResourceResult<T>.Failed(ResourceFailureKind.Unsupported);
        lock (_gate) { if (_disposed) return ResourceResult<T>.Failed(ResourceFailureKind.ProviderFailure); }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(caller);
        deadline.CancelAfter(_options.OperationTimeout);
        T? produced = default;
        var delivered = false;
        try
        {
            produced = await body(deadline.Token).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested();
            delivered = true;
            return ResourceResult<T>.Success(produced!);
        }
        catch (OperationCanceledException) when (!caller.IsCancellationRequested) { return ResourceResult<T>.Failed(ResourceFailureKind.ProviderFailure, "Operation deadline exceeded."); }
        catch (OperationCanceledException) { throw; }
        catch (Failure ex) { return ResourceResult<T>.Failed(ex.Kind); }
        catch (FileNotFoundException) { return ResourceResult<T>.Failed(ResourceFailureKind.NotFound); }
        catch (DirectoryNotFoundException) { return ResourceResult<T>.Failed(ResourceFailureKind.NotFound); }
        catch (UnauthorizedAccessException) { return ResourceResult<T>.Failed(ResourceFailureKind.AccessDenied); }
        catch (IOException) { return ResourceResult<T>.Failed(ResourceFailureKind.ProviderFailure); }
        catch (ArgumentException) { return ResourceResult<T>.Failed(ResourceFailureKind.InvalidRequest); }
        catch (NotSupportedException) { return ResourceResult<T>.Failed(ResourceFailureKind.Unsupported); }
        catch { return ResourceResult<T>.Failed(ResourceFailureKind.ProviderFailure); }
        finally
        {
            if (!delivered && produced is DirectoryPage { Continuation: { } token })
            {
                lock (_gate)
                {
                    if (_cursors.Remove(token.Value, out var abandoned)) abandoned.Dispose();
                }
            }
        }
    }

    private void ReclaimExpired()
    {
        foreach (var pair in _cursors.Where(p => p.Value.Expires <= Environment.TickCount64).ToArray())
        {
            _cursors.Remove(pair.Key);
            pair.Value.Dispose();
        }
    }
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            foreach (var cursor in _cursors.Values) cursor.Dispose();
            _cursors.Clear();
        }
    }
    private sealed class Failure(ResourceFailureKind kind) : Exception
    {
        internal ResourceFailureKind Kind { get; } = kind;
    }
    private sealed class Cursor(DirectoryListRequest request, WorkspacePath path, IEnumerator<string> enumerator) : IDisposable
    {
        internal IEnumerator<string> Enumerator { get; } = enumerator;
        internal string? Pending { get; set; }
        internal long Expires { get; set; }
        internal bool Matches(DirectoryListRequest other, WorkspacePath otherPath) => request.Workspace == other.Workspace &&
            string.Equals(path.Value, otherPath.Value, StringComparison.OrdinalIgnoreCase) &&
            request.MaxEntries == other.MaxEntries && request.MaxCandidatesScanned == other.MaxCandidatesScanned && request.MaxOutputBytes == other.MaxOutputBytes &&
            request.Invocation with { RequestIdentity = default } == other.Invocation with { RequestIdentity = default };
        public void Dispose() => Enumerator.Dispose();
    }
}
