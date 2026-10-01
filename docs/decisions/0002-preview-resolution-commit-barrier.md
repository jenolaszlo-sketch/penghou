# ADR 0002: Support a host-owned preview and commit boundary

Status: Accepted design direction, 2026-10-01; integration pending.

Adopt the [preview/commit provider contract](../preview-commit-contract.md).
Refine the earlier static advisory preview: static preflight performs no
protected target I/O; resolution performs authorized bounded discovery and
captures proposed mutations; the host admits the complete known mutation set
before an executor-enforced barrier. Providers retain final live authority,
target binding and version checks.

Resolved plans and barriers belong to Luban/the host. Keep Penghou.IO.Abstractions
independent of the language, Hufu and workflow engines. Host-retained invocation
bindings must connect each mutation to its exact admitted plan/segment; plain
record constructors or agent-controlled flags cannot supply commit permission.

WhatIf never invokes requested mutations or opaque/lazy tools. Incomplete scans,
denials and missing guarantees cannot downgrade to weaker lazy execution.
Qualified unresolved effects may be admitted for actual execution separately,
with later dependent segments resolved and admitted afresh.

Whole-set admission is not a multi-file transaction or a path-race solution.
Post-start failures may leave partial outcomes requiring journal reconciliation.
No runtime behavior or new public resolved-plan API is implemented by this
documentation amendment. This refines [ADR 0001](0001-shared-resource-boundary.md).
