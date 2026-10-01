# Penghou.IO.Abstractions

Penghou.IO.Abstractions defines neutral, bounded contracts for workspace file
and directory access and for reading one web resource. It targets .NET 8 and
.NET 10. The library contains contracts only: no provider, runtime backend,
authorization implementation, or Hufu adapter is included.

Every operation carries a host-authenticated `HostInvocation` with subject,
effect, attempt, scope references, and request identity. These records are data
only: contracts do not authenticate host IDs or enforce checks. The host
composition must preflight the complete pipeline and known static effects
before I/O; the provider snapshots inputs and makes the final current-authority
check for each concrete action and resource. There is no ambient-user lookup or
default permit. A `WorkspacePath` reference describes provenance; it does not
grant permission.

The host validates the whole pipeline's syntax, types, and execution profile
before I/O. Luban owns semantic typed effects and finite dataflow; Fuwen and
Zhinu own workflow control and durability. Preflight checks all nodes' known
static effects and exact targets, rejecting a known denied write before
upstream reads or artifacts. This advisory check reserves no authority and
does not replace provider access checks. Dynamic targets require bounded,
authorized discovery. An `ls` list effect, a later `gc` read effect, and a
write each require their own check. A live denial can follow earlier reads, but
the denied write must not occur. There is no global pipeline atomicity;
intermediates stay lazy, bounded, and cancellable, while persistence and
debug/release artifacts require explicit authorization.

[ADR 0002](docs/decisions/0002-preview-resolution-commit-barrier.md) adds the
planned [preview resolution and commit barrier](docs/preview-commit-contract.md):
authorized discovery resolves dynamic mutation targets, the complete known
mutation set is admitted before writes begin, and every actual I/O still receives
live checks. WhatIf leaves opaque tools unexecuted. Resolution and the barrier
are not implemented, and batch admission does not promise atomic multi-file writes.

See [architecture](docs/architecture.md), [provider contract](docs/provider-contract.md),
the [roadmap](ROADMAP.md), and [decision 0001](docs/decisions/0001-shared-resource-boundary.md).
The [Apache-2.0 license](LICENSE) applies.

The [implementation plan](docs/implementation-plan.md) makes the next delivery
a Windows read-only `Penghou.IO.Local` provider, followed by Luban's existing-read
migration. That provider is planned; the library above remains interfaces only.

Build with the .NET 10 SDK:

```sh
dotnet build Penghou.IO.slnx -c Release
```

The build produces libraries for .NET 8 and .NET 10. Package publication is
disabled while these initial contracts and their first providers are reviewed.
