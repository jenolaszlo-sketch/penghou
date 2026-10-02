using System.Security.Cryptography;
using Penghou.IO.Abstractions;
using Penghou.IO.Local;
using Xunit;

namespace Penghou.IO.Tests;

public sealed class ConditionalWriterTests
{
    [Fact]
    public async Task Conditional_writer_persists_binary_bytes_and_records_exact_versions()
    {
        using var workspace = new Workspace();
        var original = new byte[] { 255, 0, 128 };
        var proposed = new byte[] { 254, 0, 129, 1 };
        File.WriteAllBytes(workspace.Path, original);
        var policy = new Policy();
        var journal = new Journal();
        var result = await workspace.Writer(policy, journal).WriteFileAsync(workspace.Request(original, proposed));
        Assert.True(result.Succeeded, result.Failure?.ToString());
        Assert.Equal(proposed, File.ReadAllBytes(workspace.Path));
        var start = Assert.Single(journal.Starts);
        Assert.Equal(Version(original), start.OriginalVersion);
        Assert.Equal(Version(proposed), start.ProposedVersion);
        Assert.Equal(LocalWorkspaceWriter.ProviderProfile, start.ProviderProfile);
        Assert.Equal(MutationOutcome.Completed, Assert.Single(journal.Completions).Outcome);
        Assert.Equal(2, policy.Requests.Count(r => r.Action == ResourceAction.WriteFile));
    }

    [Fact]
    public async Task Stale_precondition_does_not_start_or_write()
    {
        using var workspace = new Workspace();
        File.WriteAllBytes(workspace.Path, [1]);
        var journal = new Journal();
        var result = await workspace.Writer(new Policy(), journal).WriteFileAsync(workspace.Request([2], [3]));
        Assert.Equal(ResourceFailureKind.PreconditionFailed, result.Failure);
        Assert.Empty(journal.Starts);
        Assert.Equal(new byte[] { 1 }, File.ReadAllBytes(workspace.Path));
    }

    [Fact]
    public async Task Initial_denial_does_not_probe_a_missing_root_or_start()
    {
        using var workspace = new Workspace();
        var policy = new Policy(_ => new(AuthorizationStatus.Deny));
        var journal = new Journal();
        var writer = new LocalWorkspaceWriter(workspace.Id, workspace.Root + "-missing", policy, journal,
            new(Namespace: LocalPatchNamespace.HostControlled));
        var result = await writer.WriteFileAsync(workspace.Request([1], [2]));
        Assert.Equal(ResourceFailureKind.AuthorizationDenied, result.Failure);
        Assert.Single(policy.Requests);
        Assert.Empty(journal.Starts);
    }

    [Fact]
    public async Task Mutable_payload_is_frozen_before_authorization_and_use()
    {
        using var workspace = new Workspace();
        File.WriteAllBytes(workspace.Path, [1]);
        var payload = new byte[] { 2, 3 };
        var request = workspace.Request([1], payload);
        var policy = new Policy(_ => { payload[0] = 9; return new(AuthorizationStatus.Permit); });
        var journal = new Journal();
        var result = await workspace.Writer(policy, journal).WriteFileAsync(request);
        Assert.True(result.Succeeded);
        Assert.Equal(new byte[] { 2, 3 }, File.ReadAllBytes(workspace.Path));
        Assert.All(policy.Requests, r => Assert.Equal(request.Invocation.RequestIdentity, r.SnapshotRequestIdentity));
    }

    [Fact]
    public async Task Revoked_final_write_check_prevents_start_and_mutation()
    {
        using var workspace = new Workspace();
        File.WriteAllBytes(workspace.Path, [1]);
        var writes = 0;
        var policy = new Policy(r => new(r.Action == ResourceAction.WriteFile && ++writes == 2
            ? AuthorizationStatus.Deny : AuthorizationStatus.Permit));
        var journal = new Journal();
        var result = await workspace.Writer(policy, journal).WriteFileAsync(workspace.Request([1], [2]));
        Assert.Equal(ResourceFailureKind.AuthorizationDenied, result.Failure);
        Assert.Empty(journal.Starts);
        Assert.Equal(new byte[] { 1 }, File.ReadAllBytes(workspace.Path));
    }

