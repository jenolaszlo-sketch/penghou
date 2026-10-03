using System.Text;
using System.Text.Json;
using Penghou.Workflow.Abstractions;
using Xunit;

namespace Penghou.Workflow.Tests;

public sealed class WorkflowAuthorizationContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ContextTakesAnImmutableSnapshotOfRequirements()
    {
        var source = new List<ExecutionRequirement>
        {
            new("docs", 1, "read", "manual/one")
        };
        var context = Context(source);
        source.Add(new("docs", 1, "write", "manual/two"));

        Assert.Single(context.Requirements);
        Assert.Equal("manual/one", Assert.Single(context.Requirements).Resource);
        Assert.Throws<NotSupportedException>(() => ((IList<ExecutionRequirement>)context.Requirements).Add(
            new("docs", 1, "write", "manual/three")));
    }

    [Fact]
    public void RequirementsAreBoundedByCountAndUtf8Bytes()
    {
        Assert.ThrowsAny<ArgumentException>(() => new ExecutionRequirement("x", 0, "read", "a"));
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement(new string('x', 129), 1, "read", "a"));
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement("x", 1, new string('x', 129), "a"));
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement("x", 1, "read", new string('x', 2049)));
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement("x", 1, "read", "a", new string('x', 257)));
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement("é", 1, "read", new string('é', 1025)));
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement(" x", 1, "read", "a"));
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement("x\n", 1, "read", "a"));
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement("bad\ud800", 1, "read", "a"));

        var maxSchema = new ExecutionRequirement(new string('s', 128), 1, new string('c', 128), new string('r', 2048));
        Assert.Equal(128, Encoding.UTF8.GetByteCount(maxSchema.SchemaId));
        Assert.Equal(2048, Encoding.UTF8.GetByteCount(maxSchema.Resource));

        var tooMany = Enumerable.Range(0, 65).Select(i => new ExecutionRequirement("x", 1, "read", i.ToString())).ToArray();
        Assert.ThrowsAny<ArgumentException>(() => new ExecutionAuthorizationContext(Identity(), "request-1", tooMany));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExecutionAuthorizationContext(Identity(), "request-1", [], schemaVersion: 2));
        Assert.Throws<ArgumentException>(() => new ExecutionAuthorizationContext(Identity(), "request-1", [null!]));
        Assert.Throws<ArgumentNullException>(() => new ExecutionAuthorizationContext(Identity(), "request-1", null!));
        Assert.Throws<ArgumentException>(() => new ExecutionAuthorizationContext(Identity(), " request-1", []));
        Assert.Throws<ArgumentException>(() => new ExecutionAuthorizationContext(Identity(), "bad\ud800", []));
        Assert.Throws<ArgumentException>(() => new ExecutionAuthorizationContext(Identity(), "request\n1", []));
        Assert.ThrowsAny<ArgumentException>(() => new ExecutionAuthorizationContext(Identity(), "request-1", new LyingRequirements()));
        Assert.Throws<ArgumentException>(() => new ExecutionAuthorizationContext(Identity(), "request-1", new ChangingCountRequirements()));
        Assert.Empty(Context(Array.Empty<ExecutionRequirement>()).Requirements);
    }

    [Fact]
    public void IdentityValidatesAttemptsBoundsAndPlanPairing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExecutionIdentity("exec", "op", 0, "r1"));
        Assert.Throws<ArgumentException>(() => new ExecutionIdentity(new string('é', 129), "op", 1, "r1"));
        Assert.Throws<ArgumentException>(() => new ExecutionIdentity("exec", "op", 1, "r1", operationPath: new string('x', 2049)));
        Assert.Throws<ArgumentException>(() => new ExecutionIdentity("exec", "op", 1, "r1", planId: "plan-only"));
        Assert.Throws<ArgumentException>(() => new ExecutionIdentity("exec", "op", 1, "r1", planRevision: "revision-only"));
        Assert.Throws<ArgumentException>(() => new ExecutionIdentity("exec", "op", 1, "r1", parentExecutionId: "exec"));

        var valid = new ExecutionIdentity("exec", "op", 1, "r1", planId: "plan", planRevision: "p1");
        Assert.Equal("plan", valid.PlanId);
        Assert.Equal("p1", valid.PlanRevision);
    }

    [Fact]
    public void ResultRejectsUnknownDecisionsInvalidExpiryAndMalformedApprovalCorrelation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Result((ExecutionAuthorizationDecision)999));
        Assert.Throws<ArgumentException>(() => Result(ExecutionAuthorizationDecision.Allowed));
        Assert.ThrowsAny<ArgumentException>(() => Result(ExecutionAuthorizationDecision.Allowed, expiresAt: Now));
        Assert.ThrowsAny<ArgumentException>(() => Result(ExecutionAuthorizationDecision.Allowed, expiresAt: Now.AddSeconds(-1)));
        Assert.Throws<ArgumentException>(() => Result(ExecutionAuthorizationDecision.ApprovalRequired));
        Assert.Throws<ArgumentException>(() => Result(ExecutionAuthorizationDecision.Denied, approvalRequestId: "approval-1"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExecutionAuthorizationResult(
            ExecutionAuthorizationDecision.Denied, "request-1", "provider", "decision", Now, schemaVersion: 2));

        var pending = Result(ExecutionAuthorizationDecision.ApprovalRequired, approvalRequestId: "approval-1");
        Assert.Equal("approval-1", pending.ApprovalRequestId);
        Assert.Throws<ArgumentException>(() => Result(ExecutionAuthorizationDecision.Unavailable, reasonCode: new string('x', 257)));

        var nonAllowWithExpiry = Result(ExecutionAuthorizationDecision.Denied, expiresAt: Now.AddMinutes(1));
        Assert.Equal(Now.AddMinutes(1), nonAllowWithExpiry.ExpiresAt);
    }

    [Fact]
    public void JsonRoundTripPreservesContractsAndConstructorValidation()
    {
        var original = Context([new("capability:v1", 1, "read", "folder/file", "scope-1")]);
        var json = JsonSerializer.Serialize(original);
        var roundTrip = JsonSerializer.Deserialize<ExecutionAuthorizationContext>(json);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Identity, roundTrip.Identity);
        Assert.Equal(original.AuthorizationRequestId, roundTrip.AuthorizationRequestId);
        Assert.Equal(original.Requirements, roundTrip.Requirements);
        Assert.Equal("scope-1", Assert.Single(roundTrip.Requirements).ScopeReference);

        const string invalidContext = "{\"Identity\":{\"ExecutionId\":\"e\",\"OperationId\":\"o\",\"Attempt\":1,\"ExecutionRevision\":\"r\"},\"AuthorizationRequestId\":\"request-1\",\"Requirements\":[],\"SchemaVersion\":2}";
        Assert.Throws<ArgumentOutOfRangeException>(() => JsonSerializer.Deserialize<ExecutionAuthorizationContext>(invalidContext));
        const string invalidResult = "{\"Decision\":1,\"AuthorizationRequestId\":\"request-1\",\"ProviderId\":\"provider\",\"DecisionId\":\"decision\",\"EvaluatedAt\":\"2026-10-03T12:00:00Z\",\"ExpiresAt\":\"2026-10-03T12:01:00Z\",\"SchemaVersion\":2}";
        Assert.Throws<ArgumentOutOfRangeException>(() => JsonSerializer.Deserialize<ExecutionAuthorizationResult>(invalidResult));
        var strictOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        };
        var unknownMember = json[..^1] + ",\"UntrustedSubject\":\"administrator\"}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ExecutionAuthorizationContext>(unknownMember, strictOptions));
    }

    [Fact]
    public async Task DeniedPendingUnavailableErrorAndInvalidProviderResponsesNeverDispatch()
    {
        foreach (var decision in new[]
        {
            ExecutionAuthorizationDecision.Denied,
            ExecutionAuthorizationDecision.ApprovalRequired,
            ExecutionAuthorizationDecision.Unavailable,
            ExecutionAuthorizationDecision.Error
        })
        {
            var authorizer = new FakeAuthorizer(_ => Result(
                decision,
                approvalRequestId: decision == ExecutionAuthorizationDecision.ApprovalRequired ? "approval-1" : null));
            var consumer = new NeutralConsumer(authorizer);

            var outcome = await consumer.ExecuteAsync(Context([Requirement()]));

            Assert.False(outcome.Dispatched);
            Assert.Equal(0, consumer.DispatchCount);
            Assert.Equal(1, authorizer.CallCount);
        }

        var throwing = new FakeAuthorizer(_ => throw new InvalidOperationException("provider failed"));
        var failedConsumer = new NeutralConsumer(throwing);
        Assert.False((await failedConsumer.ExecuteAsync(Context([Requirement()]))).Dispatched);
        Assert.Equal(0, failedConsumer.DispatchCount);

        var nullResultConsumer = new NeutralConsumer(new FakeAuthorizer(_ => null!));
        Assert.False((await nullResultConsumer.ExecuteAsync(Context([Requirement()]))).Dispatched);
        Assert.Equal(0, nullResultConsumer.DispatchCount);

        var invalid = new FakeAuthorizer(context => Result(
            ExecutionAuthorizationDecision.Allowed,
            authorizationRequestId: "some-other-request"));
        var invalidConsumer = new NeutralConsumer(invalid);
        Assert.False((await invalidConsumer.ExecuteAsync(Context([Requirement()]))).Dispatched);
        Assert.Equal(0, invalidConsumer.DispatchCount);

        var wrongProvider = new FakeAuthorizer(context => Result(
            ExecutionAuthorizationDecision.Allowed,
            authorizationRequestId: context.AuthorizationRequestId,
            expiresAt: Now.AddMinutes(1),
            providerId: "unexpected-provider"));
        var wrongProviderConsumer = new NeutralConsumer(wrongProvider, expectedProviderId: "configured-provider");
        Assert.False((await wrongProviderConsumer.ExecuteAsync(Context([Requirement()]))).Dispatched);
        Assert.Equal(0, wrongProviderConsumer.DispatchCount);
    }

    [Fact]
    public async Task OnlyFreshCorrelatedAllowWithKnownNonemptyRequirementsDispatches()
    {
        var request = Context([Requirement()]);
        var authorizer = new FakeAuthorizer(context => Result(
            ExecutionAuthorizationDecision.Allowed,
            authorizationRequestId: context.AuthorizationRequestId,
            evaluatedAt: Now,
            expiresAt: Now.AddMinutes(1)));
        var consumer = new NeutralConsumer(authorizer, clock: () => Now);

        var outcome = await consumer.ExecuteAsync(request);

        Assert.True(outcome.Dispatched);
        Assert.Equal(1, consumer.DispatchCount);
        Assert.Equal(1, authorizer.CallCount);
        Assert.Equal(request, authorizer.LastContext);
    }

    [Fact]
    public async Task ExpiredOrStaleAllowsAndUnknownOrEmptyRequirementsFailClosed()
    {
        var request = Context([Requirement()]);
        foreach (var response in new[]
        {
            Result(ExecutionAuthorizationDecision.Allowed, authorizationRequestId: request.AuthorizationRequestId,
                evaluatedAt: Now.AddMinutes(-2), expiresAt: Now.AddMinutes(-1)),
            Result(ExecutionAuthorizationDecision.Allowed, authorizationRequestId: "different-request",
                evaluatedAt: Now, expiresAt: Now.AddMinutes(1))
        })
        {
            var consumer = new NeutralConsumer(new FakeAuthorizer(_ => response), clock: () => Now);
            Assert.False((await consumer.ExecuteAsync(request)).Dispatched);
            Assert.Equal(0, consumer.DispatchCount);
        }

        var empty = Context(Array.Empty<ExecutionRequirement>());
        var emptyAuthorizer = new FakeAuthorizer(context => Allow(context));
        var emptyConsumer = new NeutralConsumer(emptyAuthorizer, clock: () => Now);
        Assert.False((await emptyConsumer.ExecuteAsync(empty)).Dispatched);
        Assert.Equal(0, emptyAuthorizer.CallCount);

        var unknown = Context([new("future-schema", int.MaxValue, "some-new-capability", "resource")]);
        var unknownAuthorizer = new FakeAuthorizer(context => Allow(context));
        var unknownConsumer = new NeutralConsumer(unknownAuthorizer, clock: () => Now);
        Assert.False((await unknownConsumer.ExecuteAsync(unknown)).Dispatched);
        Assert.Equal(0, unknownAuthorizer.CallCount);

        var unknownCapability = Context([new("capability:v1", 1, "future-capability", "resource")]);
        var unknownCapabilityAuthorizer = new FakeAuthorizer(context => Allow(context));
        var unknownCapabilityConsumer = new NeutralConsumer(unknownCapabilityAuthorizer, clock: () => Now);
        Assert.False((await unknownCapabilityConsumer.ExecuteAsync(unknownCapability)).Dispatched);
        Assert.Equal(0, unknownCapabilityAuthorizer.CallCount);

        var unboundAuthorizer = new FakeAuthorizer(context => Allow(context));
        var unboundConsumer = new NeutralConsumer(unboundAuthorizer, hostBindingValid: false);
        Assert.False((await unboundConsumer.ExecuteAsync(Context([Requirement()]))).Dispatched);
        Assert.Equal(0, unboundAuthorizer.CallCount);

        var missingEvidenceConsumer = new NeutralConsumer(
            new FakeAuthorizer(context => Allow(context)), evidenceRequired: true);
        Assert.False((await missingEvidenceConsumer.ExecuteAsync(Context([Requirement()]))).Dispatched);
        Assert.Equal(0, missingEvidenceConsumer.DispatchCount);

        var futureDatedAuthorizer = new FakeAuthorizer(context => Result(
            ExecutionAuthorizationDecision.Allowed,
            authorizationRequestId: context.AuthorizationRequestId,
            evaluatedAt: Now.AddSeconds(1),
            expiresAt: Now.AddMinutes(1)));
        var futureDatedConsumer = new NeutralConsumer(futureDatedAuthorizer, clock: () => Now);
        Assert.False((await futureDatedConsumer.ExecuteAsync(Context([Requirement()]))).Dispatched);
        Assert.Equal(0, futureDatedConsumer.DispatchCount);

        var staleFenceAuthorizer = new FakeAuthorizer(context => Allow(context));
        var staleFenceConsumer = new NeutralConsumer(staleFenceAuthorizer, isCurrentRevision: _ => false);
        Assert.False((await staleFenceConsumer.ExecuteAsync(Context([Requirement()]))).Dispatched);
        Assert.Equal(0, staleFenceConsumer.DispatchCount);

        var currentRevision = true;
        var delayedAuthorizer = new DelayedAuthorizer();
        var recheckedFenceConsumer = new NeutralConsumer(delayedAuthorizer, isCurrentRevision: _ => currentRevision);
        var pendingExecution = recheckedFenceConsumer.ExecuteAsync(Context([Requirement()]));
        Assert.False(pendingExecution.IsCompleted);
        currentRevision = false;
        delayedAuthorizer.Complete(Allow(delayedAuthorizer.LastContext!));
        Assert.False((await pendingExecution).Dispatched);
        Assert.Equal(0, recheckedFenceConsumer.DispatchCount);
    }

    [Fact]
    public async Task CancellationAndAuthorizationFailureDoNotCallProtectedDelegate()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var authorizer = new FakeAuthorizer((_, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Allow(Context([Requirement()]));
        });
        var consumer = new NeutralConsumer(authorizer);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await consumer.ExecuteAsync(Context([Requirement()]), canceled.Token));
        Assert.Equal(0, consumer.DispatchCount);

        using var canceledDuringAuthorization = new CancellationTokenSource();
        var cancelingAuthorizer = new FakeAuthorizer((context, _) =>
        {
            canceledDuringAuthorization.Cancel();
            return Allow(context);
        });
        var cancelingConsumer = new NeutralConsumer(cancelingAuthorizer);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cancelingConsumer.ExecuteAsync(Context([Requirement()]), canceledDuringAuthorization.Token));
        Assert.Equal(0, cancelingConsumer.DispatchCount);
    }

    [Fact]
    public void ContractAssemblyHasNoConcreteAuthorizerOrProductAssemblyDependencies()
    {
        var assembly = typeof(IExecutionAuthorizer).Assembly;
        var forbidden = assembly.GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .Where(name => name.StartsWith("Penghou.Zhinu", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Penghou.Hufu", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Penghou.IO.", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(forbidden);
        Assert.DoesNotContain(assembly.GetTypes(), type =>
            !type.IsInterface && !type.IsAbstract && typeof(IExecutionAuthorizer).IsAssignableFrom(type));
        Assert.Equal(typeof(IExecutionAuthorizer), assembly.GetType("Penghou.Workflow.Abstractions.IExecutionAuthorizer"));
    }

    private static ExecutionAuthorizationContext Context(IReadOnlyCollection<ExecutionRequirement> requirements) =>
        new(Identity(), "request-1", requirements);

    private static ExecutionIdentity Identity() => new("execution-1", "operation-1", 1, "revision-1");

    private static ExecutionRequirement Requirement() => new("capability:v1", 1, "read", "resource-1");

    private static ExecutionAuthorizationResult Allow(ExecutionAuthorizationContext context) => Result(
        ExecutionAuthorizationDecision.Allowed,
        authorizationRequestId: context.AuthorizationRequestId,
        evaluatedAt: Now,
        expiresAt: Now.AddMinutes(1));

    private static ExecutionAuthorizationResult Result(
        ExecutionAuthorizationDecision decision,
        string authorizationRequestId = "request-1",
        DateTimeOffset? evaluatedAt = null,
        DateTimeOffset? expiresAt = null,
        string? approvalRequestId = null,
        string? reasonCode = null,
        string providerId = "test-provider",
        string? evidenceId = null) => new(
            decision,
            authorizationRequestId,
            providerId,
            "decision-1",
            evaluatedAt ?? Now,
            expiresAt,
            approvalRequestId,
            reasonCode,
            evidenceId);

    private sealed class FakeAuthorizer : IExecutionAuthorizer
    {
        private readonly Func<ExecutionAuthorizationContext, CancellationToken, ExecutionAuthorizationResult> _authorize;

        public FakeAuthorizer(Func<ExecutionAuthorizationContext, ExecutionAuthorizationResult> authorize)
            : this((context, _) => authorize(context))
        {
        }

        public FakeAuthorizer(Func<ExecutionAuthorizationContext, CancellationToken, ExecutionAuthorizationResult> authorize)
        {
            _authorize = authorize;
        }

        public int CallCount { get; private set; }
        public ExecutionAuthorizationContext? LastContext { get; private set; }

        public ValueTask<ExecutionAuthorizationResult> AuthorizeAsync(
            ExecutionAuthorizationContext context,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastContext = context;
            return ValueTask.FromResult(_authorize(context, cancellationToken));
        }
    }

    private sealed class ChangingCountRequirements : IReadOnlyCollection<ExecutionRequirement>
    {
        private int _reads;
        public int Count => ++_reads;
        public IEnumerator<ExecutionRequirement> GetEnumerator()
        {
            yield return Requirement();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class LyingRequirements : IReadOnlyCollection<ExecutionRequirement>
    {
        public int Count => 1;

        public IEnumerator<ExecutionRequirement> GetEnumerator()
        {
            yield return Requirement();
            yield return Requirement();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class DelayedAuthorizer : IExecutionAuthorizer
    {
        private readonly TaskCompletionSource<ExecutionAuthorizationResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ExecutionAuthorizationContext? LastContext { get; private set; }

        public ValueTask<ExecutionAuthorizationResult> AuthorizeAsync(
            ExecutionAuthorizationContext context,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            return new(_completion.Task.WaitAsync(cancellationToken));
        }

        public void Complete(ExecutionAuthorizationResult result) => _completion.SetResult(result);
    }

    private sealed class NeutralConsumer
    {
        private readonly IExecutionAuthorizer _authorizer;
        private readonly Func<DateTimeOffset> _clock;
        private readonly string _expectedProviderId;
        private readonly Func<string, bool> _isCurrentRevision;
        private readonly Func<ExecutionRequirement, bool> _isKnownRequirement;
        private readonly bool _hostBindingValid;
        private readonly bool _evidenceRequired;

        public NeutralConsumer(
            IExecutionAuthorizer authorizer,
            Func<DateTimeOffset>? clock = null,
            string expectedProviderId = "test-provider",
            Func<string, bool>? isCurrentRevision = null,
            Func<ExecutionRequirement, bool>? isKnownRequirement = null,
            bool hostBindingValid = true,
            bool evidenceRequired = false)
        {
            _authorizer = authorizer;
            _clock = clock ?? (() => Now);
            _expectedProviderId = expectedProviderId;
            _isCurrentRevision = isCurrentRevision ?? (_ => true);
            _isKnownRequirement = isKnownRequirement ?? (requirement =>
                requirement.SchemaId == "capability:v1" && requirement.SchemaVersion == 1 && requirement.Capability == "read");
            _hostBindingValid = hostBindingValid;
            _evidenceRequired = evidenceRequired;
        }

        public int DispatchCount { get; private set; }

        public async ValueTask<ConsumerOutcome> ExecuteAsync(
            ExecutionAuthorizationContext context,
            CancellationToken cancellationToken = default)
        {
            if (!_hostBindingValid || context.Requirements.Count == 0
                || context.Requirements.Any(requirement => !_isKnownRequirement(requirement)))
            {
                return new(false);
            }

            ExecutionAuthorizationResult result;
            try
            {
                result = await _authorizer.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return new(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (result is null
                || result.Decision != ExecutionAuthorizationDecision.Allowed
                || result.AuthorizationRequestId != context.AuthorizationRequestId
                || result.ProviderId != _expectedProviderId
                || result.EvaluatedAt > _clock()
                || result.ExpiresAt is null
                || result.ExpiresAt <= _clock()
                || !_isCurrentRevision(context.Identity.ExecutionRevision)
                || (_evidenceRequired && string.IsNullOrWhiteSpace(result.EvidenceId)))
            {
                return new(false);
            }

            DispatchCount++;
            return new(true);
        }
    }

    private sealed record ConsumerOutcome(bool Dispatched);
}
