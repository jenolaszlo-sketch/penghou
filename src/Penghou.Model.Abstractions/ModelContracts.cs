using System.Collections.ObjectModel;
using System.Text;

namespace Penghou.Model.Abstractions;

/// <summary>Identifies the host-selected provider, model, and destination endpoint.</summary>
public sealed record ModelTarget
{
    /// <summary>Creates a validated target.</summary>
    public ModelTarget(string provider, string model, string endpointId)
    {
        Provider = ModelValidation.Identifier(provider, nameof(provider));
        Model = ModelValidation.Identifier(model, nameof(model));
        EndpointId = ModelValidation.Identifier(endpointId, nameof(endpointId));
    }
    /// <summary>Provider identity.</summary><value>Provider identity.</value>
    public string Provider { get; }
    /// <summary>Model identity.</summary><value>Model identity.</value>
    public string Model { get; }
    /// <summary>Host-selected endpoint identity.</summary><value>Endpoint identity.</value>
    public string EndpointId { get; }
}

/// <summary>A validated, extensible model operation name.</summary>
public sealed record ModelOperation
{
    /// <summary>Creates an operation name for a supported or provider-defined operation.</summary>
    public ModelOperation(string value) => Value = ModelValidation.Identifier(value, nameof(value));
    /// <summary>Operation name.</summary><value>The operation name.</value>
    public string Value { get; }
    /// <inheritdoc cref="object.ToString"/><returns>The operation name.</returns>
    public override string ToString() => Value;
    /// <summary>Streams model output.</summary>
    public static ModelOperation Stream { get; } = new("stream");
    /// <summary>Generates model output.</summary>
    public static ModelOperation Generate { get; } = new("generate");
    /// <summary>Completes a prompt.</summary>
    public static ModelOperation Complete { get; } = new("complete");
    /// <summary>Submits a batch.</summary>
    public static ModelOperation BatchSubmit { get; } = new("batch.submit");
    /// <summary>Reads batch status.</summary>
    public static ModelOperation BatchStatus { get; } = new("batch.status");
    /// <summary>Reads batch results.</summary>
    public static ModelOperation BatchResults { get; } = new("batch.results");
    /// <summary>Cancels a batch.</summary>
    public static ModelOperation BatchCancel { get; } = new("batch.cancel");
    /// <summary>Submits a job.</summary>
    public static ModelOperation JobSubmit { get; } = new("job.submit");
    /// <summary>Reads job status.</summary>
    public static ModelOperation JobStatus { get; } = new("job.status");
    /// <summary>Reads job results.</summary>
    public static ModelOperation JobResults { get; } = new("job.results");
    /// <summary>Cancels a job.</summary>
    public static ModelOperation JobCancel { get; } = new("job.cancel");
    /// <summary>Uploads model input.</summary>
    public static ModelOperation Upload { get; } = new("upload");
    /// <summary>Reads a model artifact.</summary>
    public static ModelOperation ArtifactRead { get; } = new("artifact.read");
    /// <summary>Creates embeddings.</summary>
    public static ModelOperation Embed { get; } = new("embed");
    /// <summary>Reranks candidates.</summary>
    public static ModelOperation Rerank { get; } = new("rerank");
    /// <summary>Moderates content.</summary>
    public static ModelOperation Moderate { get; } = new("moderate");
}

