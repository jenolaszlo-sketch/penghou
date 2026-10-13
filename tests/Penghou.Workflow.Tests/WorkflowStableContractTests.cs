using System.Text.Json;
using System.Text.Json.Serialization;
using Penghou.Workflow.Abstractions;
using Xunit;

namespace Penghou.Workflow.Tests;

public sealed class WorkflowStableContractTests
{
    private static readonly JsonSerializerOptions Strict = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    private static ExecutionIdentity Identity() => new("execution-1", "operation-1", 1, "revision-1");
    private static ExecutionRequirement Requirement(string capability = "read") => new("profile", 1, capability, "resource");
    private static string Vector(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "vectors", name));

    [Fact]
    public void ContextEqualityIsOrderedStructuralAndOwnsItsSnapshot()
    {
        var source = new List<ExecutionRequirement> { Requirement(), Requirement("write") };
        var first = new ExecutionAuthorizationContext(Identity(), "request", source);
        var second = new ExecutionAuthorizationContext(Identity(), "request", source.ToArray());
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new ExecutionAuthorizationContext(Identity(), "request", source.AsEnumerable().Reverse().ToArray()));
        source.Clear();
        Assert.Equal(2, first.Requirements.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<ExecutionRequirement>)first.Requirements)[0] = Requirement("delete"));
        var clone = first with { };
        Assert.Equal(first, clone);
        Assert.Equal(first, JsonSerializer.Deserialize<ExecutionAuthorizationContext>(JsonSerializer.Serialize(first)));
        Assert.NotEqual(first, new ExecutionAuthorizationContext(Identity(), "Request", first.Requirements));
    }

    [Fact]
    public void FixedVersionOneVectorsRoundTripValuesAndPropertySets()
    {
        var context = JsonSerializer.Deserialize<ExecutionAuthorizationContext>(Vector("context-v1.json"), Strict)!;
        Assert.Equal("urn:resource:α", context.Requirements.Single().Resource);
        Assert.Equal("steps/read", context.OperationPath);
        Assert.Equal(context, JsonSerializer.Deserialize<ExecutionAuthorizationContext>(JsonSerializer.Serialize(context), Strict));
        using var emitted = JsonDocument.Parse(JsonSerializer.Serialize(context));
        Assert.Equal(new[] { "AuthorizationRequestId", "Identity", "Requirements", "SchemaVersion" }, emitted.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
        var result = JsonSerializer.Deserialize<ExecutionAuthorizationResult>(Vector("allowed-v1.json"), Strict)!;
        Assert.Equal(ExecutionAuthorizationDecision.Allowed, result.Decision);
        Assert.Equal(TimeSpan.Zero, result.EvaluatedAt.Offset);
        Assert.Equal(result, JsonSerializer.Deserialize<ExecutionAuthorizationResult>(JsonSerializer.Serialize(result), Strict));
    }

    [Fact]
    public void DefaultAndStrictProfilesHaveExplicitUnknownMissingAndNullBehavior()
    {
        var json = Vector("context-v1.json");
        var unknown = json.TrimEnd() [..^1] + ",\"Future\":true}";
        Assert.NotNull(JsonSerializer.Deserialize<ExecutionAuthorizationContext>(unknown));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ExecutionAuthorizationContext>(unknown, Strict));
        var missingSchema = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        missingSchema.AsObject().Remove("SchemaVersion");
        Assert.NotNull(JsonSerializer.Deserialize<ExecutionAuthorizationContext>(missingSchema.ToJsonString(), Strict));
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ExecutionAuthorizationContext>(json.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":2"), Strict));
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ExecutionAuthorizationContext>("{\"AuthorizationRequestId\":\"request\",\"Requirements\":[]}", Strict));
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ExecutionAuthorizationContext>(json.Replace("\"Requirements\":[", "\"Requirements\":null,\"Discard\":["), Strict));
        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ExecutionAuthorizationResult>(Vector("allowed-v1.json").Replace("\"Decision\":1", "\"Decision\":99"), Strict));
        var defaultDecision = Vector("allowed-v1.json").Replace("\"Decision\":1,", "");
        Assert.Equal(ExecutionAuthorizationDecision.Unavailable, JsonSerializer.Deserialize<ExecutionAuthorizationResult>(defaultDecision, Strict)!.Decision);
    }

    [Theory]
    [InlineData(128, 128)]
    [InlineData(64, 128)]
    public void BoundsMeasureUtf8AndPreserveOpaqueUnicode(int repeats, int limit)
    {
        var value = new string(repeats == 128 ? 'a' : 'é', repeats);
        Assert.Equal(limit, System.Text.Encoding.UTF8.GetByteCount(value));
        Assert.Equal(value, new ExecutionRequirement(value, 1, "read", "resource").SchemaId);
        Assert.Throws<ArgumentException>(() => new ExecutionRequirement(value + "a", 1, "read", "resource"));
        Assert.NotEqual(Requirement() with { }, new ExecutionRequirement("Profile", 1, "read", "resource"));
        Assert.Throws<ArgumentException>(() => new ExecutionIdentity("\ud800", "op", 1, "rev"));
    }
}
