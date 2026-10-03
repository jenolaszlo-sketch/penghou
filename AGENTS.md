# Penghou shared contracts and providers: contributor guidance

Workflow contracts: read [the workflow abstractions plan](docs/workflow-abstractions-plan.md).
Penghou.Workflow.Abstractions belongs here, uses product-neutral names and has
no dependency on Zhinu, Hufu or their implementations. Zhinu is one runtime;
Hufu.Workflow is one authorization adapter. WA-1/2/3 and Zhinu ZA-2 source
adoption are complete; ZA-3A/3B/4 and the unchanged preview.15 compatibility
suite are locally qualified and pushed. The next step is passing remote CI,
then user-run Zhinu 0.2.0-preview.1 publication through ZA-6. Hufu HA-1/2/3 follows that
publication. Luban LW-1 remains optional and independent. Existing IO package
boundaries remain independent.
Current checkpoint: [workflow package release handoff](docs/workflow-package-release-handoff.md).
WA-1/2/3 and Zhinu ZA-2 source adoption are complete. Version `0.1.0-preview.2`
is published and qualified; see the release handoff and package-adoption
evidence. Zhinu ZA-3A/3B/4 and the unchanged preview.15 compatibility suite are
locally qualified and pushed; next verify remote CI, then user-publish
`0.2.0-preview.1` through ZA-6. Hufu HA-1/2/3 follows publication. Do not treat contract package
qualification as runtime acceptance; use the current Zhinu authorization
qualification for runtime evidence.

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
