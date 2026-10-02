# Shared I/O implementation plan

Active corrective delivery is now specified in
[resource-abstractions-corrective-plan.md](resource-abstractions-corrective-plan.md).
Use the [Sol handoff](resource-abstractions-sol-handoff.md) for the next exact
work item and working-tree precautions. The finish line now includes corrected
code ownership, CI/NuGet publication, and actual Luban/Hufu adoption of published
versions; design and local-feed checks are intermediate. The earlier delivery narrative below
remains historical/qualification context; it is not a competing implementation queue.

## Current corrective priority — RA-1 through RA-5

Implementation update: [ADR 0003](decisions/0003-replaceable-resource-providers.md)
selects provider composition and a Local conditional byte writer. The semantic
patch DTOs/materializer live in Luban; shared codec/path algorithms live in
IO.Protocols. Earlier Local patcher descriptions below are historical regression
evidence. Follow the [delivery ledger](resource-abstractions-corrective-plan.md)
for current qualification and the remaining publication/adoption gates.

Follow the [architecture baseline and review](resource-abstractions-architecture.md)
before extending the implementation below. [RA-0 inventory](resource-abstractions-inventory.md)
is complete as documentation; no corrective API or provider work is complete.
Resolve the enforcement, identity/path, conditional-write and migration gates,
then relocate implementation, inject Luban capabilities, qualify Hufu interception
and prove package-only consumption. Existing qualified behavior remains the
regression baseline, including candidate checks and mutation uncertainty.

The codec-in-Abstractions and provider-internal authorizer descriptions below
explain the current slice, not the intended final package boundary. Keep one
versioned codec implementation outside the contract assembly after RA-1 selects
its owner. Test-only conformance spies are permitted; no production VFS is in scope.
VFS-1 through VFS-10 remain deferred under the canonical architecture.

Status: Read slices and controlled single-patch profile implemented, 2026-10-01. The canonical request codec, Windows read-only Local provider, Luban read migration, and the explicitly host-controlled Local patcher with Luban's separate single-target executor are implemented. See the [read profile](local-reader-profile.md), [patch profile](local-patch-profile.md), and [identity encoding](canonical-request-identity.md). Native evidence is recorded below. The full writer interface, web, general batch admission/barrier, production Hufu adapter, and durable Hufu/Zhinu integration remain pending.

Native qualification passes 86/86 tests on each .NET target, and Luban's
separate executor/read/capture suite passes 167/167. Native tests demonstrated
that a hard link can be created while the leaf handle is exclusive. A subsequent
check refused the observed attack before mutation, but cannot close every alias
race. Default and unknown namespaces return Unsupported before I/O; trusted
composition must explicitly select HostControlled and ensure untrusted actors
cannot alter aliases, root/mount/reparse/case configuration. Case-sensitive
directory enablement remains unverified on this host. General confinement and
production durable adapters remain pending.

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

## Narrow Local patch profile — implemented

`Penghou.IO.Local.LocalWorkspacePatcher` implements only one existing-file original-version UTF-8 byte patch on a fixed-drive NTFS workspace. It rejects UNC/device roots, non-NTFS/fixed volumes, reparse components, case-sensitive directories, and files with more than one hard link. Directory handles are held with sharing that denies ordinary new write/delete opens; the target is opened existing, no-share, and mutated through that same handle. This provides a narrow object-bound version/check/write profile against ordinary concurrent file opens, not a defense against privileged processes or raw-volume writes. The captured `local-read-v1:sha256` token binds content only: preview does not prove that the captured pathname still denotes the same native file object at execution.

The patcher bounds the original at 16 MiB, edits at 128, replacement bytes at 1 MiB, and output at 16 MiB; input and output are strict UTF-8, edits are ordered, nonoverlapping original-version byte ranges with scalar-boundary offsets. It writes, flushes, and verifies through the same handle. In-place mutation can tear on crash and is not an atomic replacement. Any post-start uncertainty is `AmbiguousOutcome`, requiring reconciliation with no blind retry.

Luban's `SinglePatchExecutor` accepts exactly one complete literal-target patch plan. It recomputes the plan and observation identities and checks that the writer's recomputed proposed hash/length match capture. A required trusted `IPatchExecutionHost` performs whole-plan admission before provider access, live exact-resource checks, and a start call that must serialize current admission/revision/fence/revocation and durably record start evidence before returning `Started`. The provider records `NoMutation`, `Completed`, or `Ambiguous`; acknowledged completion is required for success. This is a standalone host contract, not a production Hufu/Zhinu adapter or durable journal.

The execution profile reserves operation time and read budgets before mutation; aggregate original plus post-verification bytes must fit the document read cap. The reader's captured version remains a content precondition, not historical object identity. `PreviewRuntime.WhatIfAsync` is unchanged, read-only, has no writer callback, and `ResolvedEffectPlan.CanCommit` remains false. See the [Luban executor profile](../../Penghou.Luban/docs/single-patch-execution-profile.md).

Remaining gates: stronger namespace/alias guarantees and case-sensitive-directory qualification; production host/Hufu admission and Zhinu journaling/fences/recovery; multi-node/multi-target batch admission; selected-glob execution; textual-hunk conversion; other mutation methods and web. No multi-file transaction or crash-atomicity claim is made.

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
