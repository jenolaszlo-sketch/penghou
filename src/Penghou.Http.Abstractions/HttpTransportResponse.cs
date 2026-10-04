namespace Penghou.Http.Abstractions;

/// <summary>A detached finite response-body chunk with explicit terminal state.</summary>
public sealed class HttpResponseChunk
{
    private readonly byte[] _bytes;
    /// <summary>Snapshots one bounded nonempty chunk, or an explicit empty terminal chunk.</summary>
    public HttpResponseChunk(ReadOnlyMemory<byte> bytes, bool isCompleted = false)
    {
        if (bytes.Length > HttpTransportLimits.MaximumChunkBytes) throw new ArgumentOutOfRangeException(nameof(bytes));
        if (bytes.IsEmpty && !isCompleted) throw new ArgumentException("An empty chunk must indicate completion.", nameof(bytes));
        _bytes = bytes.ToArray();
        IsCompleted = isCompleted;
    }
    /// <summary>The number of decoded bytes in this chunk.</summary>
    public int Length => _bytes.Length;
    /// <summary>Whether this chunk is the last one, possibly carrying final bytes.</summary>
    public bool IsCompleted { get; }
    /// <summary>Returns a detached copy of the response bytes.</summary>
    public byte[] GetBytes() => (byte[])_bytes.Clone();
}

/// <summary>A process-local bounded response-body capability owned by one transport response.</summary>
/// <remarks>The provider enforces request limits, deadlines and cancellation across all reads. No concurrent reads are implied.</remarks>
public interface IHttpResponseBody : IAsyncDisposable
{
    /// <summary>Reads at most the requested positive bound, within the request profile; violations must fail explicitly.</summary>
    ValueTask<HttpResponseChunk> ReadAsync(int maximumBytes, CancellationToken cancellationToken = default);
}

/// <summary>An owned HTTP response. Dispose it on success, cancellation and failure, even when its body is not read.</summary>
public sealed class HttpTransportResponse : IAsyncDisposable
{
    /// <summary>Creates an exact request-bound response with immutable headers and an owned body capability.</summary>
    public HttpTransportResponse(string requestId, int statusCode, HttpHeaders headers, IHttpResponseBody body)
    {
        RequestId = ContractBounds.Text(requestId, nameof(requestId));
        if (statusCode < 100 || statusCode > 599) throw new ArgumentOutOfRangeException(nameof(statusCode));
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(body);
        StatusCode = statusCode;
        Headers = headers;
        Body = body;
    }
    /// <summary>The request identifier the implementation must bind to the exact invocation.</summary>
    public string RequestId { get; }
    /// <summary>The HTTP status; provider 401/403 are not neutral host-authority decisions.</summary>
    public int StatusCode { get; }
    /// <summary>The immutable response headers.</summary>
    public HttpHeaders Headers { get; }
    /// <summary>The process-local body; callers must honor request chunk limits and dispose its owning response.</summary>
    public IHttpResponseBody Body { get; }
    /// <summary>Releases the body and its underlying response resources. Providers make disposal idempotent.</summary>
    public ValueTask DisposeAsync() => Body.DisposeAsync();
}

/// <summary>A replaceable outbound HTTP execution boundary, independent of model authority and policy products.</summary>
public interface IHttpTransport
{
    /// <summary>Performs one current request or denies it without inner dispatch; returns an owned bounded response.</summary>
    /// <remarks>No ambient credentials, automatic cookies, unsupported guarantees or silent redirects are permitted.</remarks>
    ValueTask<HttpTransportResponse> SendAsync(HttpTransportRequest request, CancellationToken cancellationToken = default);
}
