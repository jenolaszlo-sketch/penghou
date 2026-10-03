namespace Penghou.Workflow.Abstractions;

/// <summary>Evaluates fresh workflow execution intent without invoking the protected operation.</summary>
/// <remarks>Runtimes own dispatch, durable approval, evidence and post-await fencing. Actual resources retain separate authorization.</remarks>
public interface IExecutionAuthorizer
{
    /// <summary>Evaluates one exact immutable context through a configured trusted provider.</summary>
    /// <param name="context">The host-bound snapshot for one fresh evaluation.</param>
    /// <param name="cancellationToken">Cancellation; cancellation/exception/null/malformed responses never imply permission.</param>
    /// <returns>An attributable outcome bound to the supplied request ID.</returns>
    ValueTask<ExecutionAuthorizationResult> AuthorizeAsync(ExecutionAuthorizationContext context,
        CancellationToken cancellationToken = default);
}
