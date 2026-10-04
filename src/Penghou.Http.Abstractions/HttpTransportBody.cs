namespace Penghou.Http.Abstractions;

/// <summary>A finite, snapshotted HTTP request body; no stream or executable callback is exposed.</summary>
public abstract class HttpTransportBody
{
    /// <summary>The maximum aggregate request-body payload bytes.</summary>
    public const long MaximumBodyBytes = 1073741824;
    private protected HttpTransportBody() { }
    /// <summary>The finite payload byte count; multipart protocol framing is additionally bounded by the provider.</summary>
    public abstract long Length { get; }
}

/// <summary>A bounded immutable binary or text-encoded request body.</summary>
public sealed class HttpBinaryBody : HttpTransportBody
{
    private readonly byte[] _bytes;
    /// <summary>Snapshots bytes and their explicit media type.</summary>
    public HttpBinaryBody(ReadOnlyMemory<byte> bytes, string contentType = "application/octet-stream")
    {
        if (bytes.Length > MaximumBodyBytes) throw new ArgumentOutOfRangeException(nameof(bytes));
        ContentType = ContractBounds.Text(contentType, nameof(contentType), 256);
        if (!System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(ContentType, out _))
            throw new ArgumentException("A valid media type is required.", nameof(contentType));
        _bytes = bytes.ToArray();
    }
    /// <summary>The declared media type, interpreted and validated by the concrete HTTP implementation.</summary>
    public string ContentType { get; }
    /// <inheritdoc />
    public override long Length => _bytes.LongLength;
    /// <summary>Returns a detached copy; mutating it cannot change the admitted body.</summary>
    public byte[] GetBytes() => (byte[])_bytes.Clone();
}

/// <summary>One immutable multipart form-data part.</summary>
public sealed record HttpMultipartPart
{
    /// <summary>Creates a named body part with an optional logical file name.</summary>
    public HttpMultipartPart(string name, HttpBinaryBody body, string? fileName = null)
    {
        Name = ContractBounds.Text(name, nameof(name));
        ArgumentNullException.ThrowIfNull(body);
        Body = body;
        FileName = ContractBounds.Optional(fileName, nameof(fileName));
        if (FileName is not null && (FileName.IndexOfAny(['/', '\\', ':']) >= 0 || FileName is "." or ".."))
            throw new ArgumentException("A logical basename, not a path, is required.", nameof(fileName));
    }
    /// <summary>The form field name, safely encoded by the implementation.</summary>
    public string Name { get; }
    /// <summary>The finite part bytes and media type.</summary>
    public HttpBinaryBody Body { get; }
    /// <summary>An optional logical file name; never a local filesystem path to read.</summary>
    public string? FileName { get; }
}

/// <summary>A finite immutable multipart form-data request; the implementation owns boundary encoding.</summary>
public sealed class HttpMultipartBody : HttpTransportBody
{
    /// <summary>The maximum number of multipart fields.</summary>
    public const int MaximumParts = 64;
    /// <summary>Snapshots all named parts and validates the aggregate payload bound.</summary>
    public HttpMultipartBody(IReadOnlyList<HttpMultipartPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        Parts = ContractBounds.Snapshot(parts, MaximumParts, nameof(parts));
        if (Parts.Count == 0) throw new ArgumentException("At least one part is required.", nameof(parts));
        Length = Parts.Sum(part => part.Body.Length);
        if (Length > MaximumBodyBytes) throw new ArgumentOutOfRangeException(nameof(parts));
    }
    /// <summary>The immutable parts in declared order.</summary>
    public IReadOnlyList<HttpMultipartPart> Parts { get; }
    /// <inheritdoc />
    public override long Length { get; }
}
