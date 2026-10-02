using System.Text;
using Penghou.IO.Abstractions;
using Xunit;

namespace Penghou.IO.Abstractions.Codec.Tests;

public sealed class ResourceRequestIdentityTests
{
    private static readonly WorkspaceId Workspace = new("workspace-main");
    private static readonly HostInvocation Invocation = new(
        "invocation-a", "subject-a", "effect-a", "attempt-a", "scope-a", null,
        new RequestIdentity("host-minted-a"));

    [Fact]
    public void ReadIdentity_IsVersionedStableAndExcludesInvocationContext()
    {
        var first = ResourceRequestIdentity.Compute(Read("src/Foo.cs", Invocation));
        var second = ResourceRequestIdentity.Compute(Read("SRC/foo.CS", Invocation with
        {
            InvocationId = "another-invocation",
            SubjectId = "another-subject",
            EffectId = "another-effect",
            AttemptId = "another-attempt",
            EffectScopeId = "another-scope",
            ParentEffectScopeId = "parent",
            RequestIdentity = new RequestIdentity("different")
        }));

        Assert.Equal(first, second);
        Assert.StartsWith(ResourceRequestIdentity.IdentityPrefix, first.Value, StringComparison.Ordinal);
        Assert.Equal(64, first.Value[ResourceRequestIdentity.IdentityPrefix.Length..].Length);
        Assert.All(first.Value[ResourceRequestIdentity.IdentityPrefix.Length..], c => Assert.Contains(c, "0123456789abcdef"));
    }

    [Fact]
    public void GoldenReadAndRootListVectors_MatchSchemaV1()
    {
        var read = Read("src/Foo.txt", workspace: new WorkspaceId("ws"), maxBytes: 4096);
        var list = new DirectoryListRequest(Invocation, new WorkspaceId("ws"), WorkspacePath.Root, 10, 100, 4096);

        Assert.Equal(
            "penghou-io:request:v1:sha256:c3f2dec4a7a5f1df22441d28c427781d5612d0a5b079a0aac5bcf0c8cef7087b",
            ResourceRequestIdentity.Compute(read).Value);
        Assert.Equal(
            "penghou-io:request:v1:sha256:f9c6225c276e1675b44c6f340a8ea3f55182974d6aa0acdb86e39dd5335c4b4d",
            ResourceRequestIdentity.Compute(list).Value);
    }

    [Fact]
    public void ReadIdentity_BindsWorkspaceTargetAndBound()
    {
        var baseline = ResourceRequestIdentity.Compute(Read("src/Foo.cs"));

        Assert.NotEqual(baseline, ResourceRequestIdentity.Compute(Read("src/Bar.cs")));
        Assert.NotEqual(baseline, ResourceRequestIdentity.Compute(Read("src/Foo.cs", maxBytes: 101)));
        Assert.NotEqual(baseline, ResourceRequestIdentity.Compute(Read("src/Foo.cs", workspace: new WorkspaceId("Workspace-main"))));
    }

    [Fact]
    public void OperationDomainsSeparateEverySupportedRequestKind()
    {
        var requests = new RequestIdentity[]
        {
            ResourceRequestIdentity.Compute(Read("src/Foo.cs")),
            ResourceRequestIdentity.Compute(new FileMetadataRequest(Invocation, Workspace, new("src/Foo.cs"))),
            ResourceRequestIdentity.Compute(List("src")),
            ResourceRequestIdentity.Compute(Write("src/Foo.cs", [1, 2, 3])),
            ResourceRequestIdentity.Compute(new FileDeleteRequest(Invocation, Workspace, new("src/Foo.cs"), new("v1"))),
            ResourceRequestIdentity.Compute(new DirectoryCreateRequest(Invocation, Workspace, new("src/new"))),
            ResourceRequestIdentity.Compute(new FileMoveRequest(Invocation, Workspace, new("src/a"), new("src/b"), new("v1"), new(WritePreconditionKind.MustNotExist))),
            ResourceRequestIdentity.Compute(new WebReadRequest(Invocation, new(new Uri("https://example.test/a?q=1")), new(100)))
        };

        Assert.Equal(requests.Length, requests.Distinct().Count());
    }

