using System.Reflection;
using System.Text.Json;
using Penghou.IO.Abstractions;
using Penghou.IO.Local;
using Penghou.Luban;

var expectForwarded = args.Contains("--expect-forwarded", StringComparer.Ordinal);
var workspace = new WorkspaceId("ws");
var invocation = new HostInvocation("i", "s", "e", "a", "scope", null, default);
var request = new FileReadRequest(invocation, workspace, new("src/Foo.txt"), new(4096));
Check(ResourceRequestIdentity.Compute(request).Value == "penghou-io:request:v1:sha256:c3f2dec4a7a5f1df22441d28c427781d5612d0a5b079a0aac5bcf0c8cef7087b", "Published v1 read identity changed.");
Check(WindowsWorkspacePath.ToIdentityPath(new("café.txt")) == "CAFé.TXT", "Path profile changed.");
var names = new[] { "WorkspaceId", "WorkspacePath", "WebUrl", "ResourceVersion", "DirectoryContinuation", "RequestIdentity", "HostInvocation", "IoLimits", "WritePreconditionKind", "WritePrecondition", "FileWriteRequest", "FileDeleteRequest", "DirectoryCreateRequest", "FileMoveRequest", "FileReadRequest", "FileMetadataRequest", "DirectoryListRequest", "WebReadRequest" };
foreach (var name in names)
{
    var qualified = $"Penghou.IO.Abstractions.{name}, Penghou.IO.Abstractions, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
    var type = Type.GetType(qualified, throwOnError: true)!;
    if (expectForwarded)
        Check(type.Assembly.GetName().Name == "Penghou.IO.Identity.Abstractions", "Old assembly-qualified name failed forwarding: " + name);
}
var json = JsonSerializer.Serialize(request);
var jsonFile = args.FirstOrDefault(a => a.StartsWith("--json=", StringComparison.Ordinal))?[7..];
if (jsonFile is not null)
{
    if (expectForwarded) Check(File.ReadAllText(jsonFile) == json, "Default JSON data shape changed across forwarding.");
    else File.WriteAllText(jsonFile, json);
}
var restored = JsonSerializer.Deserialize<FileReadRequest>(json)!;
Check(restored == request, "Request JSON roundtrip changed data.");
// Serialized legacy type discriminators are resolved before deserializing data.
var legacyType = Type.GetType("Penghou.IO.Abstractions.FileReadRequest, Penghou.IO.Abstractions, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", true)!;
Check(JsonSerializer.Deserialize(json, legacyType) is FileReadRequest, "Legacy type discriminator JSON restoration failed.");

