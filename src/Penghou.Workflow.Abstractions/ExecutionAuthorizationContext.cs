using System.Text.Json.Serialization;

namespace Penghou.Workflow.Abstractions;

/// <summary>An immutable, bounded snapshot bound to one fresh authorization evaluation.</summary>
/// <remarks>The trusted host binds this exact snapshot to independently authenticated identity. No IDs confer authority.</remarks>
public sealed record ExecutionAuthorizationContext
{
    /// <summary>The maximum number of requirement declarations in one evaluation.</summary>
    public const int MaximumRequirements = 64;

    /// <summary>Creates and snapshots a version-one evaluation context.</summary>
    /// <param name="identity">Exact concrete operation attempt and runtime freshness identity.</param>
    /// <param name="authorizationRequestId">Fresh host-scoped ID, never reused for another evaluation or changed snapshot.</param>
    /// <param name="requirements">Bounded declarations; an empty set is data, never implicit permission.</param>
    /// <param name="schemaVersion">Context schema version; only version one is supported.</param>
    [JsonConstructor]
    public ExecutionAuthorizationContext(ExecutionIdentity identity, string authorizationRequestId,
        IReadOnlyCollection<ExecutionRequirement> requirements, int schemaVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(requirements);
        if (schemaVersion != 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion), "Unsupported context schema version.");
        Identity = identity;
        AuthorizationRequestId = ContractBounds.Required(authorizationRequestId, nameof(authorizationRequestId));
        var count = requirements.Count;
        if (count < 0 || count > MaximumRequirements)
            throw new ArgumentOutOfRangeException(nameof(requirements), $"At most {MaximumRequirements} declarations are supported.");
        var snapshot = new List<ExecutionRequirement>(count);
        foreach (var requirement in requirements)
        {
            if (snapshot.Count >= count) throw new ArgumentException("The collection changed or reported an invalid count.", nameof(requirements));
            if (requirement is null) throw new ArgumentException("Null declarations are not supported.", nameof(requirements));
            snapshot.Add(requirement);
        }
        if (snapshot.Count != count || requirements.Count != count)
            throw new ArgumentException("The collection changed or reported an invalid count.", nameof(requirements));
        Requirements = Array.AsReadOnly(snapshot.ToArray());
    }

    /// <summary>Gets the supported context schema version.</summary>
    public int SchemaVersion => 1;
    /// <summary>Gets the immutable operation attempt identity.</summary>
    public ExecutionIdentity Identity { get; }
    /// <summary>Gets the ID binding one fresh evaluation to this exact snapshot.</summary>
    public string AuthorizationRequestId { get; }
    /// <summary>Gets the immutable bounded requirement snapshot in declared order.</summary>
    public IReadOnlyCollection<ExecutionRequirement> Requirements { get; }
    /// <summary>Gets the host-scoped execution ID.</summary>
    [JsonIgnore] public string ExecutionId => Identity.ExecutionId;
    /// <summary>Gets the optional parent execution ID.</summary>
    [JsonIgnore] public string? ParentExecutionId => Identity.ParentExecutionId;
    /// <summary>Gets the stable operation ID.</summary>
    [JsonIgnore] public string OperationId => Identity.OperationId;
    /// <summary>Gets the optional logical operation path.</summary>
    [JsonIgnore] public string? OperationPath => Identity.OperationPath;
    /// <summary>Gets the one-based actual attempt.</summary>
    [JsonIgnore] public int Attempt => Identity.Attempt;
    /// <summary>Gets the runtime-owned opaque freshness token.</summary>
    [JsonIgnore] public string ExecutionRevision => Identity.ExecutionRevision;
    /// <summary>Gets the optional exact plan ID.</summary>
    [JsonIgnore] public string? PlanId => Identity.PlanId;
    /// <summary>Gets the optional exact plan revision.</summary>
    [JsonIgnore] public string? PlanRevision => Identity.PlanRevision;
}
