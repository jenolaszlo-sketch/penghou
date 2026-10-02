namespace Penghou.IO.Abstractions;

/// <summary>Declared provider guarantees; these are descriptive and never grant authority.</summary>
public sealed record WorkspaceProviderCapabilities(string ReadProfile, string? WriteProfile,
    bool SupportsReads, bool SupportsConditionalWrites);

/// <summary>Per-session finite directory discovery ceilings.</summary>
public sealed record WorkspaceReaderOptions(int MaxEntries = 100_000,
    int MaxCandidatesScanned = 100_000, int MaxTotalCandidatesScanned = 100_000);

/// <summary>Finite original/verification read and time ceilings for conditional persistence.</summary>
public sealed record WorkspaceWriterOptions(int MaxOriginalBytes = 16 * 1024 * 1024,
    int MaxReadBytes = 32 * 1024 * 1024, int TimeoutMilliseconds = 30_000);

/// <summary>An owned reader session. Disposing it releases continuation resources.</summary>
public interface IWorkspaceReaderSession : IWorkspaceReader, IDisposable { }

/// <summary>Conditional bounded byte persistence, without language or text-edit semantics.</summary>
public interface IWorkspaceConditionalWriter
{
    ValueTask<ResourceResult<ResourceVersion>> WriteFileAsync(FileWriteRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Explicit host-selected workspace composition. Each session uses the supplied
/// policy-neutral boundary for every discovered resource. Writers retain the
/// supplied journal at the actual prepared/commit boundary. Implementations must
/// not replace these boundaries with ambient authority or a permissive default.
/// </summary>
public interface IWorkspaceProvider
{
    WorkspaceId Workspace { get; }
    WorkspaceProviderCapabilities Capabilities { get; }
    IWorkspaceReaderSession OpenReader(IResourceAuthorizer authorizer, WorkspaceReaderOptions? options = null);
    IWorkspaceConditionalWriter OpenWriter(IResourceAuthorizer authorizer,
        IResourceMutationJournal journal, WorkspaceWriterOptions? options = null);
}