if (OperatingSystem.IsWindows())
{
    var root = Path.Combine(Path.GetTempPath(), "penghou-forwarded-consumer-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
        File.WriteAllText(Path.Combine(root, "a.txt"), "hello");
        var provider = new LocalWorkspaceProvider(workspace, root, LocalPatchNamespace.HostControlled);
        using var reader = provider.OpenReader(new Permit());
        var read = new FileReadRequest(invocation, workspace, new("a.txt"), new(32));
        read = read with { Invocation = invocation with { RequestIdentity = ResourceRequestIdentity.Compute(read) } };
        var value = await reader.ReadFileAsync(read);
        Check(value.Succeeded && System.Text.Encoding.UTF8.GetString(value.Value!.Content.Span) == "hello", "Unchanged Local DLL read failed.");
        var metadata = new FileMetadataRequest(invocation, workspace, new("a.txt"));
        metadata = metadata with { Invocation = invocation with { RequestIdentity = ResourceRequestIdentity.Compute(metadata) } };
        Check((await reader.GetFileMetadataAsync(metadata)).Value!.Exists, "Unchanged Local metadata failed.");
        var list = new DirectoryListRequest(invocation, workspace, WorkspacePath.Root, 10, 10, 4096);
        list = list with { Invocation = invocation with { RequestIdentity = ResourceRequestIdentity.Compute(list) } };
        Check((await reader.ListDirectoryAsync(list)).Value!.Entries.Any(e => e.Name == "a.txt"), "Unchanged Local list failed.");
        var deniedProvider = provider.OpenReader(new Deny());
        using (deniedProvider)
            Check((await deniedProvider.ReadFileAsync(read)).Failure == ResourceFailureKind.AuthorizationDenied, "Unchanged Local denial changed.");
        var journal = new Journal();
        var write = new FileWriteRequest(invocation, workspace, new("a.txt"), System.Text.Encoding.UTF8.GetBytes("changed"), new(32), new(WritePreconditionKind.MustMatchVersion, value.Value!.Version));
        write = write with { Invocation = invocation with { RequestIdentity = ResourceRequestIdentity.Compute(write) } };
        Check((await provider.OpenWriter(new Permit(), journal).WriteFileAsync(write)).Succeeded, "Unchanged Local conditional write failed.");
        Check(journal.Starts == 1 && journal.Completions == 1, "Unchanged Local journal behavior changed.");
        var runtime = new FileEffectRuntime(new WorkspaceReference(workspace.Value), provider, new LubanPermit());
        var luban = await runtime.ReadAsync(new("subject", "effect", "attempt"), new ReadRequest("a.txt"));
        Check(luban.Succeeded && luban.Value!.Content == "changed", "Unchanged published Luban DLL did not consume forwarded API successfully.");
        var denied = await new FileEffectRuntime(new WorkspaceReference(workspace.Value), provider, new LubanDeny()).ReadAsync(new("subject", "effect", "attempt"), new ReadRequest("a.txt"));
        Check(denied.Status == EffectStatus.AuthorityDenied, "Unchanged Luban denial changed.");
        Console.WriteLine("SUPPORTED BY THIS PROBE: unchanged Local/Luban DLL read, metadata/list, conditional write, start/completion and denial behavior.");
    }
    finally
    {
        var resolved = Path.GetFullPath(root);
        Check(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Temp cleanup boundary invalid.");
        Directory.Delete(resolved, recursive: true);
    }
}
else Console.WriteLine("OUT OF SCOPE ON THIS OS: unchanged Local/Luban native behavior; Windows qualification required.");
Console.WriteLine($"SUPPORTED BY THIS PROBE: v1 identity, path, all 18 legacy type names, JSON shape and binary calls; forwarding={expectForwarded}.");

static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
sealed class Permit : IResourceAuthorizer
{
    public ValueTask<ResourceAuthorizationDecision> AuthorizeAsync(ResourceAuthorizationRequest request, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ResourceAuthorizationDecision(AuthorizationStatus.Permit));
}
sealed class Deny : IResourceAuthorizer
{
    public ValueTask<ResourceAuthorizationDecision> AuthorizeAsync(ResourceAuthorizationRequest request, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ResourceAuthorizationDecision(AuthorizationStatus.Deny));
}
sealed class Journal : IResourceMutationJournal
{
    public int Starts { get; private set; }
    public int Completions { get; private set; }
    public ValueTask<MutationStartDecision> StartAsync(MutationStartRequest request, CancellationToken cancellationToken = default) { Starts++; return ValueTask.FromResult(new MutationStartDecision(MutationStartStatus.Started, "evidence")); }
    public ValueTask<bool> CompleteAsync(MutationCompletion completion, CancellationToken cancellationToken = default) { Completions++; return ValueTask.FromResult(true); }
}
sealed class LubanPermit : IEffectAuthorizer
{
    public ValueTask<AuthorizationDecision> AuthorizeAsync(EffectAuthorizationRequest request, CancellationToken cancellationToken) => ValueTask.FromResult(AuthorizationDecision.Permit);
}
sealed class LubanDeny : IEffectAuthorizer
{
    public ValueTask<AuthorizationDecision> AuthorizeAsync(EffectAuthorizationRequest request, CancellationToken cancellationToken) => ValueTask.FromResult(AuthorizationDecision.Deny);
}
