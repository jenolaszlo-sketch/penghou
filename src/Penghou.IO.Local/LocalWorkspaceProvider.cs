using Penghou.IO.Abstractions;

namespace Penghou.IO.Local;

/// <summary>Host composition of the qualified Windows provider; physical roots stay here.</summary>
public sealed class LocalWorkspaceProvider : IWorkspaceProvider
{
    private readonly string _root;
    private readonly LocalPatchNamespace _namespace;

    public LocalWorkspaceProvider(WorkspaceId workspace, string root,
        LocalPatchNamespace namespaceProfile = LocalPatchNamespace.Unspecified)
    {
        if (string.IsNullOrWhiteSpace(workspace.Value)) throw new ArgumentException("A workspace is required.", nameof(workspace));
        ArgumentNullException.ThrowIfNull(root);
        Workspace = workspace;
        _root = Path.GetFullPath(root);
        _namespace = namespaceProfile;
    }

    public WorkspaceId Workspace { get; }
    public WorkspaceProviderCapabilities Capabilities => new("local-windows-read-v1",
        LocalWorkspaceWriter.ProviderProfile, OperatingSystem.IsWindows(),
        OperatingSystem.IsWindows() && _namespace == LocalPatchNamespace.HostControlled);

    public IWorkspaceReaderSession OpenReader(IResourceAuthorizer authorizer, WorkspaceReaderOptions? options = null)
    {
        options ??= new();
        return new LocalWorkspaceReader(Workspace, _root, authorizer, new LocalReaderOptions
        {
            MaxEntries = options.MaxEntries,
            MaxCandidatesScanned = options.MaxCandidatesScanned,
            MaxTotalCandidatesScanned = options.MaxTotalCandidatesScanned
        });
    }

    public IWorkspaceConditionalWriter OpenWriter(IResourceAuthorizer authorizer,
        IResourceMutationJournal journal, WorkspaceWriterOptions? options = null)
    {
        options ??= new();
        return new LocalWorkspaceWriter(Workspace, _root, authorizer, journal,
            new LocalPatchOptions(options.MaxOriginalBytes, options.MaxReadBytes,
                options.TimeoutMilliseconds, _namespace));
    }
}