    [Fact]
    public void WriteIdentityBindsContentLimitsAndPreconditions()
    {
        var baseline = ResourceRequestIdentity.Compute(Write("src/Foo.cs", [1, 2, 3]));

        Assert.NotEqual(baseline, ResourceRequestIdentity.Compute(Write("src/Foo.cs", [1, 2, 4])));
        Assert.NotEqual(baseline, ResourceRequestIdentity.Compute(Write("src/Foo.cs", [1, 2, 3], maxBytes: 99)));
        Assert.NotEqual(baseline, ResourceRequestIdentity.Compute(Write(
            "src/Foo.cs", [1, 2, 3], precondition: new(WritePreconditionKind.MustMatchVersion, new ResourceVersion("v1")))));
        Assert.NotEqual(
            ResourceRequestIdentity.Compute(Write("src/Foo.cs", [1, 2, 3], precondition: new(WritePreconditionKind.MustMatchVersion, new ResourceVersion("v1")))),
            ResourceRequestIdentity.Compute(Write("src/Foo.cs", [1, 2, 3], precondition: new(WritePreconditionKind.MustMatchVersion, new ResourceVersion("v2")))));
    }

    [Fact]
    public void DeleteMoveAndCreateIdentitiesBindEveryResourceAndVersionField()
    {
        var delete = new FileDeleteRequest(Invocation, Workspace, new("src/a"), new("v1"));
        Assert.NotEqual(ResourceRequestIdentity.Compute(delete), ResourceRequestIdentity.Compute(delete with { Path = new("src/b") }));
        Assert.NotEqual(ResourceRequestIdentity.Compute(delete), ResourceRequestIdentity.Compute(delete with { ExpectedVersion = new("v2") }));

        var create = new DirectoryCreateRequest(Invocation, Workspace, new("src/new"));
        Assert.NotEqual(ResourceRequestIdentity.Compute(create), ResourceRequestIdentity.Compute(create with { Path = new("src/other") }));

        var move = new FileMoveRequest(Invocation, Workspace, new("src/a"), new("src/b"), new("v1"), new(WritePreconditionKind.MustNotExist));
        Assert.NotEqual(ResourceRequestIdentity.Compute(move), ResourceRequestIdentity.Compute(move with { Source = new("src/c") }));
        Assert.NotEqual(ResourceRequestIdentity.Compute(move), ResourceRequestIdentity.Compute(move with { Destination = new("src/c") }));
        Assert.NotEqual(ResourceRequestIdentity.Compute(move), ResourceRequestIdentity.Compute(move with { ExpectedSourceVersion = new("v2") }));
        Assert.NotEqual(ResourceRequestIdentity.Compute(move), ResourceRequestIdentity.Compute(move with
        {
            DestinationPrecondition = new(WritePreconditionKind.MustMatchVersion, new ResourceVersion("destination-v1"))
        }));
    }

    [Fact]
    public void DirectoryIdentityBindsRootLimitsAndContinuation()
    {
        var root = List("");

        Assert.NotEqual(ResourceRequestIdentity.Compute(root), ResourceRequestIdentity.Compute(List("src")));
        Assert.NotEqual(ResourceRequestIdentity.Compute(root), ResourceRequestIdentity.Compute(List("", maxEntries: 2)));
        Assert.NotEqual(ResourceRequestIdentity.Compute(root), ResourceRequestIdentity.Compute(List("", maxCandidates: 2)));
        Assert.NotEqual(ResourceRequestIdentity.Compute(root), ResourceRequestIdentity.Compute(List("", maxOutputBytes: 2)));
        Assert.NotEqual(
            ResourceRequestIdentity.Compute(root),
            ResourceRequestIdentity.Compute(List("", continuation: new DirectoryContinuation("next-page"))));
    }

