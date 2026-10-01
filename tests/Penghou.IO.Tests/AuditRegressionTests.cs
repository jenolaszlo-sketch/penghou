using System.Collections;
using Penghou.IO.Abstractions;
using Penghou.IO.Local;
using Xunit;

namespace Penghou.IO.Tests;

public sealed class AuditRegressionTests
{
    private static readonly WorkspaceId Workspace = new("audit");
    private static readonly HostInvocation Invocation = new("i", "s", "e", "a", "scope", null, default);
    [Fact]
    public void CodecRejectsHugePatchCountBeforeIndexingOrAllocation()
    {
        var request = new FilePatchRequest(Invocation, Workspace, new("a.txt"), new("v"),
            new HostileList(), new(1, 1, 1));
        Assert.Throws<ArgumentException>(() => ResourceRequestIdentity.Compute(request));
    }
    [Fact]
    public void CodecRejectsOversizedWriteAndAggregateReplacement()
    {
        Assert.Throws<ArgumentException>(() => ResourceRequestIdentity.Compute(new FileWriteRequest(
            Invocation, Workspace, new("a.txt"), new byte[2], new(1), new(WritePreconditionKind.MustNotExist))));
        Assert.Throws<ArgumentException>(() => ResourceRequestIdentity.Compute(new FilePatchRequest(
            Invocation, Workspace, new("a.txt"), new("v"),
            [new(0, 0, new byte[2]), new(1, 0, new byte[2])], new(2, 3, 4))));
    }
    [Fact]
    public async Task ContinuationRejectsUnicodeCaseVariantOutsideCanonicalEquivalence()
    {
        var root = Path.Combine(Path.GetTempPath(), "penghou-audit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "café"));
        try
        {
            File.WriteAllText(Path.Combine(root, "café", "a"), "a");
            File.WriteAllText(Path.Combine(root, "café", "b"), "b");
            using var reader = new LocalWorkspaceReader(Workspace, root, new Permit());
            var request = Bind(new(Invocation, Workspace, new("café"), 1, 100, 4096));
            var first = await reader.ListDirectoryAsync(request);
            Assert.True(first.Succeeded);
            Assert.NotNull(first.Value!.Continuation);
            var resumed = Bind(request with { Path = new("cafÉ"), Continuation = first.Value.Continuation });
            var rejected = await reader.ListDirectoryAsync(resumed);
            Assert.Equal(ResourceFailureKind.AuthorizationDenied, rejected.Failure);
            var correct = Bind(request with { Path = new("CAFé"), Continuation = first.Value.Continuation });
            Assert.True((await reader.ListDirectoryAsync(correct)).Succeeded);
        }
        finally { Directory.Delete(root, true); }
    }
    private static DirectoryListRequest Bind(DirectoryListRequest r) => r with
    { Invocation = r.Invocation with { RequestIdentity = ResourceRequestIdentity.Compute(r) } };
    private sealed class Permit : IResourceAuthorizer
    {
        public ValueTask<ResourceAuthorizationDecision> AuthorizeAsync(ResourceAuthorizationRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
    }
    private sealed class HostileList : IReadOnlyList<TextPatch>
    {
        public int Count => int.MaxValue;
        public TextPatch this[int index] => throw new InvalidOperationException("Must not index.");
        public IEnumerator<TextPatch> GetEnumerator() => throw new InvalidOperationException("Must not enumerate.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
