using System.Text;

namespace Penghou.IO.Abstractions;

/// <summary>Validation and canonicalization rules for the Windows workspace path profile.</summary>
public static class WindowsWorkspacePath
{
    public const int MaximumLength = 2048;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly HashSet<string> ReservedNames = CreateReservedNames();

    /// <summary>
    /// Validates a slash-separated, relative Windows workspace path and returns it
    /// with its original case. This method performs no filesystem access.
    /// </summary>
    public static WorkspacePath Normalize(WorkspacePath path, bool allowRoot = true)
    {
        var value = path.Value;
        if (value is null)
            throw new ArgumentException("Workspace path cannot be null.", nameof(path));

        if (value.Length == 0)
        {
            if (allowRoot)
                return WorkspacePath.Root;
            throw new ArgumentException("The workspace root is not valid for this operation.", nameof(path));
        }

        if (value.Length > MaximumLength)
            throw new ArgumentException($"Workspace paths are limited to {MaximumLength} UTF-16 code units.", nameof(path));

        if (value[0] == '/' || value[^1] == '/' || value.Contains('\\') || value.Contains(':'))
            throw new ArgumentException("Workspace paths must be relative and use '/' separators only.", nameof(path));

        try
        {
            _ = StrictUtf8.GetByteCount(value);
        }
        catch (EncoderFallbackException ex)
        {
            throw new ArgumentException("Workspace paths must contain valid Unicode scalar values.", nameof(path), ex);
        }

        foreach (var segment in value.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or "..")
                throw new ArgumentException("Workspace paths cannot contain empty, '.' or '..' segments.", nameof(path));

            if (segment[^1] is ' ' or '.')
                throw new ArgumentException("Windows path segments cannot end in a space or period.", nameof(path));

            foreach (var character in segment)
            {
                if (char.IsControl(character) || character is '<' or '>' or '"' or '|' or '?' or '*')
                    throw new ArgumentException("Workspace paths contain a character unsupported by the Windows profile.", nameof(path));
            }

            var firstPeriod = segment.IndexOf('.');
            var deviceStem = (firstPeriod < 0 ? segment : segment[..firstPeriod])
                .TrimEnd(' ', '.')
                .ToUpperInvariant();
            if (ReservedNames.Contains(deviceStem))
                throw new ArgumentException("Workspace paths cannot use reserved Windows device names.", nameof(path));
        }

        return new WorkspacePath(value);
    }

    /// <summary>Returns the validated ASCII-folded path used by request and continuation bindings.</summary>
    public static string ToIdentityPath(WorkspacePath path, bool allowRoot = true)
    {
        var normalized = Normalize(path, allowRoot);
        // Windows filesystem upcase behavior is not identical across every
        // filesystem and version. Fold ASCII only; preserve non-ASCII spelling
        // so the codec never assumes a broader equivalence than its profile.
        return FoldAsciiCase(normalized.Value);
    }

    private static string FoldAsciiCase(string value)
    {
        var chars = value.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            if (chars[index] is >= 'a' and <= 'z')
                chars[index] = (char)(chars[index] - ('a' - 'A'));
        }

        return new string(chars);
    }

    private static HashSet<string> CreateReservedNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$", "CLOCK$"
        };

        for (var index = 1; index <= 9; index++)
        {
            names.Add($"COM{index}");
            names.Add($"LPT{index}");
        }

        foreach (var suffix in new[] { '¹', '²', '³' })
        {
            names.Add($"COM{suffix}");
            names.Add($"LPT{suffix}");
        }

        return names;
    }
}
