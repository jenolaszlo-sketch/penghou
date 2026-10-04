namespace Penghou.Http.Abstractions;

/// <summary>A bounded, immutable descriptive HTTP context; it authenticates no identity.</summary>
public sealed record HttpRequestContext
{
    /// <summary>Snapshots correlation data, never interpreted as a grant or serialized as headers automatically.</summary>
    public HttpRequestContext(string? correlationId = null, IReadOnlyDictionary<string, string>? metadata = null)
    {
        CorrelationId = ContractBounds.Optional(correlationId, nameof(correlationId));
        Metadata = ContractBounds.Metadata(metadata);
    }
    /// <summary>The optional correlation identifier.</summary>
    public string? CorrelationId { get; }
    /// <summary>The immutable bounded descriptive metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>A finite exact outbound HTTP request. The host/provider independently authenticates its bindings.</summary>
/// <remarks>URI, body and headers may contain sensitive data; never log them by default.</remarks>
public sealed record HttpTransportRequest
{
    /// <summary>Creates a snapshotted HTTP/HTTPS request with an explicit request identity and bounded profile.</summary>
    public HttpTransportRequest(string requestId, string method, Uri uri, HttpTransportBody? body = null,
        HttpHeaders? headers = null, HttpRequestContext? context = null, HttpTransportLimits? limits = null)
    {
        RequestId = ContractBounds.Text(requestId, nameof(requestId));
        Method = ContractBounds.Text(method, nameof(method), 32);
        if (!ContractBounds.IsToken(Method)) throw new ArgumentException("Invalid HTTP method token.", nameof(method));
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("An absolute HTTP/HTTPS URI without user-info or fragment is required.", nameof(uri));
        ContractBounds.Text(uri.AbsoluteUri, nameof(uri), 8192);
        Uri = uri;
        Body = body;
        Headers = headers ?? HttpHeaders.Empty;
        if (Headers.Fields.Keys.Any(key => key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Host", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Body framing headers belong to the transport implementation.", nameof(headers));
        Context = context;
        Limits = limits ?? new HttpTransportLimits();
        if ((Body?.Length ?? 0) > Limits.MaximumRequestBodyBytes) throw new ArgumentOutOfRangeException(nameof(body));
    }
    /// <summary>The fresh request identifier; a caller-supplied ID is never proof of authority.</summary>
    public string RequestId { get; }
    /// <summary>The exact case-sensitive HTTP method token.</summary>
    public string Method { get; }
    /// <summary>The exact destination; policy must bind the actual endpoint and redirects.</summary>
    public Uri Uri { get; }
    /// <summary>The optional finite snapshotted body.</summary>
    public HttpTransportBody? Body { get; }
    /// <summary>The immutable headers; no ambient credentials or cookies are implied.</summary>
    public HttpHeaders Headers { get; }
    /// <summary>The optional descriptive context.</summary>
    public HttpRequestContext? Context { get; }
    /// <summary>The concrete implementation's required finite limits.</summary>
    public HttpTransportLimits Limits { get; }
    /// <summary>Returns a content-free description, omitting sensitive URI, headers and body.</summary>
    public override string ToString() => "HttpTransportRequest";
}
