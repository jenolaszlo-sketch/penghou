# Penghou.Workflow.Abstractions

Dependency-free, immutable and bounded workflow authorization contracts for .NET 8 and .NET 10. Six public types express execution identity, requirements, context, closed outcomes, results and `IExecutionAuthorizer`; no engine or authority implementation is supplied.

Stable 1.x preserves source/binary and version-one value-schema compatibility. AssemblyVersion is 1.0.0.0. Contexts own their ordered requirement snapshots and compare them structurally. Identifiers are bounded UTF-8, ordinal and case-sensitive. Results do not grant actual resource authority by themselves.

See the [stable contract, compatibility and migration policy](https://github.com/jenolaszlo-sketch/penghou/blob/main/docs/workflow-stable-contract.md) for exact bounds, JSON defaults/strict-host profile, ordering, equality, error behavior and exclusions. Preview.2 consumers retain API signatures, but independently constructed equal contexts now compare equal; update exact package pins and review hash-key assumptions.
