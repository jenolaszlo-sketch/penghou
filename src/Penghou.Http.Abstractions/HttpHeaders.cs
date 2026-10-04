using System.Collections.ObjectModel;

namespace Penghou.Http.Abstractions;

/// <summary>An immutable bounded, case-insensitive snapshot of HTTP header fields.</summary>
/// <remarks>Headers can contain secrets. Never log them by default. No header conveys authority.</remarks>
public sealed class HttpHeaders
{
    /// <summary>The maximum number of distinct header names.</summary>
    public const int MaximumFields = 64;
    /// <summary>The maximum number of values for one header.</summary>
    public const int MaximumValuesPerField = 16;
    /// <summary>The maximum aggregate UTF-8 header name/value bytes.</summary>
    public const int MaximumBytes = 65536;
    /// <summary>An empty immutable header set.</summary>
    public static HttpHeaders Empty { get; } = new();

    /// <summary>Snapshots validated HTTP header names and values.</summary>
    public HttpHeaders(IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (fields is not null)
        {
            var count = fields.Count;
            if (count < 0 || count > MaximumFields) throw new ArgumentOutOfRangeException(nameof(fields));
            var total = 0;
            foreach (var pair in fields)
            {
                if (result.Count >= count) throw new ArgumentException("Invalid or changing header collection.", nameof(fields));
                var key = ContractBounds.Text(pair.Key, nameof(fields), 256);
                if (!ContractBounds.IsToken(key)) throw new ArgumentException("Invalid HTTP header name.", nameof(fields));
                ArgumentNullException.ThrowIfNull(pair.Value);
                var values = ContractBounds.Snapshot(pair.Value, MaximumValuesPerField, nameof(fields));
                var copied = new List<string>(values.Count);
                foreach (var value in values)
                {
                    var text = ContractBounds.Text(value, nameof(fields), 8192, allowEmpty: true);
                    total = checked(total + System.Text.Encoding.UTF8.GetByteCount(text));
                    if (total > MaximumBytes) throw new ArgumentOutOfRangeException(nameof(fields));
                    copied.Add(text);
                }
                total = checked(total + System.Text.Encoding.UTF8.GetByteCount(key));
                if (total > MaximumBytes) throw new ArgumentOutOfRangeException(nameof(fields));
                result.Add(key, copied.AsReadOnly());
            }
            if (result.Count != count || fields.Count != count) throw new ArgumentException("Invalid or changing header collection.", nameof(fields));
            ByteCount = total;
        }
        Fields = new ReadOnlyDictionary<string, IReadOnlyList<string>>(result);
    }

    /// <summary>The snapshotted header fields; values preserve their original order.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Fields { get; }
    /// <summary>The aggregate UTF-8 name/value bytes, excluding protocol framing.</summary>
    public int ByteCount { get; }
}
