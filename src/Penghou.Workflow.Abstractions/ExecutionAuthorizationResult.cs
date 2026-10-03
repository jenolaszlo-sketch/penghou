using System.Text.Json.Serialization;

namespace Penghou.Workflow.Abstractions;

/// <summary>Closed execution preflight outcomes; zero is fail-closed.</summary>
public enum ExecutionAuthorizationDecision
{
    /// <summary>No usable authority decision is available.</summary>
    Unavailable = 0,
    /// <summary>Fresh preflight permission, subject to context binding, evidence and runtime fencing.</summary>
    Allowed = 1,
    /// <summary>Authority explicitly denies the requested operation.</summary>
    Denied = 2,
    /// <summary>A trusted approval process must complete before fresh reauthorization.</summary>
    ApprovalRequired = 3,
    /// <summary>The provider encountered a non-permission error.</summary>
    Error = 4
}

/// <summary>An immutable, attributable response to exactly one authorization evaluation.</summary>
/// <remarks>Decision/evidence/correlation IDs are references, not bearer tokens or proof of an external effect.</remarks>
public sealed record ExecutionAuthorizationResult
{
    /// <summary>Creates a closed response. Allowed requires an expiry; approval-required requires correlation.</summary>
    /// <param name="decision">Known outcome; unknown values are rejected.</param>
    /// <param name="authorizationRequestId">Exact request ID echoed from the evaluated context.</param>
    /// <param name="providerId">Configured provider identity, independently checked by the runtime.</param>
    /// <param name="decisionId">Unique attributable outcome ID.</param>
    /// <param name="evaluatedAt">Nondefault evaluation instant, normalized to UTC.</param>
    /// <param name="expiresAt">Expiry after evaluation, mandatory for Allowed; the runtime checks current time.</param>
    /// <param name="approvalRequestId">Durable approval correlation, mandatory only for ApprovalRequired.</param>
    /// <param name="reasonCode">Optional bounded diagnostic code; never interpreted as permission or approval.</param>
    /// <param name="evidenceId">Optional provider evidence reference; required evidence rules remain host/runtime-owned.</param>
    /// <param name="schemaVersion">Result schema version; only version one is supported.</param>
    [JsonConstructor]
    public ExecutionAuthorizationResult(ExecutionAuthorizationDecision decision, string authorizationRequestId,
        string providerId, string decisionId, DateTimeOffset evaluatedAt, DateTimeOffset? expiresAt = null,
        string? approvalRequestId = null, string? reasonCode = null, string? evidenceId = null, int schemaVersion = 1)
    {
        if (!Enum.IsDefined(decision)) throw new ArgumentOutOfRangeException(nameof(decision));
        if (schemaVersion != 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion), "Unsupported result schema version.");
        Decision = decision;
        AuthorizationRequestId = ContractBounds.Required(authorizationRequestId, nameof(authorizationRequestId));
        ProviderId = ContractBounds.Required(providerId, nameof(providerId));
        DecisionId = ContractBounds.Required(decisionId, nameof(decisionId));
        if (evaluatedAt == default) throw new ArgumentOutOfRangeException(nameof(evaluatedAt));
        EvaluatedAt = evaluatedAt.ToUniversalTime();
        if (expiresAt is not null && expiresAt <= evaluatedAt)
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Expiry must be after evaluation.");
        if (decision == ExecutionAuthorizationDecision.Allowed && expiresAt is null)
            throw new ArgumentException("Allowed requires a bounded expiry.", nameof(expiresAt));
        ExpiresAt = expiresAt?.ToUniversalTime();
        ApprovalRequestId = ContractBounds.Optional(approvalRequestId, nameof(approvalRequestId));
        if ((decision == ExecutionAuthorizationDecision.ApprovalRequired) != (ApprovalRequestId is not null))
            throw new ArgumentException("Only ApprovalRequired requires an approval correlation ID.", nameof(approvalRequestId));
        ReasonCode = ContractBounds.Optional(reasonCode, nameof(reasonCode));
        EvidenceId = ContractBounds.Optional(evidenceId, nameof(evidenceId));
    }

    /// <summary>Gets the supported result schema version.</summary>
    public int SchemaVersion => 1;
    /// <summary>Gets the closed outcome.</summary>
    public ExecutionAuthorizationDecision Decision { get; }
    /// <summary>Gets the exact evaluated authorization request ID.</summary>
    public string AuthorizationRequestId { get; }
    /// <summary>Gets the provider identity checked against runtime configuration.</summary>
    public string ProviderId { get; }
    /// <summary>Gets the unique attributable outcome ID.</summary>
    public string DecisionId { get; }
    /// <summary>Gets the UTC evaluation instant.</summary>
    public DateTimeOffset EvaluatedAt { get; }
    /// <summary>Gets the optional UTC expiry, mandatory for Allowed.</summary>
    public DateTimeOffset? ExpiresAt { get; }
    /// <summary>Gets the optional durable approval correlation.</summary>
    public string? ApprovalRequestId { get; }
    /// <summary>Gets the optional bounded diagnostic code.</summary>
    public string? ReasonCode { get; }
    /// <summary>Gets the optional provider evidence reference.</summary>
    public string? EvidenceId { get; }
}
