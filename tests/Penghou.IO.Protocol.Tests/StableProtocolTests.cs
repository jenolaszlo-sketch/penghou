using System.Reflection;
using System.Text.Json;
using Penghou.IO.Abstractions;
using Xunit;

namespace Penghou.IO.Protocol.Tests;

public sealed class StableProtocolTests
{
    private static readonly HostInvocation Invocation = new("i", "s", "e", "a", "scope", null, default);

    [Fact]
    public void IndependentFixedBytesAndHashesCoverEveryV1Opcode()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "protocol-v1-vectors.json")));
        foreach (var v in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            string S(string key) => v.GetProperty(key).GetString()!;
            int I(string key) => v.GetProperty(key).GetInt32();
            WorkspaceId workspace = v.TryGetProperty("workspace", out var w) ? new(w.GetString()!) : default;
            WorkspacePath path = v.TryGetProperty("path", out var p) ? new(p.GetString()!) : default;
            WritePrecondition Condition() => new((WritePreconditionKind)I("preconditionKind"),
                v.GetProperty("preconditionVersion").ValueKind == JsonValueKind.Null ? null : new ResourceVersion(S("preconditionVersion")));
            var identity = I("opcode") switch
            {
                1 => ResourceRequestIdentity.Compute(new FileReadRequest(Invocation, workspace, path, new(I("maxBytes")))),
                2 => ResourceRequestIdentity.Compute(new FileMetadataRequest(Invocation, workspace, path)),
                3 => ResourceRequestIdentity.Compute(new DirectoryListRequest(Invocation, workspace, path, I("maxEntries"), I("maxCandidatesScanned"), I("maxOutputBytes"),
                    v.GetProperty("continuation").ValueKind == JsonValueKind.Null ? null : new DirectoryContinuation(S("continuation")))),
                4 => ResourceRequestIdentity.Compute(new FileWriteRequest(Invocation, workspace, path, Convert.FromHexString(S("contentHex")), new(I("maxBytes")), Condition())),
                6 => ResourceRequestIdentity.Compute(new FileDeleteRequest(Invocation, workspace, path, new(S("expectedVersion")))),
                7 => ResourceRequestIdentity.Compute(new DirectoryCreateRequest(Invocation, workspace, path)),
                8 => ResourceRequestIdentity.Compute(new FileMoveRequest(Invocation, workspace, new(S("source")), new(S("destination")), new(S("expectedVersion")), Condition())),
                9 => ResourceRequestIdentity.Compute(new WebReadRequest(Invocation, new(new Uri(S("url"))), new(I("maxBytes")))),
                _ => throw new InvalidOperationException("Unsupported fixture opcode.")
            };
            Assert.Equal(S("identity"), identity.Value);
            if (v.TryGetProperty("canonicalPath", out var expectedPath))
                Assert.Equal(expectedPath.GetString(), WindowsWorkspacePath.ToIdentityPath(path));
        }
    }

    [Fact]
    public void StableSignatureClosureContainsOnlyThe18DataTypes()
    {
        var names = new[] { "WorkspaceId", "WorkspacePath", "WebUrl", "ResourceVersion", "DirectoryContinuation", "RequestIdentity", "HostInvocation", "IoLimits", "WritePreconditionKind", "WritePrecondition", "FileWriteRequest", "FileDeleteRequest", "DirectoryCreateRequest", "FileMoveRequest", "FileReadRequest", "FileMetadataRequest", "DirectoryListRequest", "WebReadRequest" };
        Assert.Equal(names.Order(StringComparer.Ordinal), typeof(WorkspaceId).Assembly.GetExportedTypes().Select(t => t.Name).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(typeof(WorkspaceId).Assembly.GetExportedTypes(), t => t.IsInterface);
        Assert.DoesNotContain(typeof(WorkspaceId).Assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Penghou.", StringComparison.Ordinal));
        Assert.Equal(new[] { "Penghou.IO.Identity.Abstractions" }, typeof(ResourceRequestIdentity).Assembly.GetReferencedAssemblies().Where(a => a.Name!.StartsWith("Penghou.", StringComparison.Ordinal)).Select(a => a.Name));
        Assert.Equal(new Version(1, 0, 0, 0), typeof(WorkspaceId).Assembly.GetName().Version);
        Assert.Equal(new Version(1, 0, 0, 0), typeof(ResourceRequestIdentity).Assembly.GetName().Version);
    }

    [Fact]
    public void BorrowedBufferEqualityAndInvocationExclusionAreExplicit()
    {
        byte[] bytes = [1, 2];
        var request = new FileWriteRequest(Invocation, new("ws"), new("a"), bytes, new(2), new(WritePreconditionKind.MustNotExist));
        var sameBytesDifferentSegment = request with { Content = new byte[] { 1, 2 } };
        Assert.NotEqual(request, sameBytesDifferentSegment);
        Assert.Equal(ResourceRequestIdentity.Compute(request), ResourceRequestIdentity.Compute(sameBytesDifferentSegment));
        var before = ResourceRequestIdentity.Compute(request);
        bytes[0] = 3;
        Assert.Equal(3, request.Content.Span[0]);
        Assert.NotEqual(before, ResourceRequestIdentity.Compute(request));
        Assert.Equal(ResourceRequestIdentity.Compute(request), ResourceRequestIdentity.Compute(request with { Invocation = null! }));
    }

    [Fact]
    public void PathCanonicalizationIsIdempotentAndDoesNotNormalizeUnicodeOrEscapes()
    {
        foreach (var text in new[] { "", "Src/A.txt", "café.txt", "cafe\u0301.txt", "a%2Fb", "#name" })
        {
            var path = WindowsWorkspacePath.Normalize(new(text));
            Assert.Equal(text, path.Value);
            var canonical = WindowsWorkspacePath.ToIdentityPath(path);
            Assert.Equal(canonical, WindowsWorkspacePath.ToIdentityPath(new(canonical)));
        }
        Assert.NotEqual(WindowsWorkspacePath.ToIdentityPath(new("café")), WindowsWorkspacePath.ToIdentityPath(new("cafe\u0301")));
    }

    [Fact]
    public void EncodingBoundsAndDefaultErrorsRemainFixed()
    {
        FileReadRequest Read(int limit) => new(Invocation, new("ws"), new("a"), new(limit));
        Assert.Throws<ArgumentNullException>(() => ResourceRequestIdentity.Compute((FileReadRequest)null!));
        Assert.Throws<ArgumentException>(() => ResourceRequestIdentity.Compute(Read(-1)));
        Assert.Throws<ArgumentException>(() => ResourceRequestIdentity.Compute(Read(16777217)));
        Assert.Throws<ArgumentException>(() => ResourceRequestIdentity.Compute(Read(1) with { Workspace = new(new string('a', 257)) }));
        Assert.Throws<ArgumentException>(() => ResourceRequestIdentity.Compute(new DirectoryListRequest(Invocation, new("ws"), WorkspacePath.Root, 1, 1, 1, new(new string('a', 16385)))));
        Assert.Throws<ArgumentException>(() => WindowsWorkspacePath.Normalize(default));
    }
}
