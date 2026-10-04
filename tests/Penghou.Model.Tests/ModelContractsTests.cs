using System.Collections;
using Penghou.Model.Abstractions;
using Xunit;

namespace Penghou.Model.Tests;

public sealed class ModelContractsTests
{
    private static ModelTarget Target() => new("provider", "model", "endpoint");
    private static ModelInvocation Invocation(ModelOperationHandle? handle = null) => new("request", Target(), ModelOperation.Generate, operationHandle: handle);

    [Fact]
    public void ContextSnapshotsMetadataAndExposesReadOnlyCopy()
    {
        var input = new Dictionary<string, string> { ["tenant"] = "one" };
        var context = new ModelExecutionContext("wf", "act", "agent", "corr", input);
        input["tenant"] = "two";
        Assert.Equal("one", context.Metadata["tenant"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)context.Metadata).Add("other", "value"));
    }

    [Fact]
    public void ContextRejectsMoreThanThirtyTwoMetadataItemsWhileEnumerating()
    {
        Assert.Throws<ArgumentException>(() => new ModelExecutionContext(metadata: Enumerable.Range(0, 40).ToDictionary(i => $"k{i}", _ => "v")));
        Assert.Throws<ArgumentException>(() => new ModelExecutionContext(metadata: new LyingDictionary(33, [])));
        Assert.Throws<ArgumentException>(() => new ModelExecutionContext(metadata: new LyingDictionary(0, [new("actual", "value")])));
    }

    [Fact]
    public void BatchSnapshotsEnumerationAndRejectsAThousandAndOneEvenWhenCountLies()
    {
        var source = new LyingCollection<ModelInvocation>(Enumerable.Repeat(Invocation(), 1001));
        Assert.Throws<ArgumentException>(() => new ModelTransportRequest<string>(Invocation(), "payload", source));
        var accepted = new ModelTransportRequest<string>(Invocation(), "payload", new LyingCollection<ModelInvocation>(Enumerable.Repeat(Invocation(), 2)));
        Assert.Equal(2, accepted.Items.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<ModelInvocation>)accepted.Items).Add(Invocation()));
        var mutableItems = new List<ModelInvocation> { Invocation() };
        var snapshot = new ModelTransportRequest<string>(Invocation(), "payload", mutableItems);
        mutableItems.Clear();
        Assert.Single(snapshot.Items);
    }

    [Fact]
    public void IdentifiersRejectMalformedUnicodeWhitespaceAndOversizeUtf8()
    {
        Assert.Throws<ArgumentException>(() => new ModelTarget("bad\uD800", "m", "e"));
        Assert.Throws<ArgumentException>(() => new ModelTarget("provider name", "m", "e"));
        Assert.Throws<ArgumentException>(() => new ModelTarget(new string('é', 129), "m", "e"));
        Assert.Throws<ArgumentException>(() => new ModelOperation(""));
    }

    [Fact]
    public void HandleIdentityMustMatchInvocationTarget()
    {
        var other = new ModelOperationHandle("opaque", new ModelTarget("provider", "different", "endpoint"));
        Assert.Throws<ArgumentException>(() => Invocation(other));
        var matching = new ModelOperationHandle("opaque", Target());
        Assert.Equal(matching, Invocation(matching).OperationHandle);
    }

    [Fact]
    public void UsagePreservesUnknownFieldsAndRequiresCostCurrencyPair()
    {
        var usage = new ModelUsage(inputTokens: 3);
        Assert.Equal(3, usage.InputTokens);
        Assert.Null(usage.OutputTokens);
        Assert.Null(usage.TotalTokens);
        Assert.Throws<ArgumentException>(() => new ModelUsage(cost: 0.2m));
        Assert.Throws<ArgumentException>(() => new ModelUsage(cost: 0.2m, currency: "usd"));
        Assert.Equal("USD", new ModelUsage(cost: 0.2m, currency: "USD").Currency);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ModelUsage(totalTokens: -1));
        var response = new ModelTransportResponse<string>("req-1", "event", usage);
        Assert.Equal("req-1", response.RequestId);
        Assert.Same(usage, response.Usage);
    }

    [Fact]
    public void BudgetDenialIsDistinctAndStillAnAccessDenial()
    {
        var denied = new ModelBudgetExceededException("req-denied");
        Assert.IsAssignableFrom<ModelAccessDeniedException>(denied);
        Assert.IsAssignableFrom<ModelTransportException>(denied);
        Assert.NotEqual(typeof(ModelUnavailableException), denied.GetType());
        Assert.Equal("req-denied", denied.RequestId);
        Assert.Equal("budget-exceeded", denied.ReasonCode);
        Assert.Null(denied.InnerException);
        Assert.DoesNotContain("secret", denied.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequestRequiresNonNullTypedDataAndKeepsInvocationIntact()
    {
        var invocation = new ModelInvocation("req", Target(), ModelOperation.BatchSubmit, usageIntent: new ModelUsageIntent(estimatedInputTokens: 10));
        var request = new ModelTransportRequest<string>(invocation, "immutable data");
        Assert.Same(invocation, request.Invocation);
        Assert.Equal(10, request.Invocation.UsageIntent!.EstimatedInputTokens);
        Assert.Throws<ArgumentNullException>(() => new ModelTransportRequest<string>(invocation, null!));
    }

    [Fact]
    public void ContractAssemblyHasNoProductOrProviderDependencies()
    {
        var names = typeof(ModelTarget).Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();
        Assert.DoesNotContain(names, n => n.StartsWith("Penghou.Baize", StringComparison.Ordinal));
        Assert.DoesNotContain(names, n => n.StartsWith("Penghou.Hufu", StringComparison.Ordinal));
        Assert.DoesNotContain(names, n => n.Contains("OpenAI", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class LyingCollection<T>(IEnumerable<T> items) : ICollection<T>
    {
        private readonly IEnumerable<T> _items = items;
        public int Count => 0;
        public bool IsReadOnly => true;
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(T item) => throw new NotSupportedException();
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
    }

    private sealed class LyingDictionary(int declaredCount, IEnumerable<KeyValuePair<string, string>> items) : IReadOnlyDictionary<string, string>
    {
        private readonly Dictionary<string, string> _items = items.ToDictionary(p => p.Key, p => p.Value);
        public int Count => declaredCount;
        public IEnumerable<string> Keys => _items.Keys;
        public IEnumerable<string> Values => _items.Values;
        public string this[string key] => _items[key];
        public bool ContainsKey(string key) => _items.ContainsKey(key);
        public bool TryGetValue(string key, out string value) => _items.TryGetValue(key, out value!);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