    [Fact]
    public async Task Failed_completion_preserves_ambiguous_outcome_after_write()
    {
        using var workspace = new Workspace();
        File.WriteAllBytes(workspace.Path, [1]);
        var journal = new Journal { RecordCompletion = false };
        var result = await workspace.Writer(new Policy(), journal).WriteFileAsync(workspace.Request([1], [2]));
        Assert.Equal(ResourceFailureKind.AmbiguousOutcome, result.Failure);
        Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(workspace.Path));
        Assert.Single(journal.Starts);
    }

    [Fact]
    public void Contract_assembly_has_no_implementation_or_semantic_dependencies()
    {
        var assembly = typeof(IWorkspaceProvider).Assembly;
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Penghou.", StringComparison.Ordinal));
        Assert.DoesNotContain(assembly.GetExportedTypes(), t => t.Name is "WindowsWorkspacePath" or "ResourceRequestIdentity" or "TextPatch" or "FilePatchRequest");
        Assert.NotSame(assembly, typeof(ResourceRequestIdentity).Assembly);
        Assert.DoesNotContain(typeof(IWorkspaceProvider).GetProperties(), p => p.Name.Contains("Root", StringComparison.Ordinal));
    }

    private static ResourceVersion Version(byte[] bytes) => new("local-read-v1:sha256:" + Convert.ToHexString(SHA256.HashData(bytes)));

    private sealed class Workspace : IDisposable
    {
        public WorkspaceId Id { get; } = new("conditional-writer-tests");
        public string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "penghou-writer-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(Root, "binary.dat");
        public Workspace() => Directory.CreateDirectory(Root);
        public IWorkspaceConditionalWriter Writer(IResourceAuthorizer authorizer, IResourceMutationJournal journal)
            => new LocalWorkspaceProvider(Id, Root, LocalPatchNamespace.HostControlled).OpenWriter(authorizer, journal);
        public FileWriteRequest Request(byte[] original, byte[] proposed)
        {
            var invocation = new HostInvocation("operation", "subject", "effect", "attempt", "scope", null, default);
            var request = new FileWriteRequest(invocation, Id, new("binary.dat"), proposed, new(1024),
                new(WritePreconditionKind.MustMatchVersion, Version(original)));
            return request with { Invocation = invocation with { RequestIdentity = ResourceRequestIdentity.Compute(request) } };
        }
        public void Dispose() => Directory.Delete(Root, true);
    }

    private sealed class Policy(Func<ResourceAuthorizationRequest, ResourceAuthorizationDecision>? decide = null) : IResourceAuthorizer
    {
        public List<ResourceAuthorizationRequest> Requests { get; } = [];
        public ValueTask<ResourceAuthorizationDecision> AuthorizeAsync(ResourceAuthorizationRequest request, CancellationToken cancellationToken = default)
        { Requests.Add(request); return ValueTask.FromResult(decide?.Invoke(request) ?? new(AuthorizationStatus.Permit)); }
    }

    private sealed class Journal : IResourceMutationJournal
    {
        public bool RecordCompletion { get; init; } = true;
        public List<MutationStartRequest> Starts { get; } = [];
        public List<MutationCompletion> Completions { get; } = [];
        public ValueTask<MutationStartDecision> StartAsync(MutationStartRequest request, CancellationToken cancellationToken = default)
        { Starts.Add(request); return ValueTask.FromResult(new MutationStartDecision(MutationStartStatus.Started, "evidence")); }
        public ValueTask<bool> CompleteAsync(MutationCompletion completion, CancellationToken cancellationToken = default)
        { Completions.Add(completion); return ValueTask.FromResult(RecordCompletion); }
    }
}
