# Sol handoff: Penghou resource abstractions

Updated 2026-10-02 during corrective implementation. This is a resumable handoff;
publication and published-package adoption remain open until the ledger has
real release/consumer evidence.

## Objective

Bring IO.Abstractions back to a contract-only, provider-independent capability
boundary for AI tool resource access. Make Local optional to neutral Luban code
and Hufu optional to neutral resource providers, while preserving real operation-time
checks, bounds, versions, admission and uncertain-outcome semantics.

The full goal is to correct the abstraction repository, move any Luban-owned code
to Luban, set up CI and NuGet publishing, publish the corrected package versions,
and make Luban and Hufu consume those published packages with passing integration
checks. Local packing, design documents or source-reference migration alone do
not complete the task.

## Start here

Primary repository: C:\Users\Laszlos\source\repos\Penghou.
Sibling integration repositories: Penghou.Luban and Penghou.Hufu.

Read AGENTS.md, the [corrective delivery plan](resource-abstractions-corrective-plan.md),
[architecture baseline](resource-abstractions-architecture.md),
[original specification](resource-abstractions-proposal.md) and
[public-type inventory](resource-abstractions-inventory.md).

**Resume from the delivery ledger, not RA-1A again.**
[ADR 0003](decisions/0003-replaceable-resource-providers.md) selects the corrective
contracts. Review the implementation diff, complete meaningful Luban/Hufu
qualification and CI evidence, then finish RA-5B publication and RA-5C restores
against actual published versions. Candidate packages do not satisfy those gates.
The user has clarified the complete implementation and delivery objective.
RA-1A/RA-1B are the first design steps, not the stopping point. Continue through
RA-2, CI/package qualification, NuGet release, and actual Luban/Hufu adoption
under RA-3/RA-4/RA-5. Resolve ordinary implementation choices within the specification;
report external blockers precisely rather than stopping for another design approval.


The migration is installed in all three original repositories. Hufu's concurrent
public-keyring and async-authorizer changes are preserved. Its IO adapter uses
`IAuthorityRequestAuthorizer`; the current-snapshot constructor remains a
convenience overload. Candidate-package evidence is 19 IO, 95 existing Hufu and
80 Biscuit tests per framework. See the ledger for release state and Hufu's
recalibration record for the preserved pre-migration checkpoint. Hufu remains
untracked with no committed HEAD; do not commit its whole tree as an IO release.

## Current Luban leaf checkpoint — 2026-10-03

Luban D1–D6 and focused AI read usability are implemented and CI-qualified, with
337 tests passing on each supported framework. Read its
[completion ledger](../../Penghou.Luban/docs/leaf-completion.md) and
[consumer impact guide](../../Penghou.Luban/docs/consumer-impact.md) before any
producer edits. Hufu and workflow durability are outside this Luban feature gate.
The next consumer step is Hufu qualification and eventual exact Luban package
adoption. Existing Hufu test counts above belong to the earlier migration
checkpoint; do not report them as validation of the newest Luban revision.
Publication/adoption RA-5B/RA-5C remain open in the corrective ledger.

## Original review baseline (superseded where ADR 0003 records a correction)

- Diff/merge/transport already live in Luban. Do not move them again.
- WindowsWorkspacePath and ResourceRequestIdentity are implementation inside the
  abstraction assembly. The identity codec uses Windows path rules and also
  encodes HTTP requests; relocation needs a single shared implementation and
  explicit compatibility.
- Luban constructs Local readers in file/language/preview runtimes and Local
  patchers in execution; WorkspaceReference carries physical roots.
- The Local reader checks discovered children internally; the patcher checks
  the version, applies edits and records starts at a locked-object boundary.
  One outer authorization check cannot replace these safeguards.
- Hufu.Luban is a narrow language-authority integration, not Hufu.IO.
- IO.Abstractions and IO.Local are still non-packable.
- Capture-only WhatIf stays read-only. Overlay WhatIf is deferred and distinct.
- No production VFS, Baize extraction, HTTP redesign or SQLite split in this correction.

## Decisions to deliver

G1 enforcement cooperation; G2 neutral context/immutable snapshots/codec ownership;
G3 logical path and view/version semantics; G4 text materialization versus conditional
persistence; G5 capabilities/outcomes/conformance; G8 migration and package delivery.
Record chosen contracts and alternatives in linked ADRs. G6/G7 remain future VFS
gates, with compatibility constraints documented now.

The detailed plan supplies owners, exit criteria, test areas and known commands.
Use those gates rather than expanding scope when an interface seems inconvenient.

## Working-tree preservation

The prior review updated documentation across nine repositories. These updates
and the new canonical documents are not committed. Penghou additionally already
had edits in docs/local-reader-profile.md, docs/implementation-plan.md,
src/Penghou.IO.Local/LocalWorkspaceReader.cs and
tests/Penghou.IO.Tests/LocalWorkspaceReaderTests.cs before this preparation.
Luban has documentation edits. Hufu's project tree is untracked, including real
source/tests and prototype integrations. Inspect current status; this is a
snapshot, not permission to discard later changes.

Historical test counts in older docs are stale snapshots. The corrective ledger
records current test evidence. The initial preparation changed documentation
only; subsequent corrective work changes source and packaging. Delivery includes CI
and NuGet publication plus real consumer adoption; publish only qualified artifacts
with available release access, and preserve unrelated work in any release commits.

## Ready-to-send resume prompt

> Continue Penghou resource-abstraction correction from
> C:\Users\Laszlos\source\repos\Penghou\docs\resource-abstractions-sol-handoff.md.
> Follow the canonical architecture, ADR 0003 and corrective delivery ledger.
> Preserve existing changes, review the candidate implementation and finish its
> regression/integration qualification. Continue from current RA status rather
> than restarting completed design work. Complete build/test/pack CI and controlled NuGet
> publishing, publish the qualified corrected packages, and make Luban and Hufu
> consume the published versions with passing integration checks. Preserve
> existing work. Keep VFS and Baize/SQLite reorganization deferred. Continue until
> the end-to-end goal succeeds or a concrete external blocker requires user action.
> Report published versions, CI/release evidence and both consumer results.
