namespace Penghou.Model.Abstractions;

/// <summary>Base failure for model transport operations.</summary>
public class ModelTransportException : Exception
{
    /// <summary>Creates a request-bound transport failure without exposing provider diagnostics.</summary>
    public ModelTransportException(string requestId, string reasonCode) : base("The model transport operation failed.")
    { RequestId = ModelExceptionValidation.Identifier(requestId, nameof(requestId)); ReasonCode = ModelExceptionValidation.Identifier(reasonCode, nameof(reasonCode)); }
    /// <summary>Request identity associated with the failure.</summary>
    public string RequestId { get; }
    /// <summary>Bounded, neutral failure category.</summary>
    public string ReasonCode { get; }
}

/// <summary>Indicates that the host denied a model operation.</summary>
public class ModelAccessDeniedException : ModelTransportException
{
    /// <summary>Creates a request-bound access denial.</summary>
    public ModelAccessDeniedException(string requestId, string reasonCode = "access-denied") : base(requestId, reasonCode) { }
}

/// <summary>Indicates that a model usage budget was exceeded.</summary>
public sealed class ModelBudgetExceededException : ModelAccessDeniedException
{
    /// <summary>Creates a request-bound budget denial.</summary>
    public ModelBudgetExceededException(string requestId, string reasonCode = "budget-exceeded") : base(requestId, reasonCode) { }
}

/// <summary>Indicates that the model service is unavailable.</summary>
public sealed class ModelUnavailableException : ModelTransportException
{
    /// <summary>Creates a request-bound model availability failure.</summary>
    public ModelUnavailableException(string requestId, string reasonCode = "unavailable") : base(requestId, reasonCode) { }
}

internal static class ModelExceptionValidation
{
    internal static string Identifier(string value, string name)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        if (value.Length == 0 || value.Length > 256 || value.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
            throw new ArgumentException("Value must be nonempty, at most 256 characters, and contain no whitespace or control characters.", name);
        try { _ = new System.Text.UTF8Encoding(false, true).GetByteCount(value); }
        catch (System.Text.EncoderFallbackException e) { throw new ArgumentException("Value contains malformed Unicode.", name, e); }
        if (System.Text.Encoding.UTF8.GetByteCount(value) > 256) throw new ArgumentException("Value exceeds the 256 UTF-8 byte limit.", name);
        return value;
    }
}
