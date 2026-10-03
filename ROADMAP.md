# Penghou contracts and providers roadmap

## Current workflow contract delivery - 2026-10-03

Read [the workflow abstractions plan](docs/workflow-abstractions-plan.md) and
the [cross-project activities](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/authority-extension-activities.md).
Penghou owns `Penghou.Workflow.Abstractions`; product names do not belong in
the shared contract package. Neither Zhinu nor Hufu is required by the contracts.

- [x] **WA-1:** neutral contracts and ZA-1 design input selected in the
  [contract profile](docs/workflow-authorization-contract.md) and
  [source review](docs/workflow-contract-review.md); additive seam, no engine type moves.
- [x] **WA-2:** isolated dependency-free package, API/conformance checks,
  .NET 8/10 tests, strict TFM package checks and fresh-cache consumer proof pass.
  All seven remote CI jobs passed in
  [run 37115430526](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37115430526).
- [x] **WA-3:** published exact version `0.1.0-preview.2`; all four publication
  jobs passed in [run 37116694209](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37116694209).
  Public package contents match CI apart from the repository signature, and
  fresh-cache NuGet-only consumers pass on .NET 8/10. See the
  [release checkpoint](docs/workflow-package-release-handoff.md) and
  [qualification record](docs/workflow-public-package-qualification.json).

Zhinu ZA-2 source adoption is complete, including the fresh seven-package
consumer graph. The exact core package pin and no-Hufu dependency graph passed
on .NET 8/10; see the [adoption checkpoint](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/workflow-package-adoption.md).
Zhinu ZA-3A/3B/4 are locally qualified in candidate `0.2.0-preview.1`, and the
unchanged preview.15 compatibility suite passed on .NET 8/10. Verify remote CI and complete
user-run ZA-6 publication with remote CI are next. Hufu HA-1/2/3 follows that publication; HA-0A/B cleanup
remains independent, and the old staged Hufu snapshot stays on hold. Luban
LW-1 is optional neutral-host integration work; keep the
language core independent of workflow authorization and of Zhinu/Hufu. IO/Luban
boundaries are unaffected; the IO queue below remains a separate ledger.

## Penghou I/O roadmap

