namespace Penghou.IO.Abstractions;

/// <summary>An opaque identity for a workspace selected by a trusted host.</summary>
public readonly record struct WorkspaceId(string Value);

/// <summary>A workspace-relative path using '/' as its separator.</summary>
public readonly record struct WorkspacePath(string Value)
{
    public static WorkspacePath Root { get; } = new("");
}

/// <summary>An absolute HTTP or HTTPS URL supplied as a concrete resource target.</summary>
public readonly record struct WebUrl(Uri Value);

/// <summary>A provider-issued version token used to make conditional mutations.</summary>
public readonly record struct ResourceVersion(string Value);

/// <summary>Opaque continuation state issued by a directory provider.</summary>
public readonly record struct DirectoryContinuation(string Value);

/// <summary>Identity of one concrete backend request minted by the trusted host.</summary>
public readonly record struct RequestIdentity(string Value);

/// <summary>
/// Host-authenticated context. Scope IDs are opaque references resolved by the
/// host; they are not grants or source-controlled tokens.
/// </summary>
public sealed record HostInvocation(
    string InvocationId,
    string SubjectId,
    string EffectId,
    string AttemptId,
    string EffectScopeId,
    string? ParentEffectScopeId,
    RequestIdentity RequestIdentity);

/// <summary>A finite byte bound for one in-memory file or response.</summary>
public sealed record IoLimits(int MaxBytes);

public enum WritePreconditionKind
{
    MustNotExist,
    MustMatchVersion
}

public sealed record WritePrecondition(WritePreconditionKind Kind, ResourceVersion? Version = null);

public sealed record FileWriteRequest(
    HostInvocation Invocation,
    WorkspaceId Workspace,
    WorkspacePath Path,
    ReadOnlyMemory<byte> Content,
    IoLimits Limits,
    WritePrecondition Precondition);

public sealed record FileDeleteRequest(
    HostInvocation Invocation,
    WorkspaceId Workspace,
    WorkspacePath Path,
    ResourceVersion ExpectedVersion);

public sealed record DirectoryCreateRequest(
    HostInvocation Invocation,
    WorkspaceId Workspace,
    WorkspacePath Path);

public sealed record FileMoveRequest(
    HostInvocation Invocation,
    WorkspaceId Workspace,
    WorkspacePath Source,
    WorkspacePath Destination,
    ResourceVersion ExpectedSourceVersion,
    WritePrecondition DestinationPrecondition);

public sealed record FileReadRequest(
    HostInvocation Invocation,
    WorkspaceId Workspace,
    WorkspacePath Path,
    IoLimits Limits);

public sealed record FileMetadataRequest(
    HostInvocation Invocation,
    WorkspaceId Workspace,
    WorkspacePath Path);

public sealed record DirectoryListRequest(
    HostInvocation Invocation,
    WorkspaceId Workspace,
    WorkspacePath Path,
    int MaxEntries,
    int MaxCandidatesScanned,
    int MaxOutputBytes,
    DirectoryContinuation? Continuation = null);

public sealed record WebReadRequest(
    HostInvocation Invocation,
    WebUrl Url,
    IoLimits Limits);
