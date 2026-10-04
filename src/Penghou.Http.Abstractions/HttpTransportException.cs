namespace Penghou.Http.Abstractions;

/// <summary>A neutral HTTP transport failure, separate from cancellation and explicit authority denial.</summary>
public class HttpTransportException : Exception
{
    /// <summary>Creates a request-bound failure with a bounded neutral code; raw provider diagnostics remain host-private.</summary>
    public HttpTransportException(string requestId, string reasonCode)
        : base("The HTTP transport could not complete the request.")
    {
        RequestId = ContractBounds.Text(requestId, nameof(requestId));
        ReasonCode = ContractBounds.Text(reasonCode, nameof(reasonCode));
    }
    /// <summary>The affected request identity.</summary>
    public string RequestId { get; }
    /// <summary>The bounded machine-readable failure code, not a target URI, secret or response body.</summary>
    public string ReasonCode { get; }
}

/// <summary>Explicit host-authority denial. Never reinterpret this as provider failure or retry it automatically.</summary>
public sealed class HttpAccessDeniedException : HttpTransportException
{
    /// <summary>Creates an explicit current-authority denial bound to the request.</summary>
    public HttpAccessDeniedException(string requestId, string reasonCode = "access-denied") : base(requestId, reasonCode) { }
}
