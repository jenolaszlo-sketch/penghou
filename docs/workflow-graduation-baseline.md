# Workflow graduation baseline — 2026-10-14

Repository: https://github.com/jenolaszlo-sketch/penghou. Branch: `codex/graduation-workflow`.
Baseline HEAD: `5d9854a09c8c243fd9e7a09bd6e8109db2937ca5`.
Published package: `Penghou.Workflow.Abstractions 0.1.0-preview.2`; target `1.0.0-rc.1` then `1.0.0`.
Only package in this track; dependency-free, net8.0/net10.0. Analyzer-only build dependency is private.
All 80 public API entries are Unshipped; Shipped contains only nullable directive.
Six intentionally public contract types; no experimental runtime or authority implementation.

Consumers: published Zhinu 0.2.0-preview.3 core, its SQLite adapter; optional Hufu.Workflow adapter (context only, implementation excluded). Existing neutral conformance fixtures and isolated package smoke exercise request binding, denials, provider failures, cancellation and ownership. Previous public preview.2 package evidence exists in workflow-public-package-qualification.json. It is historical, not RC qualification.

No database/persisted format or crash/concurrency implementation lives here. JSON constructors define version-one values; consumer-owned persisted envelopes/codecs need separate qualification. Immutable values support concurrent reads; mutable input construction requires callers to avoid concurrent mutation. Existing snapshots reject changed/inconsistent Count. No production runtime claim follows from neutral tests.

CI currently tests Ubuntu net8/net10 and Windows solution; release publisher currently targets unrelated Transport packages. Dedicated exact Workflow package qualification and scoped release selection are required.

## Decision inventory before contract edits

| Question | Disposition | Narrow decision |
|---|---|---|
| Collection ownership | must resolve before stable | Preserve enforced immutable snapshot, test aliases and record clones. |
| Context equality | must resolve before stable | Replace collection-reference equality with ordered structural value equality; preview-only behavior correction, signatures preserved. |
| Serialization/defaults | must resolve before stable | Document standard System.Text.Json default profile precisely; strict host profile rejects unknown properties. Fixed vectors freeze property names, value semantics and enum integers. No canonical byte encoding or host codec is added. |
| Bounds and missing values | must resolve before stable | Preserve existing constructor bounds/defaults; explicitly document optional versus required fields, null failures and defaults. Hosts bound serialized payloads. |
| Public record infrastructure | must resolve before stable | Preserve intentional equality/operators/clone/ToString for source/binary compatibility. ToString/hash are diagnostic/process-local, never durable identity. |
| Binary/version policy | must resolve before stable | Freeze existing signatures and assembly version 1.0.0.0 for 1.x; unchanged-preview and RC consumer probes, shipped baseline and package validation. |
| Canonical JSON, duplicate-property policy beyond STJ profile, digest | can explicitly remain unspecified | Host-owned versioned encoding; ordinary STJ last-property-wins behavior is documented, strict duplicate rejection remains host validation. |
| Runtime restart/durability, policy engine, atomic effects | future work not affecting 1.0 | Out of package scope; no engine semantics added. |
| Authority adapter internals | future work not affecting 1.0 | Hufu/Gagamba implementation excluded; no authority package dependency. |
