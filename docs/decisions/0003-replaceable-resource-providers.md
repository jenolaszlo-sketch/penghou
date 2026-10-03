# ADR 0003: Contract-only capabilities and explicit provider composition

Status: corrective code and candidate-package consumers qualified, 2026-10-02;
remote CI, NuGet publication and published restores remain delivery gates.
Implements RA-1A/RA-1B under the resource-abstractions architecture.

## Decision and closure

IO.Abstractions contains records, enums and capability interfaces. Physical roots
are configuration of LocalWorkspaceProvider, not WorkspaceReference in Luban.
IWorkspaceProvider opens explicitly owned reader sessions and conditional writers
with required policy-neutral authorizer and mutation-journal hooks. The host chooses
the provider and the authority implementation. No global workspace or default permit.

IO.Protocols owns the single versioned request codec and existing Windows namespace
profile implementation shared by caller and provider. Its public helper namespace
is retained for this first unpublished-preview migration; assembly identity changes
are deliberate. A separate support assembly is necessary because both neutral
consumers and Local need one codec, while Abstractions may not contain algorithms
and neutral consumers must not import Local. All three packages release together.

Logical contracts retain slash-relative paths and empty root. The current language
profiles retain their documented Windows spelling/case restrictions; that profile
code is outside the contract assembly. Physical mapping remains Local-only. Future
path/profile revisions require distinct codec identities, not reinterpretation of v1.
ResourceVersion stays opaque; the current provider's content token is not object
identity. Provider/view identity and capability reports are descriptive, not grants.

## G1: enforcement cooperation

The same supplied resource authorizer is retained inside reader/writer sessions.
An optional Hufu boundary can deny an initial request without opening/delegating
to an inner session, and allowed sessions still call that boundary for discovered
children/path probes. Local calls the required mutation journal at its locked
object boundary before writing. An outer decorator never replaces these checks.
Failed mandatory evidence is unavailable, not permit. Unsupported profiles cannot
manufacture stronger guarantees. Revocation blocks new starts under the existing
start profile; already started work may finish. No drain or confinement claim.

## G2/G3: context, lifetime and ownership

HostInvocation remains a neutral authenticated binding. Effect/attempt/scope IDs
are opaque host operation references, not workflow engine types or Hufu grants.
A plain host can create real operation/scope bindings; it need not create a workflow
or Hufu envelope. It must supply an explicit authentication/policy implementation.
No convenience constructor invents IDs or authority. More minimal envelopes may
be versioned later; this correction preserves current exact parent/child binding.

Requests are snapshotted before identity, checks and execution. Reader sessions
own and dispose continuations; provider factories retain immutable workspace
configuration. Multiple providers/workspaces are explicit instances. Capability
discovery describes support and does not remove operation-time validation.

## G4: semantic patches and conditional resource persistence

TextPatch, PatchLimits and FilePatchRequest belong to Luban semantics. Pure patch
materialization stays there. Catalogue windows-patch-capture-v2 owns bounded
immutable original/proposed bytes with exact hashes and lengths; retained bytes
count against the plan ceiling and require protected-content retention policy.
The executor verifies exact pure materialization, freezes bytes and issues FileWriteRequest with
MustMatchVersion. LocalWorkspaceWriter accepts existing-file conditional writes,
checks the original through its exclusive handle and writes the exact proposed
snapshot after live checks and the durable-start callback. It interprets no edits
and validates no text encoding; byte persistence is encoding-neutral.

The physical profile changes to local-windows-ntfs-controlled-write-v1. The
original and proposed content versions retain the qualified content-token scheme.
Create-only, general deletion/move, atomic replacement and a full writer remain
Unsupported in this narrow profile. In-place writes can tear; post-start uncertainty
is AmbiguousOutcome with reconciliation and no blind retry. Batch admission and
completed-prefix recovery remain Luban/host responsibilities.

## G5/G8 and delivery

Keep narrow reader and conditional-writer interfaces; preserve existing broader
neutral request DTOs for explicitly unsupported future operations. HTTP contracts
are compatibility surface only; no HTTP provider/redesign is added. Architecture
tests inspect assembly/public type closure; real native tests qualify Local, and
test-only providers qualify neutral composition. No production VFS is introduced.

First coordinated package version: 0.1.0-preview.1, subject to registry collision
check before release. Default consumer builds use pinned PackageReferences;
UsePenghouSource=true is the explicit local integration path. CI tests/packs on
Windows .NET 8/10. A validated manual release commit or matching version tag
publishes qualified artifacts through the NuGet OIDC environment. Package-only
smoke is intermediate; completion requires
published package restores and actual Luban/Hufu integration evidence.

## Protocol traces

- Initial read deny: outer decision/required recording, zero inner session calls.
- Allowed list: list decision, authorized path probes, current per-child decision,
  excluded names/metadata omitted; later pages repeat checks and limits.
- Conditional write: frozen target/bytes/precondition identity -> semantic admission
  -> physical role/probe checks -> locked original/version -> final write check ->
  serialized start -> exact byte write/flush/verification -> acknowledged outcome.
- Stale version or preparation-to-start denial: zero writes; no successful receipt.
- Possible-write cancellation/lost response: ambiguous outcome, reconciliation.
- Unsupported move: no downgrade to independent copy/delete or weaker rights.
- Two workspaces: separate IDs/configuration/sessions, never ambient root selection.

These are acceptance traces, not claims that all implementation/tests already pass.
