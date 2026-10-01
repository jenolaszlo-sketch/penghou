# Shared I/O implementation plan

Status: Ready to begin the first provider slice, 2026-10-01. Only
Penghou.IO.Abstractions interfaces and documentation exist. No local/web
provider, authorization implementation, codec or Hufu adapter exists yet.

The first real consumer is Luban's implemented read runtime. Its delivery plan
defines effect, language, resolution and barrier work; those stay outside this repository's
resource-provider contracts. [Preview/commit requirements](preview-commit-contract.md)
and the [provider contract](provider-contract.md) remain acceptance requirements.

## First delivery: Windows read-only provider

Create a real `Penghou.IO.Local` project alongside `Penghou.IO.Abstractions`,
targeting .NET 8 and 10. Implement IWorkspaceReader: bounded bytes, file
metadata/existence and direct directory pages. A concrete provider is justified
by the Luban consumer; keep it out of the abstractions project. No public
mutation or web implementation is implied by this read slice.

Before handlers, freeze canonical resource-request identity and a pinned Windows
provider profile. The neutral pure codec is a shared contract utility, not an
I/O backend. Use one implementation in trusted caller/provider, not separately
invented encodings. Specify schema/digest version and domain separation, operation
kind, normalized target, complete bounds/preconditions and content snapshots.
Exclude the digest itself to avoid recursive hashing; authenticate invocation,
subject and scope separately in the authorizer. Test stable equivalence and
conflicting-target/payload/version vectors.

Host mappings preserve parent semantic effect identity, concrete request identity
and inherited ceilings. Snapshot mutable buffers/collections before deriving
identity. Source cannot choose workspace roots or authorization bindings. An
explicit injected IResourceAuthorizer is required and only Permit proceeds;
default/unknown/unavailable results and errors fail closed. Test-only explicit
policies are fixtures, never a shipping allow-all provider.

Implement validated workspace paths and bounded read/list operations. Pin case/
encoding/link policy, versions, byte/candidate/output/time bounds and cancellation.
Authorize probes and generic WorkspaceEntry candidates before metadata/type
inspection or disclosure. Omit denied candidates without leaking names/counts;
unavailable authorization aborts. Bind continuation to query/workspace/subject,
repeat live checks and define progress/completeness without stable-snapshot claims.
Keep directory stat out of the first profile: the current API exposes file metadata
and directory listing, not a directory-metadata request.

The initial reader may report a narrower path-based profile with observed
reparse-point rejection. State replacement-race, hard-link and mount limitations;
never advertise object-bound mutation consistency or confinement from it.

Gate: real temporary-workspace tests for read bytes/versions and metadata,
root/child exclusions, denial before probes, no-match/denied scan budgets,
output/paging limits, cancellation, malformed/forged invocation bindings,
identity mismatch and path/link qualification. Run Release tests on .NET 8
and 10. Publish no packages merely to make scaffolding appear complete.

## Luban migration

After this gate, migrate Luban's Read/Find/SearchText through the reader. Keep
semantic IEffectAuthorizer and whole-plan admission above concrete I/O. A child
request digest must not be compared with its parent's semantic digest; the host
authorizer validates its relationship to the admitted parent and exact rights.
Share resource/path enforcement instead of retaining duplicated filesystem code.
Luban owns traversal/search semantics and stream limits above the bounded pages.

Use a documented sibling-checkout integration build pinned to a shared repository
revision initially. Do not depend on unpublished package IDs or vendor duplicate
contracts. Package-release enablement follows qualification and separate release
authorization. Preserve existing runtime outputs, limits and legacy pattern
semantics; richer language behavior requires explicit versions and tests.

Gate: Luban's existing behavior suite passes against the real provider, and
parent/child scope, identity, output/cancellation and unavailable-service cases
are verified. A parser, Hufu or CedarSharp is not a prerequisite for this slice.

## Mutation provider after resolution

Implement IWorkspaceWriter incrementally after Luban can capture/admit immutable
plans and enforce the barrier. Start with one existing-file byte patch. Freeze
provider ResourceVersion semantics and the mapping from observed content hashes;
tokens are not assumed to equal a source --hash. TextPatch byte ranges apply
against the original UTF-8 snapshot. Unified-hunk translation belongs to Luban's
trusted bounded adapter, not to a generic resource API.

Prove a supported object-bound version/check/commit protocol against concurrent
writers, or return Unsupported for the required guarantee. The read provider's
path profile does not establish it. Require exact plan/segment/barrier bindings
in host mappings, final live authority and authorized precondition probes.
Per-resource authorization does not replace batch admission. Journal/start
ordering must be supplied and qualified before claiming recoverable dispatch.

Gate: denied/stale input gives zero mutation; changing target/payload/version
cannot reuse permission; post-start cancellation/failure yields attributable
success or ambiguous outcome, never implied rollback. Later Copy/Move/Delete/
CreateDirectory and bounded batches earn their own role/precondition profiles.

## Later integration and web

Hufu adapters follow its real authority/store and CedarSharp gates; standalone
explicit host policy remains independent. Zhinu integration owns durable plans,
fences, start ordering and recovery. Providers do not create a second authority
or language-plan store. Supervisor guarantees are qualified separately.

Web remains later work: bounded credential-free GET, requested/redirected URL
and resolved-endpoint checks, pinned connection resolution, SSRF/profile limits,
wire/decompressed bytes and finite deadlines. Do not execute opaque external
operations in WhatIf or treat a read-looking name as qualified discovery.

Use Luna for bounded provider/test work where possible, with review of contract
identity and native consistency changes. APIs and runtime guarantees advance
only with their corresponding behavior tests and an actual consumer.
