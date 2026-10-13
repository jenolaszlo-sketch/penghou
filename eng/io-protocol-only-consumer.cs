using System.Text.Json;
using Penghou.IO.Abstractions;

var Invocation = new HostInvocation("i", "s", "e", "a", "scope", null, default);
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
            if (S("identity") != identity.Value) throw new InvalidOperationException("Fixed v1 vector changed: " + S("name"));
            if (v.TryGetProperty("canonicalPath", out var expectedPath))
                if (expectedPath.GetString() != WindowsWorkspacePath.ToIdentityPath(path)) throw new InvalidOperationException("Canonical path changed: " + S("name"));
        }
var dependencies = typeof(ResourceRequestIdentity).Assembly.GetReferencedAssemblies().Where(a => a.Name!.StartsWith("Penghou.", StringComparison.Ordinal)).Select(a => a.Name).ToArray();
if (dependencies.Length != 1 || dependencies[0] != "Penghou.IO.Identity.Abstractions") throw new InvalidOperationException("Stable protocol graph includes preview dependency.");
if (typeof(WorkspaceId).Assembly.GetExportedTypes().Length != 18) throw new InvalidOperationException("Stable identity surface expanded.");
Console.WriteLine("SUPPORTED BY THIS PROBE: 15 fixed vectors and exact data-only stable package graph.");
