# Penghou.IO.Abstractions

Penghou.IO.Abstractions defines neutral, bounded contracts for workspace file
and directory access and for reading one web resource. It targets .NET 8 and
.NET 10. The abstractions library contains contracts and a pure canonical request
identity codec. The separate Penghou.IO.Local project implements bounded Windows
reads, file metadata, directory pages, and a narrow existing-file patch profile
with required host-supplied authorization and mutation-journal contracts. The
full IWorkspaceWriter surface, web backend, Hufu adapter, and production authority
policy are not implemented.

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
intermediates use finite buffered values with byte/work bounds and cancellation; lazy production and backpressure are not guaranteed. Persistence and debug/release artifacts require explicit authorization.

[ADR 0002](docs/decisions/0002-preview-resolution-commit-barrier.md) adds the
planned [preview resolution and commit barrier](docs/preview-commit-contract.md):
authorized discovery resolves dynamic mutation targets, the complete known
mutation set is admitted before writes begin, and every actual I/O still receives
live checks. WhatIf leaves opaque tools unexecuted. Luban now implements a separate programmatic capture-only WhatIf profile using
this reader; its plans cannot commit and WhatIf never dispatches the separate executor. Luban now has a narrow standalone single-exact-target patch executor that requires explicit host admission, live resource checks, and journaled start/completion evidence. It does not turn CanCommit on or implement a general batch barrier. Hufu/Zhinu adapters, broader admission/recovery, and atomic multi-file writes remain pending.

See [architecture](docs/architecture.md), [provider contract](docs/provider-contract.md),
the [roadmap](ROADMAP.md), and [decision 0001](docs/decisions/0001-shared-resource-boundary.md).
The [Apache-2.0 license](LICENSE) applies.

The [implementation plan](docs/implementation-plan.md) records
the Windows read-only `Penghou.IO.Local` provider and Luban's read migration
as completed slices. See its qualified [profile](docs/local-reader-profile.md)
and [identity encoding](docs/canonical-request-identity.md); the Local existing-file patcher is a separate narrow profile. Other mutations and web remain pending.

Build with the .NET 10 SDK:

```sh
dotnet test Penghou.IO.slnx -c Release
```

The build produces libraries for .NET 8 and .NET 10. Package publication is
disabled while these initial contracts and their first providers are reviewed.
