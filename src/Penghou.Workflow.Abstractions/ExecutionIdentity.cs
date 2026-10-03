using System.Text.Json.Serialization;

namespace Penghou.Workflow.Abstractions;

/// <summary>Immutable, host-scoped identity of one concrete operation attempt.</summary>
/// <remarks>These values do not authenticate a principal or establish a runtime fence by themselves.</remarks>
public sealed record ExecutionIdentity
{
    /// <summary>Creates a neutral identity. IDs are opaque, ordinal, and bounded to 256 UTF-8 bytes.</summary>
    /// <param name="executionId">Execution ID scoped to the trusted host's namespace.</param>
    /// <param name="operationId">Stable operation ID within that execution.</param>
    /// <param name="attempt">One-based actual operation attempt.</param>
    /// <param name="executionRevision">Opaque runtime freshness token; changes when the effective execution ownership/revision changes.</param>
    /// <param name="parentExecutionId">Optional distinct parent execution ID in the same host namespace.</param>
    /// <param name="operationPath">Optional logical operation path, bounded to 2048 UTF-8 bytes.</param>
    /// <param name="planId">Optional exact plan ID, paired with its revision.</param>
    /// <param name="planRevision">Optional exact plan revision, paired with its ID.</param>
    [JsonConstructor]
    public ExecutionIdentity(string executionId, string operationId, int attempt, string executionRevision,
        string? parentExecutionId = null, string? operationPath = null, string? planId = null, string? planRevision = null)
    {
        ExecutionId = ContractBounds.Required(executionId, nameof(executionId));
        OperationId = ContractBounds.Required(operationId, nameof(operationId));
        if (attempt < 1) throw new ArgumentOutOfRangeException(nameof(attempt), "Attempts are one-based.");
        Attempt = attempt;
        ExecutionRevision = ContractBounds.Required(executionRevision, nameof(executionRevision));
        ParentExecutionId = ContractBounds.Optional(parentExecutionId, nameof(parentExecutionId));
        if (string.Equals(ExecutionId, ParentExecutionId, StringComparison.Ordinal))
            throw new ArgumentException("An execution cannot be its own parent.", nameof(parentExecutionId));
        OperationPath = ContractBounds.Optional(operationPath, nameof(operationPath), 2048);
        PlanId = ContractBounds.Optional(planId, nameof(planId));
        PlanRevision = ContractBounds.Optional(planRevision, nameof(planRevision));
        if ((PlanId is null) != (PlanRevision is null))
            throw new ArgumentException("Plan ID and revision must be supplied together.", nameof(planRevision));
    }

    /// <summary>Gets the host-scoped execution ID.</summary>
    public string ExecutionId { get; }
    /// <summary>Gets the optional parent execution ID.</summary>
    public string? ParentExecutionId { get; }
    /// <summary>Gets the stable operation ID.</summary>
    public string OperationId { get; }
    /// <summary>Gets the optional logical operation path.</summary>
    public string? OperationPath { get; }
    /// <summary>Gets the one-based actual operation attempt.</summary>
    public int Attempt { get; }
    /// <summary>Gets the runtime-owned opaque freshness token.</summary>
    public string ExecutionRevision { get; }
    /// <summary>Gets the optional exact plan ID.</summary>
    public string? PlanId { get; }
    /// <summary>Gets the optional exact plan revision.</summary>
    public string? PlanRevision { get; }
}
