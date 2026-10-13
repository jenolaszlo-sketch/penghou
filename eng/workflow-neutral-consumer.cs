using System.Text.Json;
using Penghou.Workflow.Abstractions;

var identity = new ExecutionIdentity("execution", "operation", 1, "revision");
var requirement = new ExecutionRequirement("profile", 1, "read", "resource");
var source = new List<ExecutionRequirement> { requirement };
var context = new ExecutionAuthorizationContext(identity, "request", source);
source.Clear();
if (context.Requirements.Count != 1) throw new Exception("Ownership failure");
var roundtrip = JsonSerializer.Deserialize<ExecutionAuthorizationContext>(JsonSerializer.Serialize(context))!;
if (roundtrip.Identity != context.Identity || !roundtrip.Requirements.SequenceEqual(context.Requirements)) throw new Exception("JSON failure");
IExecutionAuthorizer authorizer = new DenyAuthorizer();
var result = await authorizer.AuthorizeAsync(context);
if (result.Decision != ExecutionAuthorizationDecision.Denied || result.AuthorizationRequestId != "request") throw new Exception("Binding failure");
try { new ExecutionIdentity("execution", "operation", 0, "revision"); throw new Exception("Accepted invalid attempt"); }
catch (ArgumentOutOfRangeException) { }
Console.WriteLine($"Workflow loaded {typeof(ExecutionIdentity).Assembly.GetName().Version} at {typeof(ExecutionIdentity).Assembly.Location}");
Console.WriteLine("Neutral packaged behavior passed: ownership, JSON, interface dispatch, denial and bounds.");
sealed class DenyAuthorizer : IExecutionAuthorizer
{
    public ValueTask<ExecutionAuthorizationResult> AuthorizeAsync(ExecutionAuthorizationContext context, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ExecutionAuthorizationResult(ExecutionAuthorizationDecision.Denied, context.AuthorizationRequestId, "provider", "decision", DateTimeOffset.UtcNow));
}

