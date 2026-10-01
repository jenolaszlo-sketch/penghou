using System.Security.Cryptography;
using System.Text;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Penghou.IO.Abstractions;
using Penghou.IO.Local;
using Xunit;

namespace Penghou.IO.Local.Tests;

public sealed class LocalWorkspaceReaderTests
{
    private static readonly WorkspaceId Workspace = new("test-workspace");

    [Fact]
    public async Task ReadFile_AuthorizesBeforeOpening_AndReturnsBoundedContentVersion()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("note.txt", "hello");
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);
        var request = Read(temp.Invocation, "note.txt", 5);

        var result = await reader.ReadFileAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal("hello", Encoding.UTF8.GetString(result.Value!.Content.Span));
        Assert.Equal("local-read-v1:sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("hello"))), result.Value.Version.Value);
        Assert.Contains(auth.Requests, r => r.Action == ResourceAction.ReadFile &&
            r.Resource == new ResourceBinding.WorkspaceFile(Workspace, new WorkspacePath("note.txt")));
        Assert.All(auth.Requests, r => Assert.Equal(request.Invocation.RequestIdentity, r.SnapshotRequestIdentity));
        AssertRootAndAncestorsChecked(auth.Requests);
    }

    [Fact]
    public async Task ReadFile_RejectsOversizedAndCancelledRequests()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("note.txt", "12345");
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth, new LocalReaderOptions { MaxFileBytes = 4 });

        var oversized = await reader.ReadFileAsync(Read(temp.Invocation, "note.txt", 4));
        Assert.Equal(ResourceFailureKind.TooLarge, oversized.Failure);
        Assert.Contains(auth.Requests, r => r.Action == ResourceAction.ReadFile);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await reader.ReadFileAsync(Read(temp.Invocation, "note.txt", 4), cancellation.Token));
    }

    [Fact]
    public async Task ReadLimitAboveProviderProfileIsInvalidBeforeAuthorization()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("note.txt", "data");
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth, new LocalReaderOptions { MaxFileBytes = 4 });

        var result = await reader.ReadFileAsync(Read(temp.Invocation, "note.txt", 5));

        Assert.Equal(ResourceFailureKind.InvalidRequest, result.Failure);
        Assert.Empty(auth.Requests);
    }

    [Fact]
    public async Task Metadata_IsAuthorizedBeforeProbe_AndDoesNotReadContentVersion()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("note.txt", new string('x', 128));
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);
        var request = Metadata(temp.Invocation, "note.txt");

        var result = await reader.GetFileMetadataAsync(request);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.Exists);
        Assert.Equal(128, result.Value.Length);
        Assert.Null(result.Value.Version);
        Assert.Contains(auth.Requests, r => r.Action == ResourceAction.ReadMetadata &&
            r.Resource == new ResourceBinding.WorkspaceFile(Workspace, new WorkspacePath("note.txt")));
        AssertRootAndAncestorsChecked(auth.Requests);
    }

    [Fact]
    public async Task Metadata_DoesNotDiscloseWhetherDeniedTargetExists()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("present.txt", "secret");
        var auth = new RecordingAuthorizer(r => r.Resource is ResourceBinding.WorkspaceFile
            ? new ResourceAuthorizationDecision(AuthorizationStatus.Deny)
            : new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);

        var present = await reader.GetFileMetadataAsync(Metadata(temp.Invocation, "present.txt"));
        var absent = await reader.GetFileMetadataAsync(Metadata(temp.Invocation, "absent.txt"));

        Assert.Equal(ResourceFailureKind.AuthorizationDenied, present.Failure);
        Assert.Equal(present.Failure, absent.Failure);
        Assert.Equal(2, auth.Requests.Count(r => r.Resource is ResourceBinding.WorkspaceFile));
    }

    [Theory]
    [InlineData(AuthorizationStatus.Unavailable)]
    [InlineData((AuthorizationStatus)999)]
    public async Task AuthorizationUnavailableOrUnknown_FailsClosedBeforeProbe(AuthorizationStatus status)
    {
        using var temp = new TemporaryWorkspace();
        var auth = new RecordingAuthorizer(_ => new ResourceAuthorizationDecision(status));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);

        var result = await reader.GetFileMetadataAsync(Metadata(temp.Invocation, "missing.txt"));

        Assert.Equal(ResourceFailureKind.AuthorizationUnavailable, result.Failure);
        Assert.NotEmpty(auth.Requests);
    }

    [Fact]
    public async Task AuthorizerExceptionAndNullDecision_FailClosed()
    {
        using var temp = new TemporaryWorkspace();
        using var throwing = new LocalWorkspaceReader(Workspace, temp.Root, new RecordingAuthorizer(_ => throw new InvalidOperationException()));
        var thrown = await throwing.GetFileMetadataAsync(Metadata(temp.Invocation, "missing.txt"));
        Assert.Equal(ResourceFailureKind.AuthorizationUnavailable, thrown.Failure);

        using var nullDecision = new LocalWorkspaceReader(Workspace, temp.Root, new RecordingAuthorizer(_ => null));
        var nullResult = await nullDecision.GetFileMetadataAsync(Metadata(temp.Invocation, "missing.txt"));
        Assert.Equal(ResourceFailureKind.AuthorizationUnavailable, nullResult.Failure);
    }

    [Fact]
    public async Task IdentityMismatchAndMalformedInvocation_AreRejectedBeforeAuthorization()
    {
        using var temp = new TemporaryWorkspace();
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);
        var valid = Read(temp.Invocation, "note.txt", 10);
        var mismatch = valid with { Invocation = valid.Invocation with { RequestIdentity = new RequestIdentity("forged") } };

        var mismatchResult = await reader.ReadFileAsync(mismatch);
        Assert.Equal(ResourceFailureKind.AuthorizationDenied, mismatchResult.Failure);
        Assert.Empty(auth.Requests);

        var malformed = valid with { Invocation = valid.Invocation with { SubjectId = " " } };
        var malformedResult = await reader.ReadFileAsync(malformed);
        Assert.Equal(ResourceFailureKind.InvalidRequest, malformedResult.Failure);
        Assert.Empty(auth.Requests);
    }

    [Fact]
    public async Task AuthorizerRejectingUnmappedInvocationPreventsAnyResourceProbe()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("present.txt", "sensitive");
        var auth = new RecordingAuthorizer(r => r.Invocation.InvocationId == "forged"
            ? new ResourceAuthorizationDecision(AuthorizationStatus.Deny)
            : new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);
        var invocation = Invocation() with { InvocationId = "forged" };
        var result = await reader.GetFileMetadataAsync(Metadata(invocation, "present.txt"));

        Assert.Equal(ResourceFailureKind.AuthorizationDenied, result.Failure);
        Assert.Single(auth.Requests);
        Assert.Equal("forged", auth.Requests[0].Invocation.InvocationId);
    }

    [Fact]
    public async Task AncestorDenialPreventsLeafMetadataProbe()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("folder/present.txt", "sensitive");
        var auth = new RecordingAuthorizer(r => r.Resource is ResourceBinding.WorkspaceEntry entry && entry.Path.Value == "folder"
            ? new ResourceAuthorizationDecision(AuthorizationStatus.Deny)
            : new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);

        var result = await reader.GetFileMetadataAsync(Metadata(temp.Invocation, "folder/present.txt"));

        Assert.Equal(ResourceFailureKind.AuthorizationDenied, result.Failure);
        Assert.DoesNotContain(auth.Requests, r => r.Resource == new ResourceBinding.WorkspaceEntry(Workspace, new WorkspacePath("folder/present.txt")));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("C:/outside.txt")]
    [InlineData("a\\b.txt")]
    [InlineData("a/../../b")]
    public async Task InvalidPathsFailWithoutAuthorizationOrDisclosure(string path)
    {
        using var temp = new TemporaryWorkspace();
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);

        var result = await reader.GetFileMetadataAsync(new FileMetadataRequest(temp.Invocation, Workspace, new WorkspacePath(path)));

        Assert.Equal(ResourceFailureKind.InvalidPath, result.Failure);
        Assert.Empty(auth.Requests);
    }

    [Fact]
    public async Task ListingChecksDirectoryAndEveryCandidateBeforeInspection_OmitsDeniedEntries()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("allowed.txt", "ok");
        temp.Write("hidden.txt", "no");
        var auth = new RecordingAuthorizer(r => r.Resource is ResourceBinding.WorkspaceEntry e && e.Path.Value == "hidden.txt"
            ? new ResourceAuthorizationDecision(AuthorizationStatus.Deny)
            : new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);

        var page = await reader.ListDirectoryAsync(List(temp.Invocation, "", maxEntries: 10, maxCandidates: 10, maxBytes: 1024));

        Assert.True(page.Succeeded);
        Assert.Equal(new[] { "allowed.txt" }, page.Value!.Entries.Select(e => e.Name));
        Assert.True(page.Value.IsComplete);
        Assert.Null(page.Value.Continuation);
        Assert.Equal(ResourceAction.ListDirectory, auth.Requests[0].Action);
        Assert.All(auth.Requests.Skip(1), r => Assert.IsType<ResourceBinding.WorkspaceEntry>(r.Resource));
    }

    [Fact]
    public async Task ListingBoundsPageAndContinuationIsSingleUseAndContextBound()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("a.txt", "a"); temp.Write("b.txt", "b"); temp.Write("c.txt", "c");
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);
        var first = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 100, 1024));

        Assert.True(first.Succeeded);
        Assert.Single(first.Value!.Entries);
        Assert.False(first.Value.IsComplete);
        Assert.Equal(DirectoryTruncationReason.EntryLimit, first.Value.TruncationReason);
        Assert.NotNull(first.Value.Continuation);

        var continuation = first.Value.Continuation!.Value;
        var wrongSubject = await reader.ListDirectoryAsync(List(Invocation("other-subject"), "", 1, 100, 1024, continuation));
        Assert.Equal(ResourceFailureKind.AuthorizationDenied, wrongSubject.Failure);

        var next = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 100, 1024, continuation));
        Assert.True(next.Succeeded);
        Assert.True(next.Value!.Entries.Count > 0);
        Assert.DoesNotContain(first.Value.Entries[0].Name, next.Value.Entries.Select(e => e.Name));

        var replay = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 100, 1024, continuation));
        Assert.Equal(ResourceFailureKind.AuthorizationDenied, replay.Failure);
    }

    [Fact]
    public async Task ListingRejectsTamperedContinuation_AndRechecksAuthorizationOnNextPage()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("a.txt", "a"); temp.Write("b.txt", "b");
        var denyEntries = false;
        var auth = new RecordingAuthorizer(r => denyEntries && r.Resource is ResourceBinding.WorkspaceEntry
            ? new ResourceAuthorizationDecision(AuthorizationStatus.Deny)
            : new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);
        var first = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 100, 1024));
        Assert.NotNull(first.Value!.Continuation);

        var token = first.Value.Continuation!.Value;
        var tampered = new DirectoryContinuation(token.Value + "x");
        var rejected = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 100, 1024, tampered));
        Assert.Equal(ResourceFailureKind.AuthorizationDenied, rejected.Failure);

        denyEntries = true;
        var before = auth.Requests.Count;
        var next = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 100, 1024, token));
        Assert.True(next.Succeeded);
        Assert.True(next.Value!.IsComplete);
        Assert.Empty(next.Value.Entries);
        Assert.True(auth.Requests.Count > before);
    }

    [Fact]
    public async Task ListingCandidateBudgetProgressesAcrossDeniedCandidatesWithoutCountingThemAsEntries()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("a.txt", "a"); temp.Write("b.txt", "b"); temp.Write("c.txt", "c");
        var auth = new RecordingAuthorizer(r => r.Resource is ResourceBinding.WorkspaceEntry e && e.Path.Value.Length > 0
            ? new ResourceAuthorizationDecision(AuthorizationStatus.Deny)
            : new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);
        var page = await reader.ListDirectoryAsync(List(temp.Invocation, "", 10, 1, 1024));

        Assert.True(page.Succeeded);
        Assert.Empty(page.Value!.Entries);
        Assert.False(page.Value.IsComplete);
        Assert.Equal(DirectoryTruncationReason.CandidateScanLimit, page.Value.TruncationReason);
        Assert.NotNull(page.Value.Continuation);
    }

    [Fact]
    public async Task TotalCandidateBudgetPersistsAcrossListsAndChargesDeniedCandidatesAndExhaustionProbe()
    {
        using var temp = new TemporaryWorkspace();
        foreach (var directory in new[] { "one", "two", "three", "four" })
            temp.Write(Path.Combine(directory, "hidden.txt"), "private");
        var auth = new RecordingAuthorizer(r => r.Resource is ResourceBinding.WorkspaceEntry entry &&
            entry.Path.Value.EndsWith("/hidden.txt", StringComparison.Ordinal)
                ? new ResourceAuthorizationDecision(AuthorizationStatus.Deny)
                : new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth,
            new LocalReaderOptions { MaxTotalCandidatesScanned = 5 });

        var first = await reader.ListDirectoryAsync(List(temp.Invocation, "one", 10, 10, 1024));
        var second = await reader.ListDirectoryAsync(List(temp.Invocation, "two", 10, 10, 1024));
        Assert.True(first.Succeeded && first.Value!.IsComplete);
        Assert.Empty(first.Value.Entries);
        Assert.True(second.Succeeded && second.Value!.IsComplete);
        Assert.Empty(second.Value.Entries);

        var exhaustedPage = await reader.ListDirectoryAsync(List(temp.Invocation, "three", 10, 10, 1024));
        Assert.True(exhaustedPage.Succeeded);
        Assert.Empty(exhaustedPage.Value!.Entries);
        Assert.False(exhaustedPage.Value.IsComplete);
        Assert.Equal(DirectoryTruncationReason.CandidateScanLimit, exhaustedPage.Value.TruncationReason);
        Assert.False(string.IsNullOrWhiteSpace(exhaustedPage.Value.Continuation!.Value.Value));

        var beyondBudget = await reader.ListDirectoryAsync(List(temp.Invocation, "four", 10, 10, 1024));
        Assert.Equal(ResourceFailureKind.TooLarge, beyondBudget.Failure);
    }

    [Fact]
    public async Task UnavailableCandidateAuthorizationAbortsListing()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("candidate.txt", "data");
        var auth = new RecordingAuthorizer(r => r.Resource is ResourceBinding.WorkspaceEntry e && e.Path.Value.Length > 0
            ? new ResourceAuthorizationDecision(AuthorizationStatus.Unavailable)
            : new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);

        var result = await reader.ListDirectoryAsync(List(temp.Invocation, "", 10, 10, 1024));

        Assert.Equal(ResourceFailureKind.AuthorizationUnavailable, result.Failure);
        Assert.Contains(auth.Requests, r => r.Resource is ResourceBinding.WorkspaceEntry e && e.Path.Value == "candidate.txt");
    }

    [Fact]
    public async Task ContinuationCursorCapReturnsTooLargeInsteadOfDroppingState()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("a.txt", "a"); temp.Write("b.txt", "b");
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth, new LocalReaderOptions { MaxContinuations = 1 });

        var first = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 10, 1024));
        Assert.True(first.Succeeded);
        var second = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 10, 1024));
        Assert.Equal(ResourceFailureKind.TooLarge, second.Failure);
    }

    [Fact]
    public async Task ExpiredContinuationIsRejected()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("a.txt", "a"); temp.Write("b.txt", "b");
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth, new LocalReaderOptions { ContinuationLifetime = TimeSpan.FromMilliseconds(50) });
        var first = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 10, 1024));
        Assert.NotNull(first.Value!.Continuation);
        await Task.Delay(150);

        var expired = await reader.ListDirectoryAsync(List(temp.Invocation, "", 1, 10, 1024, first.Value.Continuation));

        Assert.Equal(ResourceFailureKind.AuthorizationDenied, expired.Failure);
    }

    [Fact]
    public async Task OperationDeadlineCancelsStalledAuthorization()
    {
        using var temp = new TemporaryWorkspace();
        var auth = new RecordingAuthorizer(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new ResourceAuthorizationDecision(AuthorizationStatus.Permit);
        });
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth, new LocalReaderOptions { OperationTimeout = TimeSpan.FromMilliseconds(50) });

        var result = await reader.GetFileMetadataAsync(Metadata(temp.Invocation, "missing.txt"));

        Assert.Equal(ResourceFailureKind.ProviderFailure, result.Failure);
    }

    [Fact]
    public async Task ListingOutputByteBoundReturnsIncompletePage()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("a-long-entry-name.txt", "a");
        temp.Write("b-long-entry-name.txt", "b");
        var auth = new RecordingAuthorizer();
        using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);

        var page = await reader.ListDirectoryAsync(List(temp.Invocation, "", 10, 100, 60));

        Assert.True(page.Succeeded);
        Assert.False(page.Value!.IsComplete);
        Assert.Equal(DirectoryTruncationReason.OutputByteLimit, page.Value.TruncationReason);
        Assert.NotNull(page.Value.Continuation);
        Assert.Single(page.Value.Entries);
    }

    [Fact]
    public async Task ListingOmitsObservedReparsePointCandidates()
    {
        using var temp = new TemporaryWorkspace();
        temp.Write("ordinary.txt", "ok");
        var outside = Path.Combine(Path.GetTempPath(), "penghou-junction-target-" + Guid.NewGuid().ToString("N"));
        var junction = Path.Combine(temp.Root, "linked-directory");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "secret.txt"), "outside");
        try
        {
            CreateJunction(junction, outside);
            var auth = new RecordingAuthorizer();
            using var reader = new LocalWorkspaceReader(Workspace, temp.Root, auth);
            var page = await reader.ListDirectoryAsync(List(temp.Invocation, "", 10, 20, 4096));
            Assert.True(page.Succeeded);
            Assert.DoesNotContain(page.Value!.Entries, e => e.Name == "linked-directory");
        }
        finally
        {
            DeleteGuardedJunction(junction, temp.Root);
            DeleteGuardedTargetDirectory(outside);
        }
    }

    private static void CreateJunction(string junctionPath, string targetPath)
    {
        Directory.CreateDirectory(junctionPath);
        var substituteName = @"\??\" + Path.GetFullPath(targetPath);
        var printName = Path.GetFullPath(targetPath);
        var substituteBytes = Encoding.Unicode.GetBytes(substituteName + "\0");
        var printBytes = Encoding.Unicode.GetBytes(printName + "\0");
        var pathBytes = substituteBytes.Length + printBytes.Length;
        var dataLength = 8 + pathBytes;
        var buffer = new byte[8 + dataLength];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), 0xA0000003); // IO_REPARSE_TAG_MOUNT_POINT
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(4, 2), checked((ushort)dataLength));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(8, 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(10, 2), checked((ushort)(substituteBytes.Length - 2)));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(12, 2), checked((ushort)substituteBytes.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(14, 2), checked((ushort)(printBytes.Length - 2)));
        substituteBytes.CopyTo(buffer, 16);
        printBytes.CopyTo(buffer, 16 + substituteBytes.Length);

        using var handle = CreateFileW(junctionPath, 0x40000000, 0x00000007, IntPtr.Zero, 3, 0x00200000 | 0x02000000, IntPtr.Zero);
        if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to open junction directory for reparse setup.");
        if (!DeviceIoControl(handle, 0x000900A4, buffer, buffer.Length, null, 0, out _, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install test directory junction.");
    }

    private static void DeleteGuardedTargetDirectory(string targetPath)
    {
        var fullPath = Path.GetFullPath(targetPath);
        var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(fullPath).StartsWith("penghou-junction-target-", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to clean an unexpected junction target path.");
        if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
    }

    private static void DeleteGuardedJunction(string junctionPath, string workspaceRoot)
    {
        var fullPath = Path.GetFullPath(junctionPath);
        var fullRoot = Path.GetFullPath(workspaceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFileName(fullPath), "linked-directory", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to clean an unexpected junction path.");
        if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: false);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(SafeFileHandle device, uint controlCode, byte[] inputBuffer, int inputBufferSize,
        byte[]? outputBuffer, int outputBufferSize, out int bytesReturned, IntPtr overlapped);

    private static FileReadRequest Read(HostInvocation invocation, string path, int maxBytes)
    {
        var request = new FileReadRequest(invocation, Workspace, new WorkspacePath(path), new IoLimits(maxBytes));
        return request with { Invocation = invocation with { RequestIdentity = ResourceRequestIdentity.Compute(request) } };
    }

    private static FileMetadataRequest Metadata(HostInvocation invocation, string path)
    {
        var request = new FileMetadataRequest(invocation, Workspace, new WorkspacePath(path));
        return request with { Invocation = invocation with { RequestIdentity = ResourceRequestIdentity.Compute(request) } };
    }

    private static DirectoryListRequest List(HostInvocation invocation, string path, int maxEntries, int maxCandidates, int maxBytes, DirectoryContinuation? continuation = null)
    {
        var request = new DirectoryListRequest(invocation, Workspace, new WorkspacePath(path), maxEntries, maxCandidates, maxBytes, continuation);
        return request with { Invocation = invocation with { RequestIdentity = ResourceRequestIdentity.Compute(request) } };
    }

    private static HostInvocation Invocation(string subject = "test-subject") => new(
        "test-invocation", subject, "test-effect", "test-attempt", "test-scope", null, new RequestIdentity("pending"));

    private static void AssertRootAndAncestorsChecked(IEnumerable<ResourceAuthorizationRequest> requests)
    {
        Assert.Contains(requests, r => r.Action == ResourceAction.ReadMetadata &&
            r.Resource == new ResourceBinding.WorkspaceEntry(Workspace, WorkspacePath.Root));
    }

    private sealed class RecordingAuthorizer : IResourceAuthorizer
    {
        private readonly Func<ResourceAuthorizationRequest, ResourceAuthorizationDecision?>? _decide;
        private readonly Func<ResourceAuthorizationRequest, CancellationToken, ValueTask<ResourceAuthorizationDecision?>>? _asyncDecide;

        internal RecordingAuthorizer(Func<ResourceAuthorizationRequest, ResourceAuthorizationDecision?>? decide = null) => _decide = decide;
        internal RecordingAuthorizer(Func<ResourceAuthorizationRequest, CancellationToken, ValueTask<ResourceAuthorizationDecision?>> asyncDecide) => _asyncDecide = asyncDecide;

        public List<ResourceAuthorizationRequest> Requests { get; } = [];

        public async ValueTask<ResourceAuthorizationDecision> AuthorizeAsync(ResourceAuthorizationRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            if (_asyncDecide is not null) return (await _asyncDecide(request, cancellationToken))!;
            if (_decide is null) return new ResourceAuthorizationDecision(AuthorizationStatus.Permit);
            return _decide(request)!;
        }
    }

    private sealed class TemporaryWorkspace : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "penghou-local-reader-tests-" + Guid.NewGuid().ToString("N"));
        public string Root => _root;
        public HostInvocation Invocation { get; } = Invocation();

        public TemporaryWorkspace() => Directory.CreateDirectory(_root);

        public void Write(string relativePath, string content)
        {
            var path = Path.Combine(_root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        public void Dispose()
        {
            var fullRoot = Path.GetFullPath(_root);
            var expectedParent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullRoot.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(fullRoot).StartsWith("penghou-local-reader-tests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to clean an unexpected temporary workspace path.");
            if (Directory.Exists(fullRoot)) Directory.Delete(fullRoot, recursive: true);
        }
    }
}
