namespace Penghou.Http.Abstractions;

/// <summary>Finite provider-enforced HTTP size, chunk, deadline and redirect requirements.</summary>
public sealed record HttpTransportLimits
{
    /// <summary>The maximum bytes returned by one response-body read.</summary>
    public const int MaximumChunkBytes = 65536;
    /// <summary>Creates a bounded transport profile. Redirects are disabled by default.</summary>
    public HttpTransportLimits(long maximumRequestBodyBytes = 67108864, long maximumResponseBodyBytes = 67108864,
        int maximumResponseHeaderBytes = 65536, int maximumChunkBytes = 16384, TimeSpan? timeout = null,
        int maximumRedirects = 0)
    {
        if (maximumRequestBodyBytes < 0 || maximumRequestBodyBytes > HttpTransportBody.MaximumBodyBytes)
            throw new ArgumentOutOfRangeException(nameof(maximumRequestBodyBytes));
        if (maximumResponseBodyBytes < 0 || maximumResponseBodyBytes > HttpTransportBody.MaximumBodyBytes)
            throw new ArgumentOutOfRangeException(nameof(maximumResponseBodyBytes));
        if (maximumResponseHeaderBytes <= 0 || maximumResponseHeaderBytes > HttpHeaders.MaximumBytes)
            throw new ArgumentOutOfRangeException(nameof(maximumResponseHeaderBytes));
        if (maximumChunkBytes <= 0 || maximumChunkBytes > MaximumChunkBytes)
            throw new ArgumentOutOfRangeException(nameof(maximumChunkBytes));
        var deadline = timeout ?? TimeSpan.FromMinutes(2);
        if (deadline <= TimeSpan.Zero || deadline > TimeSpan.FromDays(1)) throw new ArgumentOutOfRangeException(nameof(timeout));
        if (maximumRedirects < 0 || maximumRedirects > 10) throw new ArgumentOutOfRangeException(nameof(maximumRedirects));
        MaximumRequestBodyBytes = maximumRequestBodyBytes;
        MaximumResponseBodyBytes = maximumResponseBodyBytes;
        MaximumResponseHeaderBytes = maximumResponseHeaderBytes;
        MaximumReadBytes = maximumChunkBytes;
        Timeout = deadline;
        MaximumRedirects = maximumRedirects;
    }
    /// <summary>The maximum aggregate request payload bytes; implementations also bound multipart framing.</summary>
    public long MaximumRequestBodyBytes { get; }
    /// <summary>The maximum decoded response bytes, including streaming reads.</summary>
    public long MaximumResponseBodyBytes { get; }
    /// <summary>The maximum aggregate UTF-8 response-header name/value bytes.</summary>
    public int MaximumResponseHeaderBytes { get; }
    /// <summary>The maximum bytes returned by each body read.</summary>
    public int MaximumReadBytes { get; }
    /// <summary>The total operation timeout, covering response-body consumption.</summary>
    public TimeSpan Timeout { get; }
    /// <summary>The maximum explicitly permitted redirects; each destination requires provider policy validation.</summary>
    public int MaximumRedirects { get; }
}
