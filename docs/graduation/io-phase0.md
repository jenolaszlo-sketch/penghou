# IO.Protocols graduation baseline

Recorded 2026-10-14 before changing the public dependency boundary.

| Item | Baseline |
| --- | --- |
| Repository | `https://github.com/jenolaszlo-sketch/penghou` |
| Branch/source | `codex/graduation-io-protocols`, starting HEAD `5d9854a09c8c243fd9e7a09bd6e8109db2937ca5` |
| Published family | Abstractions, Protocols, Local `0.1.0-preview.1`; publication/equal-content proof in resource-package-release-handoff.md |
| Frameworks | net8.0 and net10.0 |
| Baselines | Neither IO assembly has shipped/unshipped analyzer baselines or binary compatibility policy |
| Protocol public API | `ResourceRequestIdentity` (8 Compute overloads, 3 constants); `WindowsWorkspacePath` (Normalize, ToIdentityPath, MaximumLength) |
| Dependencies | Protocols → Abstractions; Local → Protocols + Abstractions; Abstractions has no package dependencies |
| Consumers | Local read/list/metadata/write; Luban language, preview, diff/merge, single/batch conditional persistence; optional Hufu adapter only as context |
| Formats | Canonical request identity schema v1, SHA-256 prefix, Windows logical path profile; no owned persisted store |
| Existing vectors | Independent fixed read and root-list digests; field sensitivity, opcode separation, path restrictions, URL canonicalization tests |
| Existing operational probes | Local bounded read/list, continuation binding, conditional write/version/start/outcome tests; no protocol restart requirement |
| CI | Windows full IO tests net8/net10; Linux neutral builds only; local-feed package smoke on Windows |
| Release | Trusted NuGet environment/OIDC, manual exact-commit/tag checks, coordinated three-IO-package preview release; Transport/Workflow separate profiles |

## Exact public dependency closure

Protocol signatures require exactly these 18 data types, including transitive
record properties/constructors: WorkspaceId, WorkspacePath, RequestIdentity,
ResourceVersion, DirectoryContinuation, WebUrl, HostInvocation, IoLimits,
WritePreconditionKind, WritePrecondition, FileReadRequest, FileMetadataRequest,
DirectoryListRequest, FileWriteRequest, FileDeleteRequest, DirectoryCreateRequest,
FileMoveRequest, WebReadRequest. Framework types include Uri and ReadOnlyMemory<byte>.
No provider, authorization, mutation-journal, result, or capability interface is
in this closure. The request invocation is excluded from every v1 digest.

Local and Luban exercise the read/metadata/list/conditional-write overloads.
Delete/create/move/web encoding appears only in this repository's protocol tests.
Retaining their v1 data encodings is not a provider support or authority promise.

## Unresolved-item disposition

| Item | Disposition |
| --- | --- |
| Preview capabilities transitively pulled into Protocols | MUST RESOLVE: extract exact DTO closure and forward old assembly types |
| No API/binary/versioning baseline | MUST RESOLVE: shipped analyzer baseline, unchanged-binary and type-resolution probes, explicit 1.x policy |
| OS-independent path/canonical identity evidence | MUST RESOLVE: Linux/Windows protocol tests and fixed independently checkable byte vectors |
| Only read/list fixed vectors | MUST RESOLVE: all eight opcodes, Unicode, continuation/precondition, bounds and canonical paths |
| Request and result mutable buffer ownership | MUST RESOLVE for stable DTOs: explicitly borrowed memory; computation snapshots once; caller freezes during call and provider owns separate snapshot. Provider result ownership stays preview |
| URI runtime canonicalization | MUST RESOLVE: qualify .NET 8/10 fixed URL vectors; framework canonicalization changes must not silently reinterpret supported vectors |
| Invalid operational bounds/precondition combinations | CAN REMAIN UNSPECIFIED as provider acceptance; protocol v1 explicitly encodes all int32 list limits and precondition enum values without permitting operations |
| Future unknown schemas | MUST RESOLVE: never reinterpret v1/opcode 05; no parser exists and arbitrary future identity strings are opaque |
| Physical filesystem Unicode equivalence/links/confinement | OUT OF SCOPE: protocol provides no physical namespace guarantee |
| Writer, web, provider, mutation journal, VFS capabilities | EXPERIMENTAL: remain in preview Abstractions and Local; never promoted through Protocols dependency |
| New formats/performance/providers | FUTURE WORK not affecting 1.0 |

The minimum mechanically credible stable prerequisite is the 18-type data-only
`Penghou.IO.Identity.Abstractions` package. Moving it with type forwarding keeps
existing source imports and old binary type references usable while leaving the
broader Abstractions package preview. See ADR 0005 and the stable contract manual.
