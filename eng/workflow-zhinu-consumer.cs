using Penghou.Workflow.Abstractions;
using Penghou.Zhinu;
using Penghou.Zhinu.Sqlite;

var databasePath = Path.Combine(AppContext.BaseDirectory, "closure-consumer.db");
if (File.Exists(databasePath)) File.Delete(databasePath);
var store = new SqliteWorkflowStore(new ZhinuSqliteOptions { DatabasePath = databasePath, Pooling = false });
var ordinary = new OrdinaryWorkflow();
var protectedWorkflow = new ProtectedWorkflow();
var registry = new WorkflowRegistry()
    .Register("ordinary", "1", ordinary)
    .Register("protected", "1", protectedWorkflow);

await using (var directEngine = new WorkflowEngine(store, registry, new ZhinuOptions()))
{
    var result = await directEngine.RunAsync<string, string>("ordinary", "1", "package-closure");
    if (result != "ordinary:package-closure" || ordinary.Calls != 1)
        throw new InvalidOperationException("Direct-constructor ordinary execution did not complete exactly once.");
}

var authorizer = new AllowAuthorizer();
var authorization = new WorkflowExecutionAuthorizationOptions("closure-allow", "package-closure-v1", authorizer);
var options = new ZhinuOptions { ExecutionAuthorization = authorization };
await using (var builderEngine = new WorkflowEngineBuilder()
    .WithStore(store)
    .WithRegistry(registry)
    .WithOptions(options)
    .Build())
{
    var result = await builderEngine.RunAsync<string, string>("protected", "1", "package-closure");
    if (result != "protected:package-closure" || protectedWorkflow.Calls != 1)
        throw new InvalidOperationException("Builder protected execution did not complete exactly once.");
    if (authorizer.Contexts.Count != 1 || authorizer.Contexts[0].Requirements.Count != 1 ||
        authorizer.Contexts[0].Requirements.Single().Capability != "execute")
        throw new InvalidOperationException("Protected execution did not pass its declared requirement to the authorizer.");
}

Console.WriteLine($"Workflow loaded {typeof(ExecutionIdentity).Assembly.GetName().Version} at {typeof(ExecutionIdentity).Assembly.Location}");
Console.WriteLine("Package closure consumer completed both direct SQLite and protected builder execution.");

sealed class OrdinaryWorkflow : IWorkflow<string, string>
{
    public int Calls { get; private set; }
    public Task<string> RunAsync(WorkflowContext context, string input, CancellationToken cancellationToken) =>
        context.StepAsync("ordinary-step", input, (value, _) =>
        {
            Calls++;
            return Task.FromResult($"ordinary:{value}");
        }, cancellationToken: cancellationToken);
}

sealed class ProtectedWorkflow : IWorkflow<string, string>
{
    public int Calls { get; private set; }
    public Task<string> RunAsync(WorkflowContext context, string input, CancellationToken cancellationToken) =>
        context.StepAsync("protected-step", input, (value, _) =>
        {
            Calls++;
            return Task.FromResult($"protected:{value}");
        }, new StepOptions
        {
            Authorization = new WorkflowAuthorizationDeclaration([
                new ExecutionRequirement("workflow.capability", 1, "execute", "package-closure", null)])
        }, cancellationToken);
}

sealed class AllowAuthorizer : IExecutionAuthorizer
{
    public List<ExecutionAuthorizationContext> Contexts { get; } = [];

    public ValueTask<ExecutionAuthorizationResult> AuthorizeAsync(
        ExecutionAuthorizationContext context, CancellationToken cancellationToken = default)
    {
        Contexts.Add(context);
        var now = DateTimeOffset.UtcNow;
        return ValueTask.FromResult(new ExecutionAuthorizationResult(
            ExecutionAuthorizationDecision.Allowed,
            context.AuthorizationRequestId,
            "closure-allow",
            Guid.NewGuid().ToString("N"),
            now,
            now.AddMinutes(2)));
    }
}
