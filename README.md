# Penghou I/O

Penghou I/O supplies bounded resource capabilities for AI tool usage, with
explicit authorization at each concrete resource boundary. Trusted hosts inject
providers and authority implementations; Hufu is an optional integration.

Architecture direction (2026-10-02): [replaceable resource capabilities](docs/resource-abstractions-architecture.md),
with the [original specification](docs/resource-abstractions-proposal.md),
[public-type inventory](docs/resource-abstractions-inventory.md) and
[corrective roadmap](ROADMAP.md). [ADR 0003](docs/decisions/0003-replaceable-resource-providers.md)
records the implemented correction. The [delivery plan](docs/resource-abstractions-corrective-plan.md)
and [handoff](docs/resource-abstractions-sol-handoff.md) distinguish qualification,
publication and consumer adoption.

Three .NET 8 and .NET 10 NuGet package candidates share version
`0.1.0-preview.1`; publication status is recorded in the delivery plan.

- `Penghou.IO.Abstractions` contains the neutral request, result, provider and
  authorization contracts. It has no provider or protocol implementation
  dependency.
- `Penghou.IO.Protocols` contains the versioned request identity codec and the
  Windows workspace path profile. It depends on the neutral contracts package.
- `Penghou.IO.Local` contains the Windows filesystem provider and depends on both
  the contracts and protocol packages. It applies host-supplied authorization and
  mutation-journal checks at the concrete resource boundary.

Build and test both target frameworks, with the native Windows provider qualified
on Windows. See the [release guide](docs/releasing.md) for the validated tag and
NuGet publication workflow.

Every operation carries a host-authenticated `HostInvocation` with subject,
effect, attempt, scope references, and request identity. These records are data
only: contracts do not authenticate host IDs or enforce checks. The host
composition authenticates the operation and its scope; the provider snapshots
inputs and makes the final current-authority
check for each concrete action and resource. There is no ambient-user lookup or
default permit. A `WorkspacePath` reference describes provenance; it does not
grant permission.

Luban validates its whole pipeline's syntax, types, and execution profile
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
this reader; its plans cannot commit and WhatIf never dispatches the separate
executor. Luban's single and batch patch executors require explicit host admission,
live resource checks and journaled start/completion evidence. Luban materializes
the exact text edits; Local conditionally persists frozen bytes. Batch execution
is ordered and can leave a completed prefix; it is not an atomic transaction.
Production mutation-host composition and terminal-outcome recovery remain separate
qualification obligations.

See [architecture](docs/architecture.md), [provider contract](docs/provider-contract.md),
the [roadmap](ROADMAP.md), and [decision 0001](docs/decisions/0001-shared-resource-boundary.md).
The [Apache-2.0 license](LICENSE) applies.

The [implementation plan](docs/implementation-plan.md) records the Windows
read/list provider and Luban's read migration. See its qualified
[profile](docs/local-reader-profile.md), [identity encoding](docs/canonical-request-identity.md),
and the separate Local [conditional existing-file write profile](docs/local-patch-profile.md). Other mutations
and the web backend remain pending.

Build with the .NET 10 SDK:

```sh
dotnet test Penghou.IO.slnx -c Release -f net8.0
dotnet test Penghou.IO.slnx -c Release -f net10.0
```

CI runs these Windows tests, builds the neutral contracts and protocols on Linux,
packs all three packages, verifies their identities and contents, and restores a
separate package-only consumer from the generated local feed. CI packages use a
unique prerelease version and are not published.
