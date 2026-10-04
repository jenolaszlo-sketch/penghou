using System.Runtime.CompilerServices;
using Penghou.Http.Abstractions;
using Penghou.Model.Abstractions;

var target = new ModelTarget("fake", "wire-model", "host-endpoint-profile");
var context = new ModelExecutionContext("workflow", "activity", "agent", "correlation",
    new Dictionary<string, string> { ["tenant"] = "demo" });
var request = new ModelTransportRequest<string>(new ModelInvocation("model-1", target,
    ModelOperation.Generate, context, new ModelUsageIntent(8, 16, 0.01m, "USD"), "opaque-profile"), "bounded data");
var modelInner = new FakeModelTransport();
var model = new ModelDecorator(modelInner);
var result = await model.InvokeAsync(request);
if (result.RequestId != request.Invocation.RequestId || result.Payload != "reply" ||
    result.Usage?.CachedInputTokens != 2 || result.Usage.CacheMissInputTokens != 3 ||
    result.Usage.TotalTokens is not null || modelInner.LastRequest?.Invocation.Context != context)
    throw new InvalidOperationException("Typed model result, usage or context was not preserved.");

await foreach (var item in model.StreamAsync(request))
    if (item.RequestId != request.Invocation.RequestId)
        throw new InvalidOperationException("The stream event was not request-bound.");
model.Allowed = false;
try { await model.InvokeAsync(request); throw new InvalidOperationException("Revocation did not deny the next request."); }
catch (ModelAccessDeniedException denial) when (denial.RequestId == request.Invocation.RequestId) { }
if (modelInner.Calls != 1 || modelInner.StreamOpens != 1)
    throw new InvalidOperationException("Denied model work reached the inner transport.");

var httpInner = new FakeHttpTransport();
var http = new HttpDecorator(httpInner) { Allowed = false };
var bytes = new byte[] { 1, 2, 3 };
var body = new HttpMultipartBody(new[] { new HttpMultipartPart("file", new HttpBinaryBody(bytes), "file.jsonl") });
bytes[0] = 99;
if (body.Parts[0].Body.GetBytes()[0] != 1) throw new InvalidOperationException("The request body was not snapshotted.");
var httpRequest = new HttpTransportRequest("http-1", "POST", new Uri("https://example.invalid/upload"), body,
    context: new HttpRequestContext("correlation"), limits: new HttpTransportLimits(maximumResponseBodyBytes: 16));
try { await http.SendAsync(httpRequest); throw new InvalidOperationException("Arbitrary HTTP was not denied."); }
catch (HttpAccessDeniedException denial) when (denial.RequestId == httpRequest.RequestId) { }
if (httpInner.Calls != 0) throw new InvalidOperationException("Denied HTTP was dispatched.");
http.Allowed = true;
await using (var response = await http.SendAsync(httpRequest))
{
    if (response.RequestId != httpRequest.RequestId) throw new InvalidOperationException("HTTP result binding mismatch.");
    var chunk = await response.Body.ReadAsync(8);
    if (!chunk.IsCompleted || chunk.Length != 3) throw new InvalidOperationException("Bounded HTTP completion was lost.");
}
if (!httpInner.Disposed) throw new InvalidOperationException("The owned HTTP response leaked.");
Console.WriteLine("Exact neutral model/HTTP packages: typed context/usage, stream, denial, snapshots and ownership passed.");

sealed class ModelDecorator(FakeModelTransport inner) : IModelTransport<string, string>, IStreamingModelTransport<string, string>
{
    public bool Allowed { get; set; } = true;
    private void Check(ModelTransportRequest<string> request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!Allowed) throw new ModelAccessDeniedException(request.Invocation.RequestId);
    }
    public ValueTask<ModelTransportResponse<string>> InvokeAsync(ModelTransportRequest<string> request, CancellationToken cancellationToken = default)
    {
        Check(request, cancellationToken);
        return inner.InvokeAsync(request, cancellationToken);
    }
    public async IAsyncEnumerable<ModelTransportResponse<string>> StreamAsync(ModelTransportRequest<string> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Check(request, cancellationToken);
        await foreach (var item in inner.StreamAsync(request, cancellationToken)) yield return item;
    }
}

sealed class FakeModelTransport : IModelTransport<string, string>, IStreamingModelTransport<string, string>
{
    public int Calls { get; private set; }
    public int StreamOpens { get; private set; }
    public ModelTransportRequest<string>? LastRequest { get; private set; }
    public ValueTask<ModelTransportResponse<string>> InvokeAsync(ModelTransportRequest<string> request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastRequest = request;
        return ValueTask.FromResult(new ModelTransportResponse<string>(request.Invocation.RequestId, "reply",
            new ModelUsage(inputTokens: 8, outputTokens: 4, cachedInputTokens: 2, cacheMissInputTokens: 3)));
    }
    public async IAsyncEnumerable<ModelTransportResponse<string>> StreamAsync(ModelTransportRequest<string> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StreamOpens++;
        await Task.Yield();
        yield return new ModelTransportResponse<string>(request.Invocation.RequestId, "event");
    }
}

sealed class HttpDecorator(FakeHttpTransport inner) : IHttpTransport
{
    public bool Allowed { get; set; }
    public ValueTask<HttpTransportResponse> SendAsync(HttpTransportRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Allowed) throw new HttpAccessDeniedException(request.RequestId);
        return inner.SendAsync(request, cancellationToken);
    }
}

sealed class FakeHttpTransport : IHttpTransport, IHttpResponseBody
{
    public int Calls { get; private set; }
    public bool Disposed { get; private set; }
    private bool _read;
    private HttpTransportLimits? _limits;
    public ValueTask<HttpTransportResponse> SendAsync(HttpTransportRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        _limits = request.Limits;
        return ValueTask.FromResult(new HttpTransportResponse(request.RequestId, 200, HttpHeaders.Empty, this));
    }
    public ValueTask<HttpResponseChunk> ReadAsync(int maximumBytes, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (maximumBytes < 3 || maximumBytes > _limits!.MaximumReadBytes || _limits.MaximumResponseBodyBytes < 3)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        var result = new HttpResponseChunk(_read ? ReadOnlyMemory<byte>.Empty : new byte[] { 1, 2, 3 }, true);
        _read = true;
        return ValueTask.FromResult(result);
    }
    public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
}
