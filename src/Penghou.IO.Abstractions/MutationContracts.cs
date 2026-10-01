namespace Penghou.IO.Abstractions;

/// <summary>Exact prepared operation at the provider's locked-object boundary.</summary>
public sealed record MutationStartRequest(HostInvocation Invocation, RequestIdentity RequestIdentity,
    WorkspaceId Workspace, WorkspacePath Path, string ProviderProfile, string ObjectIdentity,
    ResourceVersion OriginalVersion, ResourceVersion ProposedVersion,
    int OriginalByteLength, int ProposedByteLength);

public enum MutationStartStatus { Unavailable, Deny, Started, AlreadyStarted }

/// <summary>The host commits mandatory start evidence and serializes current authority/fence checks.</summary>
public sealed record MutationStartDecision(MutationStartStatus Status, string? EvidenceId = null);

public enum MutationOutcome { NoMutation, Completed, Ambiguous }
public sealed record MutationCompletion(MutationStartRequest Start, string EvidenceId,
    MutationOutcome Outcome, ResourceVersion? ObservedVersion);

/// <summary>
/// Required trusted-host boundary, not a permit token. Start must atomically check
/// current admission/revision/fence and record start before returning Started.
/// Repeated or conflicting operations must never return Started again. Complete
/// records an attributable outcome; uncertain operations require reconciliation.
/// </summary>
public interface IResourceMutationJournal
{
    ValueTask<MutationStartDecision> StartAsync(MutationStartRequest request, CancellationToken cancellationToken = default);
    ValueTask<bool> CompleteAsync(MutationCompletion completion, CancellationToken cancellationToken = default);
}
