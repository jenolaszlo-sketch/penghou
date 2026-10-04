using System.Collections.ObjectModel;
using System.Text;

namespace Penghou.Http.Abstractions;

internal static class ContractBounds
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static string Text(string? value, string parameter, int maximum = 256, bool allowEmpty = false)
    {
        ArgumentNullException.ThrowIfNull(value, parameter);
        if (value.Length > maximum || (!allowEmpty && string.IsNullOrWhiteSpace(value)) ||
            value != value.Trim() || value.Any(char.IsControl))
            throw new ArgumentException("The value must be bounded text without control or surrounding whitespace.", parameter);
        try
        {
            if (Utf8.GetByteCount(value) > maximum)
                throw new ArgumentException("The value exceeds its UTF-8 byte limit.", parameter);
        }
        catch (EncoderFallbackException exception)
        {
            throw new ArgumentException("Invalid Unicode.", parameter, exception);
        }
        return value;
    }

    internal static string? Optional(string? value, string parameter, int maximum = 256) =>
        value is null ? null : Text(value, parameter, maximum);

    internal static bool IsToken(string value) => value.All(c =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' || "!#$%&'*+-.^_`|~".Contains(c));

    internal static IReadOnlyList<T> Snapshot<T>(IReadOnlyList<T> source, int maximum, string parameter) where T : class
    {
        var count = source.Count;
        if (count < 0 || count > maximum) throw new ArgumentOutOfRangeException(parameter);
        var items = new List<T>(count);
        foreach (var item in source)
        {
            if (item is null || items.Count >= count) throw new ArgumentException("Invalid or changing collection.", parameter);
            items.Add(item);
        }
        if (items.Count != count || source.Count != count) throw new ArgumentException("Invalid or changing collection.", parameter);
        return items.AsReadOnly();
    }

    internal static IReadOnlyDictionary<string, string> Metadata(IReadOnlyDictionary<string, string>? source)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (source is null) return new ReadOnlyDictionary<string, string>(result);
        var count = source.Count;
        if (count < 0 || count > 32) throw new ArgumentOutOfRangeException(nameof(source));
        foreach (var pair in source)
        {
            if (result.Count >= count) throw new ArgumentException("Invalid or changing collection.", nameof(source));
            result.Add(Text(pair.Key, nameof(source), 64), Text(pair.Value, nameof(source), 1024, allowEmpty: true));
        }
        if (result.Count != count || source.Count != count) throw new ArgumentException("Invalid or changing collection.", nameof(source));
        return new ReadOnlyDictionary<string, string>(result);
    }
}
