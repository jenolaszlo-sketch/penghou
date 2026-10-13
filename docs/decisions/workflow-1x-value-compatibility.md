# ADR: Workflow 1.x value ownership and compatibility

Status: Accepted, 2026-10-14. Scope: Penghou.Workflow.Abstractions only.

Version-one neutral authorization values remain independent of runtime/authority implementations. Preserve all 80 published API entries, including generated record infrastructure. Context owns its immutable ordered snapshot; equality compares that snapshot structurally. Retaining collection-reference equality would make equal independently decoded persisted requests unequal and permanently expose an accidental allocation detail. Changing to classes or a new collection API would impose needless preview source/binary migration. Structural equality preserves signatures; its one preview behavioral change is called out explicitly.

1.x fixes AssemblyVersion 1.0.0.0 and promises source/binary, valid-input behavior, ordered value equality, bounds and version-one schema compatibility. Package validation and unchanged compiled consumers distinguish API checks, loading and exercised behavior. Hash/diagnostic representation is unspecified and never durable identity.

System.Text.Json remains value construction, not a universal trusted codec: native defaults and fixed vectors are documented; strict hosts reject unmapped fields and duplicate ambiguity and bound transport bytes. Runtime-owned canonical digest/envelope versions remain outside this package. Adding a serializer implementation would enlarge this foundation without settling a consumer-owned trust boundary.

This refines workflow-authorization-contract and supersedes its silence on context collection equality and 1.x binary policy. It does not alter runtime approval, freshness, fence, evidence or resource-effect responsibilities.
