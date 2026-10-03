using System.Text;

namespace Penghou.Workflow.Abstractions;

internal static class ContractBounds
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static string Required(string? value, string parameter, int maximumBytes = 256)
    {
        ArgumentNullException.ThrowIfNull(value, parameter);
        // A valid Unicode string cannot have more UTF-16 code units than UTF-8 bytes.
        // Reject oversized inputs before scanning or encoding their contents.
        if (value.Length > maximumBytes)
            throw new ArgumentException($"The value exceeds {maximumBytes} UTF-8 bytes.", parameter);
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Any(char.IsControl))
            throw new ArgumentException("A nonblank value without surrounding whitespace or control characters is required.", parameter);
        try
        {
            if (StrictUtf8.GetByteCount(value) > maximumBytes)
                throw new ArgumentException($"The value exceeds {maximumBytes} UTF-8 bytes.", parameter);
        }
        catch (EncoderFallbackException exception)
        {
            throw new ArgumentException("The value must contain valid Unicode.", parameter, exception);
        }
        return value;
    }

    internal static string? Optional(string? value, string parameter, int maximumBytes = 256) =>
        value is null ? null : Required(value, parameter, maximumBytes);
}
