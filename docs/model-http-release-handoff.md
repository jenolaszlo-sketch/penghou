# Model/HTTP contract release checkpoint

Updated 2026-10-04. New contract implementation and local package
qualification are complete; remote CI, publication and consumer adoption remain open.

| Gate | State |
| --- | --- |
| GM-0 | Source operation inventory and contract decisions recorded in [ADR 0004](decisions/0004-model-http-transport-contracts.md) and [consumer matrix](model-http-consumer-matrix.md). |
| GM-1 source | Two neutral contract projects, bounded snapshots, typed model/stream seam, finite HTTP body/owned response, neutral failures and API baselines implemented. |
| GM-1 local contracts | Windows .NET 8/.NET 10: 14 model + 26 HTTP cases per framework, 80 passed, zero failed/skipped. |
| GM-1 package/solution | Complete: full Windows repository regressions on both frameworks, both nupkg/snupkg pairs, dependency-free package inspection and fresh artifact-only package consumers; existing IO/Workflow profiles also pass. |
| GM-1 remote | Windows/Linux/macOS .NET 8/10 CI is configured; delivery is recorded in Git history; remote run verification remains open. |
| GM-1P | User-controlled input-free main publication of both new packages, indexing and exact public-byte verification remain open. |
| Baize GM-2/3/4/5/6 | Not migrated. Consume exact published versions after GM-1P; default/denial/all-route conformance and release then follow. |
| Hufu HM-1 onward | Separate optional enforcement integration after Baize seam coverage. |

Release only `Penghou.Model.Abstractions` and `Penghou.Http.Abstractions`, each
`0.1.0-preview.1`. Existing IO `0.1.0-preview.1` and workflow `0.1.0-preview.2`
are unchanged. Do not bump or republish them as part of this stage.

Read the [contract manual](model-http-contracts.md) and
[release instructions](releasing.md). The existing `.github/workflows/publish.yml`
is now **Publish transport abstractions to NuGet**; it has no inputs, allows
`main` only, and reuses OIDC/`nuget` controls. Confirm the existing publisher's
package scope covers both new IDs if it was restricted. Credentials never enter
build/test/consumer jobs. Packages/symbols are pushed separately once each.

Local candidates were built from the uncommitted working tree based on Penghou
`e451b71c4927d6106834b8a49f145411bdd05648`; they are qualification candidates,
not exact committed release artifacts. CI/release packages are rebuilt from the
event's exact committed revision. Publication verification excludes only NuGet's
repository signature and must use those same validated artifacts.

No Hufu policy, Baize provider, budget store, credential broker or sandbox is
implemented by these packages. Generic payload validation/snapshotting, concrete
HTTP limits/redirect enforcement and actual authority remain provider/host
obligations. All new tests use finite fake transports; no paid provider calls.

Next: verify remote CI for the pushed implementation commit,
then the user runs publication on `main`. After public indexing, resume Baize
from [its handoff](../../Penghou.Baize/docs/governable-model-http-handoff.md).

## Local evidence

[Qualification ledger](model-http-qualification.json) records package hashes,
base revisions, precise test counts and open remote/publication gates.
The full Penghou solution passed 123 cases on .NET 8 and
123 on .NET 10, with zero failures/skips. The new transport
contribution is 40 cases per framework. Baize's existing non-live .NET 8 solution
baseline passed 827 cases, with 14 existing skips
and zero failures; these are baseline results, not new transport adoption evidence.
The full supported Baize unit solution was used; paid/live integration was excluded.

Both candidate packages contain only their own net8/net10 DLL/XML assets and
README, with no runtime dependencies. Fresh local-feed-only consumers compiled
and ran typed model/stream and HTTP decoration, request/result binding, nullable
cache usage, snapshots, denied zero inner dispatch and response disposal on both
frameworks. Existing IO/Workflow package-only consumers and profiles still pass.
Candidate hashes identify local qualification bytes, not public release artifacts;
CI rebuilds the committed source and produces the artifacts used for publication.
