using System.Text.Json.Serialization;

namespace Penghou.Workflow.Abstractions;

/// <summary>A bounded, versioned declaration interpreted by a trusted authority adapter.</summary>
/// <remarks>Unknown schemas/capabilities and unresolved resource/scope references must not imply permission.</remarks>
public sealed record ExecutionRequirement
{
    /// <summary>Creates a declaration without interpreting policy or resolving a resource.</summary>
    /// <param name="schemaId">Requirement vocabulary/profile ID, bounded to 128 UTF-8 bytes.</param>
    /// <param name="schemaVersion">Positive version of that vocabulary.</param>
    /// <param name="capability">Capability ID within the vocabulary, bounded to 128 UTF-8 bytes.</param>
    /// <param name="resource">Opaque logical resource reference, bounded to 2048 UTF-8 bytes.</param>
    /// <param name="scopeReference">Optional retained scope reference, bounded to 256 UTF-8 bytes; its schema determines whether it is mandatory.</param>
    [JsonConstructor]
    public ExecutionRequirement(string schemaId, int schemaVersion, string capability, string resource, string? scopeReference = null)
    {
        SchemaId = ContractBounds.Required(schemaId, nameof(schemaId), 128);
        if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        SchemaVersion = schemaVersion;
        Capability = ContractBounds.Required(capability, nameof(capability), 128);
        Resource = ContractBounds.Required(resource, nameof(resource), 2048);
        ScopeReference = ContractBounds.Optional(scopeReference, nameof(scopeReference));
    }

    /// <summary>Gets the requirement vocabulary/profile ID.</summary>
    public string SchemaId { get; }
    /// <summary>Gets the positive vocabulary version.</summary>
    public int SchemaVersion { get; }
    /// <summary>Gets the declared capability ID.</summary>
    public string Capability { get; }
    /// <summary>Gets the opaque logical resource reference.</summary>
    public string Resource { get; }
    /// <summary>Gets the optional retained scope reference.</summary>
    public string? ScopeReference { get; }
}