    [Fact]
    public void WebIdentityCanonicalizesHostSchemeAndExcludesFragment()
    {
        var first = new WebReadRequest(Invocation, new(new Uri("HTTPS://ExAmPlE.test:443/a?q=x#one")), new(4096));
        var equivalent = new WebReadRequest(Invocation, new(new Uri("https://example.test/a?q=x#two")), new(4096));
        var otherPath = new WebReadRequest(Invocation, new(new Uri("https://example.test/b?q=x")), new(4096));

        Assert.Equal(ResourceRequestIdentity.Compute(first), ResourceRequestIdentity.Compute(equivalent));
        Assert.NotEqual(ResourceRequestIdentity.Compute(first), ResourceRequestIdentity.Compute(otherPath));
        Assert.NotEqual(ResourceRequestIdentity.Compute(first), ResourceRequestIdentity.Compute(first with { Limits = new IoLimits(4095) }));
    }

    [Theory]
    [InlineData("/absolute")]
    [InlineData("//server/share")]
    [InlineData("C:/drive")]
    [InlineData("folder\\child")]
    [InlineData("a//b")]
    [InlineData("a/")]
    [InlineData(".")]
    [InlineData("a/./b")]
    [InlineData("a/../b")]
    [InlineData("file.txt:stream")]
    [InlineData("name ")]
    [InlineData("name.")]
    [InlineData("CON")]
    [InlineData("aux.txt")]
    [InlineData("CON .txt")]
    [InlineData("conin$.log")]
    [InlineData("clock$.txt")]
    [InlineData("COM3.txt")]
    [InlineData("lpt²")]
    [InlineData("bad?name")]
    public void WindowsPath_RejectsInvalidForms(string value)
    {
        Assert.Throws<ArgumentException>(() => WindowsWorkspacePath.Normalize(new WorkspacePath(value)));
    }

    [Fact]
    public void WindowsPath_PreservesOriginalCaseAndEnforcesRootAndLength()
    {
        var path = new WorkspacePath("Src/Foo.cs");
        Assert.Equal(path, WindowsWorkspacePath.Normalize(path));
        Assert.Equal(WorkspacePath.Root, WindowsWorkspacePath.Normalize(WorkspacePath.Root));
        Assert.Throws<ArgumentException>(() => WindowsWorkspacePath.Normalize(WorkspacePath.Root, allowRoot: false));
        Assert.Equal(ResourceRequestIdentity.Compute(Read("src/Foo.cs")), ResourceRequestIdentity.Compute(Read("SRC/foo.CS")));
        Assert.NotEqual(ResourceRequestIdentity.Compute(Read("café.txt")), ResourceRequestIdentity.Compute(Read("cafÉ.txt")));
        Assert.Equal(new string('a', 2048), WindowsWorkspacePath.Normalize(new WorkspacePath(new string('a', 2048))).Value);
        Assert.Throws<ArgumentException>(() => WindowsWorkspacePath.Normalize(new WorkspacePath(new string('a', 2049))));
    }

    [Fact]
    public void WindowsPath_RejectsIllFormedUnicode()
    {
        Assert.Throws<ArgumentException>(() => WindowsWorkspacePath.Normalize(new WorkspacePath("bad\ud800name")));
    }

    [Theory]
    [InlineData("file:///tmp/file")]
    [InlineData("ftp://example.test/file")]
    [InlineData("https://user@example.test/file")]
    public void WebIdentity_RejectsUnsupportedOrCredentialBearingUrls(string value)
    {
        var request = new WebReadRequest(Invocation, new(new Uri(value)), new(100));
        Assert.Throws<ArgumentException>(() => ResourceRequestIdentity.Compute(request));
    }

    private static FileReadRequest Read(string path, HostInvocation? invocation = null, int maxBytes = 100, WorkspaceId? workspace = null) =>
        new(invocation ?? Invocation, workspace ?? Workspace, new WorkspacePath(path), new IoLimits(maxBytes));

    private static FileWriteRequest Write(string path, byte[] content, int maxBytes = 100, WritePrecondition? precondition = null) =>
        new(Invocation, Workspace, new WorkspacePath(path), content, new IoLimits(maxBytes), precondition ?? new(WritePreconditionKind.MustNotExist));

    private static DirectoryListRequest List(
        string path,
        int maxEntries = 10,
        int maxCandidates = 100,
        int maxOutputBytes = 4096,
        DirectoryContinuation? continuation = null) =>
        new(Invocation, Workspace, new WorkspacePath(path), maxEntries, maxCandidates, maxOutputBytes, continuation);
}