Active delivery queue: [corrective plan](docs/resource-abstractions-corrective-plan.md).
Resume with [Sol's handoff](docs/resource-abstractions-sol-handoff.md):
the current ledger and [ADR 0003](docs/decisions/0003-replaceable-resource-providers.md).
RA-0/RA-1 are complete. Corrective code, candidate packages and remote CI are qualified;
NuGet publication and actual published-package adoption remain open.
The [release checkpoint](docs/resource-package-release-handoff.md) records the validated
version tag, missing publishing identity and isolated real-consumer package evidence.
The plan supplies owners, dependencies, exit criteria and qualification evidence.

## Architecture correction takes precedence for new work — 2026-10-02

The [resource-abstractions architecture](docs/resource-abstractions-architecture.md)
and [original specification](docs/resource-abstractions-proposal.md) define the
intended replaceable capability boundary. The earlier milestones below record
qualified implementation history; their package layout is not the target design.
The [public-type inventory](docs/resource-abstractions-inventory.md) completes RA-0.

- [x] **RA-1:** resolve the linked G1-G5/G8 contract, enforcement, patch and compatibility gates in ADR 0003.
- [x] **RA-2:** keep contract DTOs/interfaces in Abstractions; relocate Windows path
  behavior and the canonical codec implementation with preserved identity vectors.
  Record separate future ownership for HTTP; do not redesign it in this correction.
- [ ] **RA-3 (candidate qualified; published adoption pending):** coordinate Luban's injected capabilities and isolate physical composition.
- [ ] **RA-4 (candidate qualified; published adoption pending):** qualify a Hufu.IO boundary with discovered-resource and commit checks.
- [x] **RA-5A:** architecture/conformance tests, build/test/pack CI and local-feed qualification.
- [ ] **RA-5B:** controlled NuGet release workflow and published corrected IO.Abstractions/Local versions.
- [ ] **RA-5C:** Luban and Hufu use those published versions with passing consumer integration checks.

The finish line is corrected ownership/code, working CI and NuGet delivery, then
real published-package consumption by both Luban and Hufu. Design work is the
first stage, not completion; local packages or sibling references do not satisfy it.

Diff/merge already belong to Luban. A simple outer authorization check must not
replace current child-resource checks or the mutation-start protocol.
Keep current capture-only WhatIf unchanged until a separate virtual execution
profile is qualified. No production virtual provider is required for RA completion.

Deferred: **VFS-1** in-memory, **VFS-2** snapshot, **VFS-3** overlay,
**VFS-4** operation evidence/resource delta, **VFS-5** isolated WhatIf,
**VFS-6** declared/observed analysis, **VFS-7** approved application,
**VFS-8** filtered views, **VFS-9** Git, **VFS-10** remote/container integration.
The canonical architecture gives owners, dependencies and acceptance conditions.
Every handoff must cite its RA/VFS ID and unresolved gates.

Status: Revised 2026-10-01. Neutral .NET 8/.NET 10 I/O interfaces, canonical
identity codec, Windows read-only Local provider, and a narrow existing-file
Local NTFS patch profile are implemented. Luban uses the shared reader and its
standalone exact-target executor. The full IWorkspaceWriter surface, web,
general multi-target admission/barrier, production authority policy, and Hufu /
Zhinu adapters remain pending. See the [implementation plan](docs/implementation-plan.md).

## Delivered foundation and next gates

Versioned [request identity](docs/canonical-request-identity.md) and the
[Windows read-only profile](docs/local-reader-profile.md) now support Luban
Read/Find/SearchText. Luban's typed read language/static preflight and capture-only WhatIf are
implemented. The narrow Local patcher plus Luban's separate single-target
executor implement one standalone write slice; broad writer APIs, batch
admission/recovery, and governed adapters remain future gates.

## Ordered gates

1. **Contract and read provider — complete.** One neutral canonical identity codec; bounded
   reads/file metadata/direct listing; explicit host authorizer and parent
   bindings; candidate exclusions, paging, versions, cancellation and honest
   path/race guarantees. Real workspace tests on .NET 8 and 10.
2. **Luban migration — complete.** Existing effect checks and typed outputs preserved on
   the real shared reader. Document a pinned sibling-checkout integration build
   rather than depend on unpublished packages or duplicate contract code.
3. **Single-file patch profile — implemented.** The Local provider supports one
   existing-file original-version UTF-8 byte patch on qualified fixed-drive
   NTFS. Luban's standalone executor requires host admission, live resource
   checks, a serialized start fence and journaled outcomes. It is not a general
   IWorkspaceWriter implementation. Broader batches, recovery, and governed
   Hufu execution remain pending; missing guarantees remain Unsupported.
4. **Broader resource profiles.** Additional metadata/mutations and both-endpoint
   rights, preconditions and recovery; opaque versions are not assumed content hashes.
5. **Governed/durable adapters and web.** Hufu/Zhinu integration after their own
   prerequisites; bounded GET with qualified URL/endpoint/redirect/SSRF/decompression
   and deadline behavior. Supervisor support is separately qualified.

Luban/the host owns language, static preflight, resolution, immutable plans,
whole-batch admission and commit barrier. Providers preserve concrete request/
plan bindings and final live resource checks. WhatIf invokes no mutations or
opaque tools; incomplete coverage and denials never downgrade to lazy execution.
The barrier is not a multi-file transaction. See the
[preview/commit contract](docs/preview-commit-contract.md).

Use Luna for bounded implementation/tests where possible, review identity and
native consistency gates, and release packages only after qualification and
the configured release controls. Publication and both consumer migrations are
part of the clarified delivery goal. No enforcement claim follows from interfaces.