/// <summary>Descriptive correlation data. Values are not authenticated by this contract.</summary>
public sealed record ModelExecutionContext
{
    /// <summary>Creates a context and snapshots bounded metadata.</summary>
    public ModelExecutionContext(string? workflowId = null, string? activityId = null, string? agentId = null, string? correlationId = null, IReadOnlyDictionary<string, string>? metadata = null)
    {
        WorkflowId = ModelValidation.OptionalIdentifier(workflowId, nameof(workflowId));
        ActivityId = ModelValidation.OptionalIdentifier(activityId, nameof(activityId));
        AgentId = ModelValidation.OptionalIdentifier(agentId, nameof(agentId));
        CorrelationId = ModelValidation.OptionalIdentifier(correlationId, nameof(correlationId));
        Metadata = ModelValidation.Metadata(metadata);
    }
    /// <summary>Workflow identifier.</summary><value>Workflow identifier, if supplied.</value>
    public string? WorkflowId { get; }
    /// <summary>Activity identifier.</summary><value>Activity identifier, if supplied.</value>
    public string? ActivityId { get; }
    /// <summary>Agent identifier.</summary><value>Agent identifier, if supplied.</value>
    public string? AgentId { get; }
    /// <summary>Correlation identifier.</summary><value>Correlation identifier, if supplied.</value>
    public string? CorrelationId { get; }
    /// <summary>Bounded metadata snapshot; it is not automatically sent to providers.</summary><value>Read-only metadata.</value>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>Estimated usage supplied before dispatch.</summary>
public sealed record ModelUsageIntent
{
    /// <summary>Creates validated nonnegative estimates.</summary>
    public ModelUsageIntent(long? estimatedInputTokens = null, long? maxOutputTokens = null, decimal? estimatedCost = null, string? currency = null)
    {
        EstimatedInputTokens = ModelValidation.NonNegative(estimatedInputTokens, nameof(estimatedInputTokens));
        MaxOutputTokens = ModelValidation.NonNegative(maxOutputTokens, nameof(maxOutputTokens));
        EstimatedCost = ModelValidation.NonNegative(estimatedCost, nameof(estimatedCost));
        Currency = ModelValidation.Currency(currency, estimatedCost.HasValue);
    }
    /// <summary>Estimated input tokens.</summary><value>Estimate, or unknown.</value>
    public long? EstimatedInputTokens { get; }
    /// <summary>Maximum requested output tokens.</summary><value>Limit, or unspecified.</value>
    public long? MaxOutputTokens { get; }
    /// <summary>Estimated cost.</summary><value>Estimate, or unknown.</value>
    public decimal? EstimatedCost { get; }
    /// <summary>Currency for estimated cost.</summary><value>ISO-style three-letter currency, or null.</value>
    public string? Currency { get; }
}

/// <summary>Provider-reported usage; absent values remain unknown.</summary>
public sealed record ModelUsage
{
    /// <summary>Creates validated actual usage.</summary>
    public ModelUsage(long? inputTokens = null, long? outputTokens = null, long? cachedInputTokens = null, long? thinkingTokens = null, long? totalTokens = null, decimal? cost = null, string? currency = null, long? cacheMissInputTokens = null)
    {
        InputTokens = ModelValidation.NonNegative(inputTokens, nameof(inputTokens)); OutputTokens = ModelValidation.NonNegative(outputTokens, nameof(outputTokens));
        CachedInputTokens = ModelValidation.NonNegative(cachedInputTokens, nameof(cachedInputTokens)); ThinkingTokens = ModelValidation.NonNegative(thinkingTokens, nameof(thinkingTokens));
        TotalTokens = ModelValidation.NonNegative(totalTokens, nameof(totalTokens)); Cost = ModelValidation.NonNegative(cost, nameof(cost)); Currency = ModelValidation.Currency(currency, cost.HasValue);
        CacheMissInputTokens = ModelValidation.NonNegative(cacheMissInputTokens, nameof(cacheMissInputTokens));
    }
    /// <summary>Input tokens. <value>Count, or unknown.</value></summary>
    public long? InputTokens { get; }
    /// <summary>Output tokens. <value>Count, or unknown.</value></summary>
    public long? OutputTokens { get; }
    /// <summary>Provider-reported cache-hit/read input tokens; never inferred from totals.</summary>
    public long? CachedInputTokens { get; }
    /// <summary>Provider-reported cache-miss/write/creation input tokens; provider semantics must be retained separately.</summary>
    public long? CacheMissInputTokens { get; }
    /// <summary>Thinking tokens. <value>Count, or unknown.</value></summary>
    public long? ThinkingTokens { get; }
    /// <summary>Total tokens. <value>Count, or unknown.</value></summary>
    public long? TotalTokens { get; }
    /// <summary>Actual reported cost. <value>Cost, or unknown.</value></summary>
    public decimal? Cost { get; }
    /// <summary>Cost currency. <value>Three-letter currency, or null.</value></summary>
    public string? Currency { get; }
}

/// <summary>Opaque remote operation identity; possession grants no authority.</summary>
public sealed record ModelOperationHandle
{
    /// <summary>Creates an opaque operation handle bound descriptively to a target.</summary>
    public ModelOperationHandle(string id, ModelTarget target, ModelExecutionContext? context = null)
    {
        Id = ModelValidation.Identifier(id, nameof(id), 1024); Target = target ?? throw new ArgumentNullException(nameof(target)); Context = context;
    }
    /// <summary>Opaque provider operation identifier.</summary>
    public string Id { get; }
    /// <summary>Target associated with the operation.</summary>
    public ModelTarget Target { get; }
    /// <summary>Descriptive originating context.</summary>
    public ModelExecutionContext? Context { get; }
}

/// <summary>One semantic model invocation, including identity and attempt data.</summary>
public sealed record ModelInvocation
{
    /// <summary>Creates a validated invocation envelope.</summary>
    public ModelInvocation(string requestId, ModelTarget target, ModelOperation operation, ModelExecutionContext? context = null, ModelUsageIntent? usageIntent = null, string? credentialProfile = null, int attempt = 1, ModelOperationHandle? operationHandle = null)
    {
        RequestId = ModelValidation.Identifier(requestId, nameof(requestId)); Target = target ?? throw new ArgumentNullException(nameof(target)); Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        if (attempt < 1) throw new ArgumentOutOfRangeException(nameof(attempt)); Attempt = attempt;
        Context = context; UsageIntent = usageIntent; CredentialProfile = ModelValidation.OptionalIdentifier(credentialProfile, nameof(credentialProfile));
        if (operationHandle is not null && operationHandle.Target != Target) throw new ArgumentException("Operation handle target must match the invocation target.", nameof(operationHandle));
        OperationHandle = operationHandle;
    }
    /// <summary>Request identity.</summary>
    public string RequestId { get; }
    /// <summary>Provider, model and endpoint.</summary>
    public ModelTarget Target { get; }
    /// <summary>Requested semantic operation.</summary>
    public ModelOperation Operation { get; }
    /// <summary>Correlation context.</summary>
    public ModelExecutionContext? Context { get; }
    /// <summary>Estimated usage limits.</summary>
    public ModelUsageIntent? UsageIntent { get; }
    /// <summary>Opaque host-owned credential profile reference.</summary>
    public string? CredentialProfile { get; }
    /// <summary>Attempt number, starting at one.</summary>
    public int Attempt { get; }
    /// <summary>Remote operation handle associated with this request.</summary>
    public ModelOperationHandle? OperationHandle { get; }
}

/// <summary>Immutable envelope for typed data payloads. Payloads must follow the consumer's bounded data profile and be immutable snapshots.</summary>
public sealed record ModelTransportRequest<TPayload> where TPayload : notnull
{
    /// <summary>Creates a request envelope and snapshots batch item descriptors.</summary>
    public ModelTransportRequest(ModelInvocation invocation, TPayload payload, IEnumerable<ModelInvocation>? items = null)
    {
        Invocation = invocation ?? throw new ArgumentNullException(nameof(invocation)); Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Items = ModelValidation.Items(items);
    }
    /// <summary>Invocation metadata.</summary>
    public ModelInvocation Invocation { get; }
    /// <summary>Typed data payload. Consumer must reject unsupported profiles; arbitrary generic payloads cannot be bounded by this contract.</summary>
    public TPayload Payload { get; }
    /// <summary>Per-item invocation descriptors for bounded batch operations.</summary>
    public IReadOnlyList<ModelInvocation> Items { get; }
    /// <summary>Returns a content-free description, without rendering the payload or context.</summary>
    public override string ToString() => "ModelTransportRequest";
}

/// <summary>Typed result envelope with optional usage and provider correlation.</summary>
public sealed record ModelTransportResponse<TPayload> where TPayload : notnull
{
    /// <summary>Creates a response envelope.</summary>
    public ModelTransportResponse(string requestId, TPayload payload, ModelUsage? usage = null, string? providerRequestId = null, ModelOperationHandle? operationHandle = null)
    { RequestId = ModelValidation.Identifier(requestId, nameof(requestId)); Payload = payload ?? throw new ArgumentNullException(nameof(payload)); Usage = usage; ProviderRequestId = ModelValidation.OptionalIdentifier(providerRequestId, nameof(providerRequestId)); OperationHandle = operationHandle; }
    /// <summary>Request identity associated with this result or stream event.</summary>
    public string RequestId { get; }
    /// <summary>Typed data result.</summary>
    public TPayload Payload { get; }
    /// <summary>Actual usage; omitted fields are unknown.</summary>
    public ModelUsage? Usage { get; }
    /// <summary>Provider request identifier.</summary>
    public string? ProviderRequestId { get; }
    /// <summary>Remote operation handle, if returned.</summary>
    public ModelOperationHandle? OperationHandle { get; }
    /// <summary>Returns a content-free description, without rendering result data.</summary>
    public override string ToString() => "ModelTransportResponse";
}

/// <summary>Calls a model using typed bounded data contracts.</summary>
public interface IModelTransport<TRequest, TResponse> where TRequest : notnull where TResponse : notnull
{
    /// <summary>Invokes the model transport.</summary>
    ValueTask<ModelTransportResponse<TResponse>> InvokeAsync(ModelTransportRequest<TRequest> request, CancellationToken cancellationToken = default);
}

/// <summary>Streams model events using typed bounded data contracts.</summary>
public interface IStreamingModelTransport<TRequest, TEvent> where TRequest : notnull where TEvent : notnull
{
    /// <summary>Opens a model event stream.</summary>
    IAsyncEnumerable<ModelTransportResponse<TEvent>> StreamAsync(ModelTransportRequest<TRequest> request, CancellationToken cancellationToken = default);
}

/// <summary>Neutral model contract validation helpers.</summary>
internal static class ModelValidation
{
    internal static string Identifier(string value, string name, int maximumBytes = 256)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        if (value.Length == 0 || value.Length > maximumBytes) throw new ArgumentException($"Value must be nonempty and at most {maximumBytes} UTF-8 bytes.", name);
        int length;
        try { length = new UTF8Encoding(false, true).GetByteCount(value); } catch (EncoderFallbackException e) { throw new ArgumentException("Value contains malformed Unicode.", name, e); }
        if (length == 0 || length > maximumBytes || value.Any(c => char.IsControl(c) || char.IsWhiteSpace(c))) throw new ArgumentException($"Value must be nonempty, at most {maximumBytes} UTF-8 bytes, and contain no whitespace or control characters.", name);
        return value;
    }
    internal static string? OptionalIdentifier(string? value, string name) => value is null ? null : Identifier(value, name);
    internal static long? NonNegative(long? value, string name) => value < 0 ? throw new ArgumentOutOfRangeException(name) : value;
    internal static decimal? NonNegative(decimal? value, string name) => value < 0 ? throw new ArgumentOutOfRangeException(name) : value;
    internal static string? Currency(string? currency, bool hasCost)
    {
        if ((currency is null) != !hasCost) throw new ArgumentException("Currency and cost must be supplied together.", nameof(currency));
        if (currency is null) return null;
        if (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z')) throw new ArgumentException("Currency must be three uppercase ASCII letters.", nameof(currency));
        return currency;
    }
    internal static IReadOnlyDictionary<string, string> Metadata(IReadOnlyDictionary<string, string>? source)
    {
        if (source is null) return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
        var declaredCount = source.Count;
        if (declaredCount is < 0 or > 32) throw new ArgumentException("Metadata must declare between zero and 32 entries.", nameof(source));
        var copy = new Dictionary<string, string>(StringComparer.Ordinal); int count = 0;
        foreach (var pair in source)
        {
            if (++count > 32) throw new ArgumentException("Metadata is limited to 32 entries.", nameof(source));
            var key = Identifier(pair.Key, nameof(source), 64); ArgumentNullException.ThrowIfNull(pair.Value, nameof(source));
            if (pair.Value.Length > 1024) throw new ArgumentException("Metadata values are limited to 1024 UTF-8 bytes.", nameof(source));
            int bytes; try { bytes = new UTF8Encoding(false, true).GetByteCount(pair.Value); } catch (EncoderFallbackException e) { throw new ArgumentException("Metadata contains malformed Unicode.", nameof(source), e); }
            if (bytes > 1024 || pair.Value.Any(char.IsControl)) throw new ArgumentException("Metadata values are limited to 1024 UTF-8 bytes and may not contain controls.", nameof(source));
            if (!copy.TryAdd(key, pair.Value)) throw new ArgumentException("Metadata contains a duplicate key.", nameof(source));
        }
        if (count != declaredCount || source.Count != declaredCount) throw new ArgumentException("Metadata count does not match its enumeration.", nameof(source));
        return new ReadOnlyDictionary<string, string>(copy);
    }
    internal static IReadOnlyList<ModelInvocation> Items(IEnumerable<ModelInvocation>? source)
    {
        if (source is null) return Array.Empty<ModelInvocation>();
        var copy = new List<ModelInvocation>();
        foreach (var item in source)
        {
            if (copy.Count == 1000) throw new ArgumentException("A batch may contain at most 1000 item descriptors.", nameof(source));
            copy.Add(item ?? throw new ArgumentException("Batch items cannot be null.", nameof(source)));
        }
        return copy.AsReadOnly();
    }
}
