# Resource abstraction public-type inventory

Corrective implementation update, 2026-10-02: the contract assembly now has
53 public types including nested binding cases. The original 52-type review
below is retained as the migration baseline. The implemented changes are:

- `WindowsWorkspacePath` and `ResourceRequestIdentity` moved into IO.Protocols;
  their existing public namespace is retained for this unpublished preview.
- `TextPatch` and `PatchLimits` belong to `Penghou.Luban.Changes`; the unused
  `FilePatchRequest` was removed during the final API review. Their
  pure materialization is a language operation; Local accepts conditional bytes.
- Added `WorkspaceProviderCapabilities`, `WorkspaceReaderOptions`,
  `WorkspaceWriterOptions`, `IWorkspaceProvider`, `IWorkspaceReaderSession`
  and `IWorkspaceConditionalWriter` in ProviderContracts.cs.
- `IWorkspaceWriter` no longer exposes text patching. Broader resource/HTTP DTOs
  remain compatibility contracts with no new implementation.
- Logical Luban workspace references no longer carry physical roots. The host
  injects a provider. Hufu.IO supplies the optional current-authority adapter.

See [ADR 0003](decisions/0003-replaceable-resource-providers.md) for selected
contracts and the [corrective plan](resource-abstractions-corrective-plan.md) for
qualification and publication status.

## Original migration baseline

Reviewed 2026-10-02. Read-only inventory for RA-0 in the
[architecture baseline](resource-abstractions-architecture.md).
Source: four C# files in src/Penghou.IO.Abstractions. Every public top-level
type and each public nested ResourceBinding case is named below. Suggested moves
are pending RA-1 decisions; this document changes no API.

| Source | Public types | Classification and disposition |
| --- | --- | --- |
| Contracts.cs | WorkspaceId, WorkspacePath, ResourceVersion, DirectoryContinuation, RequestIdentity | Contract. Keep neutral identity/value concepts; resolve path/view/incarnation and version semantics under G3. |
| Contracts.cs | HostInvocation | Contract with execution-model coupling. Review required effect/attempt/scope fields under G2; not a Hufu-specific implementation. |
| Contracts.cs | ResourceAction | Contract enum. Filesystem actions stay; HTTP action follows future resource-domain split. Patch action depends on G4. |
| Contracts.cs | ResourceBinding; ResourceBinding.WorkspaceFile, ResourceBinding.WorkspaceDirectory, ResourceBinding.WorkspaceEntry | Contract discriminated resource values. Keep logical resource identities; do not add physical roots. |
| Contracts.cs | ResourceBinding.Web, ResourceBinding.WebEndpoint; WebUrl | HTTP/resource-endpoint contracts. Future HTTP owner; define compatibility without introducing new HTTP behavior now. |
| Contracts.cs | ResourceAuthorizationRequest, AuthorizationStatus, ResourceAuthorizationDecision, IResourceAuthorizer | Neutral enforcement contracts, no Hufu policy implementation. Resolve G1/G2 placement and cooperation; do not delete final checks just to achieve a decorator shape. |
| Contracts.cs | IoLimits, ResourceFailureKind, ResourceResult<T> | Contract. Keep bounds/outcome semantics; trivial result factories and Succeeded do not constitute an algorithm. |
| Contracts.cs | FileReadResult, FileMetadata, DirectoryEntry, DirectoryTruncationReason, DirectoryPage | Contract. Clarify result ownership, authorized-view completeness and continuation semantics. |
| Contracts.cs | WritePreconditionKind, WritePrecondition, FileWriteRequest, FileDeleteRequest, DirectoryCreateRequest, FileMoveRequest | Resource mutation contracts. Preserve conditional semantics and both-endpoint roles; capability support is separate from interface presence. |
| Contracts.cs | TextPatch, PatchLimits, FilePatchRequest | Contract carrying text-specific patch semantics. Resolve G4: Luban materialization plus conditional resource write is the recommended direction; no mechanical removal before replacement qualification. |
| Contracts.cs | FileReadRequest, FileMetadataRequest, DirectoryListRequest | Resource request contracts. Keep finite operations; review common execution envelope under G2. |
| Contracts.cs | IWorkspaceReader, IWorkspaceWriter, IWorkspaceFileSystem | Capability contracts. Reader currently combines content, metadata and listing; writer combines five mutation operations. Narrow where a real consumer benefits, under G5. |
| Contracts.cs | WebReadRequest, WebReadResult, IWebResourceReader | HTTP contracts already mixed into this assembly. Schedule migration to a separate domain contract, with compatibility, after the immediate correction. |
| MutationContracts.cs | MutationStartRequest, MutationStartStatus, MutationStartDecision, MutationOutcome, MutationCompletion, IResourceMutationJournal | Neutral-shaped enforcement/start/outcome contracts with locked-object/profile assumptions. Review G1/G3: physical/native identity must be opaque provider evidence, not required by virtual callers. Preserve durable start and ambiguous-outcome guarantees. |
| WindowsWorkspacePath.cs | WindowsWorkspacePath | Local/profile implementation, including reserved names and ASCII identity folding. Move outside Abstractions; separate neutral logical-path invariants from Windows restrictions. |
| ResourceRequestIdentity.cs | ResourceRequestIdentity | Other implementation: versioned canonical encoding/hash algorithm. Uses WindowsWorkspacePath for all workspace identities and also implements HTTP URL encoding. Move into deliberately owned support/profile code with shared vectors; choose destination under G2/G8. |

No diff or merge algorithm and no Hufu/Cedar/Biscuit implementation were found in
this assembly. Diff/merge/transport behavior already lives in
[Penghou.Luban/Changes](../../Penghou.Luban/src/Penghou.Luban/Changes).

## Confirmed correction points outside the assembly

- [FileEffectRuntime.cs](../../Penghou.Luban/src/Penghou.Luban/Runtime/FileEffectRuntime.cs):
  WorkspaceReference carries RootPath/FullRootPath (line 13); reader construction
  returns LocalWorkspaceReader (line 217).
- [LanguageRuntime.cs](../../Penghou.Luban/src/Penghou.Luban/Language/LanguageRuntime.cs):
  Local reader construction/property at lines 456/465.
- [PreviewRuntime.cs](../../Penghou.Luban/src/Penghou.Luban/Resolution/PreviewRuntime.cs):
  Local reader construction/property at lines 360/367.
- [SinglePatchExecutor.cs](../../Penghou.Luban/src/Penghou.Luban/Execution/SinglePatchExecutor.cs):
  public LocalPatchNamespace and Local patcher construction at lines 11/46/91.
  Include batch consumers in the migration closure rather than fixing only readers.
- [LocalWorkspaceReader.cs](../src/Penghou.IO.Local/LocalWorkspaceReader.cs):
  candidate authorization inside listing (line 165) and path-component probes
  (lines 249/261) explain why an outer request-only decorator is insufficient.
- [LocalWorkspacePatcher.cs](../src/Penghou.IO.Local/LocalWorkspacePatcher.cs):
  version check/materialization/live authorization/start at lines 189/194/200/208;
  patch application implementation at line 584. Preserve the qualified protocol
  until conditional persistence replaces it.
- Both IO projects currently set IsPackable=false. NuGet delivery remains pending.
- Hufu.Luban implements language admission/resource mapping. No Hufu.IO project
  exists in the reviewed source; do not mark the resource decorator complete.

Line numbers describe the reviewed checkout and may shift. Follow the linked
symbols when implementing. Existing uncommitted work was included in the review
and must be preserved.
