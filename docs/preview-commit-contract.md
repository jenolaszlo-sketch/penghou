# Resource providers under preview resolution and a commit barrier

Direction update, 2026-10-02: the [resource-abstractions baseline](resource-abstractions-architecture.md)
governs the pending RA correction and deferred VFS work. This document remains
the current profile/design reference until a versioned replacement is qualified.
In particular, preserve concrete checks and identity vectors during relocation;
capture-only WhatIf and future overlay execution are distinct modes.

Status: Provider integration requirements, 2026-10-01. The project includes a qualified read-only Local provider and canonical request codec, plus a narrow Local existing-file patcher described in [local-patch-profile.md](local-patch-profile.md). Luban's separate programmatic capture-only WhatIf profile uses the reader and retains immutable edit payloads/provider observations; its plans cannot commit or dispatch the standalone executor. This neutral repository owns no preview-plan API. General writer methods, multi-target barrier, Hufu adapter and durable workflow integration remain pending.

The host/Luban execution design uses four separate safeguards:

1. Validate the entire document and statically preflight known effect requirements
   and targets. A known denied downstream write blocks upstream target I/O.
2. Resolve dynamic effects using only authorized bounded read-only observations.
   Capture proposed mutations instead of dispatching them.
3. Freeze a typed resolved plan and admit its whole known mutation set before
   an executor-enforced commit barrier. Any known denied mutation blocks the set.
4. Recheck current authority, resource identity and preconditions at actual I/O.

Static preflight may read trusted authority state; read-only resolution performs
real target I/O. Resolution therefore uses `IWorkspaceReader` and the required
resource authorizer, with the same limits, candidate exclusions, cancellation,
evidence and release controls as ordinary reads. It is not a pure policy
simulation and does not grant write authority. Only qualified discovery handlers
may participate; names such as Git.Status or web GET do not prove safe preview
behavior. No web or Git discovery provider is included here.

Explicit WhatIf execution never dispatches requested mutations or opaque/lazy
effects, including approved native tools and mutating remote APIs. It reports
unresolved nodes without running them. Trusted authority/evidence bookkeeping
may still write under host policy; WhatIf is not a claim of zero host I/O.

## Host bindings and the final provider gate

The resolved capture plan and future commit admission belong to Luban/the host, not this neutral interface
library. It binds semantic IR identity, exact nodes/dependencies, concrete target
roles, stable payload digests, observation versions, preconditions, limits,
catalogue/provider profiles, coverage and unresolved reasons. Source fingerprints
are diagnostic provenance, not approvals. Keep plan identity distinct from the
semantic effect digest and each concrete backend request identity.

A future governed provider must resolve trusted invocation/effect mappings and
verify that a proposed mutation belongs to the admitted plan/segment and that
its commit barrier has been released for the current subject/revision/fence.
An agent-controlled `preview=false` or `commit=true` argument is never sufficient.
Current data records do not authenticate bindings or implement this gate.
Standalone hosts supply their own explicit equivalent policy; Hufu integration
remains optional and separate.

Snapshot mutable inputs and verify exact concrete request identity before
authorization. Recheck every resource role immediately before protected access.
Copy/move need both endpoints; reads and metadata/discovery also need permission.
Mutation preconditions must bind the actual object under a qualified consistency
protocol. A plan, path string or earlier allow cannot close replacement races.

## Coverage and changed state

Directory pages describe an authorized-only view. `IsComplete` does not prove
the absence of excluded resources. A mutation plan must explicitly declare
authorized-view versus all-match selection semantics. Hidden omissions cannot
prove all-match coverage, and limits/errors cannot become a complete prefix plan.
Required incomplete coverage blocks admission; no silent lazy downgrade.

Freeze the exact resolved target manifest. Commit cannot rerun globs and add
files. Changed observation assumptions, targets, payloads or versions require
re-resolution and new admission, or a typed failure. Cached observations and
plan rendering need current release controls; retain content only as explicitly
authorized protected snapshots/artifacts, not ordinary telemetry.

Read-after-proposed-write dependencies and effects following opaque state-changing
tools may need separately resolved/admitted segments. Do not resolve future
state by reading the current filesystem or invent simulated native behavior.
Unsupported operations, unavailable authorization, exhausted resolution budgets
and denied access do not qualify as permitted lazy execution.

## Commit and recovery limits

The barrier prevents known denied mutations from starting. It is not a
multi-resource transaction: changes, revocation or failures after earlier commits
can leave partial outcomes. Record exact receipts, stop subsequent dispatch and
reconcile uncertain operations rather than imply rollback or blindly retry.
Failed preconditions cause zero mutation for the individual effect, under the
provider's documented protocol.

Durable hosts retain plan/segment/admission identities and started/completed
effects in their existing journal/evidence stores. Restart revalidates authority,
fences and observations before new dispatch; preview completion is not a reusable
permit. Compensation/cleanup requires separate authorization. This library does
not introduce a competing plan, authority or execution store.

Qualification must demonstrate known downstream denial before discovery, denied
dynamic mutation sets with zero writes, WhatIf with no tool/mutation dispatch,
incomplete coverage blocking, target/payload drift, revocation after admission,
stale versions, partial post-start failures, restart and ambiguous outcomes.
