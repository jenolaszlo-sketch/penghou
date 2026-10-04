using System.Runtime.CompilerServices;
using Penghou.Model.Abstractions;
using Xunit;

namespace Penghou.Model.Tests;

public sealed class ModelTransportConformanceTests
{
    private static ModelTransportRequest<string> Request()
    {
        var context = new ModelExecutionContext("workflow", "activity", "agent", "correlation",
            new Dictionary<string, string> { ["tenant"] = "one" });
        var invocation = new ModelInvocation("r1", new ModelTarget("provider", "wire-model", "endpoint-profile"),
            ModelOperation.Stream, context, new ModelUsageIntent(10, 20, 0.01m, "USD"), "credential-profile");
        return new ModelTransportRequest<string>(invocation, "sensitive prompt");
    }

    [Fact]
    public async Task TypedInvocationPreservesExactDescriptorAndActualUsage()
    {
        var inner = new FakeTransport();
        var decorated = new AuthorityDecorator(inner);
        var request = Request();
        var result = await decorated.InvokeAsync(request);
        Assert.Same(request, inner.Seen);
        Assert.Equal("wire-model", inner.Seen!.Invocation.Target.Model);
        Assert.Equal("credential-profile", inner.Seen.Invocation.CredentialProfile);
        Assert.Equal("workflow", inner.Seen.Invocation.Context!.WorkflowId);
        Assert.Equal("activity", inner.Seen.Invocation.Context.ActivityId);
        Assert.Equal("agent", inner.Seen.Invocation.Context.AgentId);
        Assert.Equal("correlation", inner.Seen.Invocation.Context.CorrelationId);
        Assert.Equal("one", inner.Seen.Invocation.Context.Metadata["tenant"]);
        Assert.Equal(20, inner.Seen.Invocation.UsageIntent!.MaxOutputTokens);
        Assert.Equal(request.Invocation.RequestId, result.RequestId);
        Assert.Equal("provider-request", result.ProviderRequestId);
        Assert.Equal(7, result.Usage!.CachedInputTokens);
        Assert.Equal(3, result.Usage.CacheMissInputTokens);
        Assert.Null(result.Usage.TotalTokens);
        Assert.Equal(0.005m, result.Usage.Cost);
    }

    [Fact]
    public async Task RevokingAfterFirstCallPreventsAnySecondDispatch()
    {
        var inner = new FakeTransport();
        var decorated = new AuthorityDecorator(inner);
        await decorated.InvokeAsync(Request());
        decorated.Allowed = false;
        var denial = await Assert.ThrowsAsync<ModelAccessDeniedException>(async () => await decorated.InvokeAsync(Request()));
        Assert.Equal("r1", denial.RequestId);
        Assert.Equal(1, inner.Calls);
        Assert.Equal(0, inner.StreamOpens);
    }

    [Fact]
    public async Task EveryLazyEnumerationChecksCurrentAuthorityBeforeOpeningInnerStream()
    {
        var inner = new FakeTransport();
        var decorated = new AuthorityDecorator(inner);
        var request = Request();
        var stream = decorated.StreamAsync(request);
        Assert.Equal(0, inner.StreamOpens);
        var first = new List<ModelTransportResponse<string>>();
        await foreach (var item in stream) first.Add(item);
        Assert.Equal(2, first.Count);
        Assert.Equal(request.Invocation.RequestId, first[1].RequestId);
        Assert.NotNull(first[1].Usage);
        decorated.Allowed = false;
        await Assert.ThrowsAsync<ModelAccessDeniedException>(async () =>
        {
            await foreach (var _ in stream) { }
        });
        Assert.Equal(1, inner.StreamOpens);
        Assert.Equal(1, inner.StreamDisposals);
    }

    [Fact]
    public async Task CancelledStreamAndEarlyExitReleaseOwnedEnumeration()
    {
        var inner = new FakeTransport();
        var decorated = new AuthorityDecorator(inner);
        await foreach (var _ in decorated.StreamAsync(Request())) break;
        Assert.Equal(1, inner.StreamDisposals);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in decorated.StreamAsync(Request(), cancelled.Token)) { }
        });
        Assert.Equal(1, inner.StreamOpens);
        Assert.Equal(1, inner.StreamDisposals);
    }

    [Fact]
    public void EnvelopesDoNotRenderPayloadsInDefaultDescriptions()
    {
        Assert.DoesNotContain("sensitive prompt", Request().ToString());
        Assert.DoesNotContain("secret result", new ModelTransportResponse<string>("r", "secret result").ToString());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ModelUsage(cacheMissInputTokens: -1));
    }

    private sealed class AuthorityDecorator(FakeTransport inner) : IModelTransport<string, string>, IStreamingModelTransport<string, string>
    {
        public bool Allowed { get; set; } = true;
        private void Check(ModelTransportRequest<string> request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Allowed) throw new ModelAccessDeniedException(request.Invocation.RequestId);
        }
        public ValueTask<ModelTransportResponse<string>> InvokeAsync(ModelTransportRequest<string> request, CancellationToken cancellationToken = default)
        {
            Check(request, cancellationToken);
            return inner.InvokeAsync(request, cancellationToken);
        }
        public async IAsyncEnumerable<ModelTransportResponse<string>> StreamAsync(ModelTransportRequest<string> request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Check(request, cancellationToken);
            await foreach (var item in inner.StreamAsync(request, cancellationToken)) yield return item;
        }
    }

    private sealed class FakeTransport : IModelTransport<string, string>, IStreamingModelTransport<string, string>
    {
        public int Calls { get; private set; }
        public int StreamOpens { get; private set; }
        public int StreamDisposals { get; private set; }
        public ModelTransportRequest<string>? Seen { get; private set; }
        public ValueTask<ModelTransportResponse<string>> InvokeAsync(ModelTransportRequest<string> request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Seen = request;
            Calls++;
            return ValueTask.FromResult(Result(request, "reply"));
        }
        public async IAsyncEnumerable<ModelTransportResponse<string>> StreamAsync(ModelTransportRequest<string> request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StreamOpens++;
            try
            {
                await Task.Yield();
                yield return new ModelTransportResponse<string>(request.Invocation.RequestId, "delta");
                cancellationToken.ThrowIfCancellationRequested();
                yield return Result(request, "completed");
            }
            finally { StreamDisposals++; }
        }
        private static ModelTransportResponse<string> Result(ModelTransportRequest<string> request, string payload) =>
            new(request.Invocation.RequestId, payload, new ModelUsage(inputTokens: 10, outputTokens: 2, cachedInputTokens: 7,
                cacheMissInputTokens: 3, cost: 0.005m, currency: "USD"), "provider-request");
    }
}
