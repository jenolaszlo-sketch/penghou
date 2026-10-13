using System.Net;

namespace Penghou.IO.Abstractions;

public enum ResourceAction
{
    ReadFile,
    ReadMetadata,
    ListDirectory,
    WriteFile,
    PatchFile,
    DeleteFile,
    CreateDirectory,
    MoveFile,
    ReadWebResource
}

/// <summary>A concrete resource binding; workspace resources always include their workspace.</summary>
public abstract record ResourceBinding
{
    private ResourceBinding() { }

    public sealed record WorkspaceFile(WorkspaceId Workspace, WorkspacePath Path) : ResourceBinding;
    public sealed record WorkspaceDirectory(WorkspaceId Workspace, WorkspacePath Path) : ResourceBinding;
    public sealed record WorkspaceEntry(WorkspaceId Workspace, WorkspacePath Path) : ResourceBinding;
    public sealed record Web(WebUrl Url) : ResourceBinding;
    public sealed record WebEndpoint(WebUrl Url, IPAddress Address, int Port) : ResourceBinding;
}

/// <summary>A single authorization check for one concrete action and resource.</summary>
public sealed record ResourceAuthorizationRequest(
    HostInvocation Invocation,
    RequestIdentity SnapshotRequestIdentity,
    ResourceAction Action,
    ResourceBinding Resource);

public enum AuthorizationStatus
{
    Unavailable,
    Deny,
    Permit
}

public sealed record ResourceAuthorizationDecision(AuthorizationStatus Status, string? Reason = null);

/// <summary>
/// The trusted host's authorization boundary. Implementations must authenticate
/// the invocation, match request identity, and enforce inherited scope ceilings.
/// </summary>
public interface IResourceAuthorizer
{
    ValueTask<ResourceAuthorizationDecision> AuthorizeAsync(
        ResourceAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}

public enum ResourceFailureKind
{
    NotFound,
    AlreadyExists,
    AuthorizationDenied,
    AuthorizationUnavailable,
    AccessDenied,
    InvalidPath,
    InvalidRequest,
    PreconditionFailed,
    TooLarge,
    AmbiguousOutcome,
    Unsupported,
    ProviderFailure
}

/// <summary>A typed operation result that does not expose provider exceptions or handles.</summary>
public sealed record ResourceResult<T>(T? Value, ResourceFailureKind? Failure, string? Detail = null)
{
    public bool Succeeded => Failure is null;

    public static ResourceResult<T> Success(T value) => new(value, null);
    public static ResourceResult<T> Failed(ResourceFailureKind failure, string? detail = null) => new(default, failure, detail);
}

public sealed record FileReadResult(ReadOnlyMemory<byte> Content, ResourceVersion Version);

public sealed record FileMetadata(bool Exists, long? Length, ResourceVersion? Version);

public sealed record DirectoryEntry(string Name, bool IsDirectory, long? Length, ResourceVersion? Version);

public enum DirectoryTruncationReason
{
    EntryLimit,
    CandidateScanLimit,
    OutputByteLimit
}

/// <summary>
/// An authorized-only page. Incomplete pages state their reason and continuation;
/// a denied candidate is never included.
/// </summary>
public sealed record DirectoryPage(
    IReadOnlyList<DirectoryEntry> Entries,
    bool IsComplete,
    DirectoryContinuation? Continuation,
    DirectoryTruncationReason? TruncationReason);

public sealed record WebReadResult(int StatusCode, WebUrl EffectiveUrl, ReadOnlyMemory<byte> Content);

/// <summary>Read-only bounded workspace contracts; no raw streams are returned.</summary>
public interface IWorkspaceReader
{
    ValueTask<ResourceResult<FileReadResult>> ReadFileAsync(FileReadRequest request, CancellationToken cancellationToken = default);
    ValueTask<ResourceResult<FileMetadata>> GetFileMetadataAsync(FileMetadataRequest request, CancellationToken cancellationToken = default);
    ValueTask<ResourceResult<DirectoryPage>> ListDirectoryAsync(DirectoryListRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Bounded workspace mutation contracts.</summary>
public interface IWorkspaceWriter
{
    ValueTask<ResourceResult<ResourceVersion>> WriteFileAsync(FileWriteRequest request, CancellationToken cancellationToken = default);
    ValueTask<ResourceResult<bool>> DeleteFileAsync(FileDeleteRequest request, CancellationToken cancellationToken = default);
    ValueTask<ResourceResult<bool>> CreateDirectoryAsync(DirectoryCreateRequest request, CancellationToken cancellationToken = default);
    ValueTask<ResourceResult<ResourceVersion>> MoveFileAsync(FileMoveRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Complete workspace contract for providers supporting reads and mutations.</summary>
public interface IWorkspaceFileSystem : IWorkspaceReader, IWorkspaceWriter
{
}

/// <summary>Bounded, credential-free GET retrieval of an explicitly bound HTTP or HTTPS resource.</summary>
public interface IWebResourceReader
{
    ValueTask<ResourceResult<WebReadResult>> ReadAsync(WebReadRequest request, CancellationToken cancellationToken = default);
}
