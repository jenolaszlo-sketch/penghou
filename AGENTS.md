# Penghou shared contracts and providers: contributor guidance

Workflow contracts belong here and remain product-neutral, with no runtime or
authority implementation dependency. Read [the workflow plan](docs/workflow-abstractions-plan.md)
and [release handoff](docs/workflow-package-release-handoff.md).

Updated 2026-10-04. WA-1/2/3 and Zhinu ZA-2/3A/3B/4/6 are complete.
`Penghou.Workflow.Abstractions` 0.1.0-preview.2 and all seven Zhinu
0.2.0-preview.1 packages are published and indexed. Zhinu source commit
`2f02a2e91d87e6429fd17a3819308301ab91f17c` passed both OS jobs in
[CI 37137640422](https://github.com/jenolaszlo-sketch/penghou-zhinu/actions/runs/37137640422)
and [publication 37138352675](https://github.com/jenolaszlo-sketch/penghou-zhinu/actions/runs/37138352675).
All seven public packages were downloaded; hashes and exact repository commit
metadata are recorded in [public-release evidence](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/zhinu-public-release.json).

Hufu HA-0A/B review and test isolation are complete; HA-1 is implemented.
The optional adapter depends only on Hufu and the exact neutral contract.
The current local source suite passed 764 cases, 382 per .NET 8/10 framework
(184 core, 93 Biscuit, 19 IO, 22 legacy, 52 Workflow unit, 12 integration).
The bounded typed-path explanation slice adds 32 cases per framework, with
actual evaluator capture and separately authorized redacted disclosure. See
[profile](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/decision-explanations.md)
and [current evidence](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/decision-explanations.json).
The earlier 700-case core checkpoint remains historical evidence.
Independent core admission/issuance adds 73 cases per framework and no engine
dependency; see [the profile](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/core-admission-and-issuance.md)
and [earlier core qualification](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/core-hardening.json).
The earlier 554-case workflow qualification remains historical evidence.
HA-2 fresh candidate-package qualification passed on both frameworks; HA-3
remote CI and user-run Hufu publication remain open. No Hufu package or production
host is claimed published. See the [Hufu handoff](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/zhinu-authority-handoff.md) and
[qualification ledger](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/workflow-authorization.json).

Implementation handoff: read [the corrective plan](docs/resource-abstractions-corrective-plan.md)
and [Sol's resume point](docs/resource-abstractions-sol-handoff.md). Resume from
the current delivery ledger and ADR 0003; old delivery history is not the active queue. Inspect and preserve
existing changes across sibling repositories. Complete the decisions and protocol
traces before changing dependent APIs; do not expand into deferred VFS or package
reorganizations. Report current evidence rather than copying historical test counts.
The clarified delivery includes correcting code ownership, CI and NuGet publishing,
then Luban/Hufu consumption of published versions. Continue beyond design gates;
local-feed checks or sibling-source builds alone cannot mark the task complete.

Architecture direction: read [resource abstractions](docs/resource-abstractions-architecture.md)
and its [public-type inventory](docs/resource-abstractions-inventory.md) first.
They define the RA corrective gates and deferred VFS milestones. The requirements
below protect today's qualified profiles during migration; do not use their
current packaging as the target design. Keep contracts free of substantive
algorithms and OS helpers, use injected provider capabilities, and preserve
per-resource/commit checks through a qualified neutral cooperation boundary.
Test-only spies for conformance are allowed; do not ship placeholder providers.
Current no-mutation WhatIf rules apply to capture-only preview. Future virtual
execution requires a separate profile and cannot weaken real enforcement.
Every handoff cites the canonical document, RA/VFS ID, open gates and evidence.

Keep the shared contract packages in this repository independent and product-neutral. Keep Penghou.IO.Abstractions an independent, neutral I/O contract library. Do not add
dependencies on Luban, Hufu, Fuwen, Zhinu, a host UI, MCP, or an agent runtime.
Luban owns semantic typed effects and finite dataflow; Fuwen and Zhinu own
workflow control and durability. This library defines contracts for a
provider's final checks against concrete resources.

Every operation requires host-authenticated invocation, subject, effect,
attempt, scope references, and request identity. Contract records are data only:
they do not authenticate those IDs or enforce checks. The host resolves scope
references against retained mappings, preserves the parent ceiling, and
preflights all known static effects before I/O. The provider snapshots mutable
inputs, verifies the exact request identity, and makes a final current-authority
check for each concrete resource. There is no ambient-user lookup or default
permit. Batch operations authorize each source and destination separately.
File references establish provenance only; they do not convey permission.

Keep APIs bounded and typed. Do not expose raw streams, native handles, shell or
process escape hatches, arbitrary callbacks, or an unbounded System.IO mirror.
Keep backends out of Penghou.IO.Abstractions. A real Penghou.IO.Local provider
belongs in its own project when implementation is authorized; do not add fake
backends or authorizers as scaffolding. Read docs/implementation-plan.md. Document
provider preconditions and limits before introducing behavior. Hufu remains
independent; any adapter belongs in a separate integration package.

Read docs/preview-commit-contract.md and ADR 0002 before implementing providers.
Keep static preflight separate from authorized read-only resolution. WhatIf
must not invoke requested mutations or opaque/lazy tools. Planned governed
mutations bind to the exact host-admitted plan/segment and released commit
barrier; every concrete access still needs a live check. Incomplete coverage or
denial cannot become lazy fallback. No global transaction or sandbox is implied.
