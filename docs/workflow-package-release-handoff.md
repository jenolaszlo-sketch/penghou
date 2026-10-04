# Workflow abstractions: published package checkpoint

Updated 2026-10-03. Repository: `C:/Users/Laszlos/source/repos/Penghou`.
Published: **Penghou.Workflow.Abstractions 0.1.0-preview.2**.

WA-1 contract design and ZA-1 design input are selected in the
[contract manual](workflow-authorization-contract.md) and
[source review](workflow-contract-review.md). WA-1/2/3 are complete. Source
commit `5a76b7c` passed all seven CI jobs in
[run 37115430526](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37115430526).
The exact `0.1.0-preview.2` package is published; all four publication jobs
passed in [run 37116694209](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37116694209).
See the [public-package qualification record](workflow-public-package-qualification.json).

## Change and qualification

- Six neutral public contract types: authorizer interface, execution identity,
  immutable context, versioned requirement, closed decision enum and result.
  No engine, SQL, IO/Hufu/Zhinu dependency or shipped authorizer implementation.
- Constructors bound Unicode/UTF-8 inputs, snapshot at most 64 declarations,
  validate plan pairing, require freshness/request identity, and reject unknown
  context/result schemas. Allow requires expiry; approval requires correlation.
- All 73 IO regression cases and 10 new workflow contract/conformance cases
  pass on each of .NET 8 and .NET 10: **166 total, zero failed/skipped**.
  Fixtures cover malformed data, immutable snapshots, unknown schemas,
  denied/pending/unavailable/error results, missing trusted binding/evidence,
  null/throwing providers, mismatched request/provider, future/expired outcomes,
  post-await revision fencing and cancellation. They are neutral test consumers,
  not proof of actual Zhinu behavior.
- Strict compatible-TFM package validation passes. The enforced 80-symbol API
  inventory builds cleanly; removing the interface entry fails with RS0016,
  and the original inventory was restored before final qualification.
- The nupkg/snupkg contain exactly the workflow net8/net10 DLL/XML assets and
  package-specific README; the nuspec has no dependency. A fresh-cache local-feed
  consumer has one exact package reference/target graph and builds/runs a
  request-bound Denied proof with zero protected callbacks on both frameworks.
- The exact published package contents match the CI artifact apart from the
  repository signature. Fresh-cache consumers restore only from NuGet.org and
  pass build/runtime checks on .NET 8 and .NET 10. The neutral package has no
  dependencies. This evidence qualifies package contents and consumption; it
  does not establish runtime behavior in Zhinu or Hufu.

## Source push and publication

The contract source, tests, manuals and workflow/scripts were pushed in
`5a76b7c` and published by the user through the validated release workflow.
No public engine API moved, so type forwarding is not part of this additive
package release. Subsequent Zhinu adoption is recorded in its
[adoption checkpoint](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/workflow-package-adoption.md).

The package has its own checked-in PackageVersion; future shared IO version
changes cannot silently change it. Existing IO preview packages remain immutable.
Ordinary CI still qualifies IO using unique CI versions, and separately packs
and consumes Workflow artifacts. The same existing **Publish to NuGet** workflow
publishes **only Workflow.Abstractions**, from an input-free manual run on `main`,
using the existing NuGet environment/OIDC identity. It validates the exact
commit and artifact before uploading; it never repacks/reuploads existing IO
preview versions. See [release instructions](releasing.md).

Zhinu **ZA-2 source adoption is complete**, including the fresh seven-package
consumer closure on .NET 8/10. The exact core package pin passed and the
resolved graph contains no Hufu package. ZA-3A/3B/4 are also locally qualified:
1,017 runtime tests passed (506 on .NET 8, 511 on .NET 10), and the isolated
seven-package consumer passed on both TFMs. See the [adoption checkpoint](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/workflow-package-adoption.md)
and [authorization qualification](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/qualification/workflow-authorization.json).
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
The current local source suite passed 816 cases, 408 per .NET 8/10 framework
(210 core, 93 Biscuit, 19 IO, 22 legacy, 52 Workflow unit, 12 integration).
The bounded request-preflight telemetry slice adds 26 cases per framework.
A finite worker queue emits closed categories/timing without request metadata;
listener loss, saturation and shutdown preserve mandatory evidence/results. See
[telemetry profile](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/optional-telemetry.md)
and [current evidence](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/optional-telemetry.json).
The 764-case explanation checkpoint remains historical evidence. Earlier work
was committed as Hufu 42a045b, Penghou 77bac95 and Zhinu a1df6e9; the telemetry
delivery is local, with push/remote CI and user-controlled Hufu publication open.
The bounded typed-path explanation slice adds 32 cases per framework, with
actual evaluator capture and separately authorized redacted disclosure. See
[profile](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/decision-explanations.md)
and [explanation evidence](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/decision-explanations.json).
The earlier 700-case core checkpoint remains historical evidence.
Independent core admission/issuance adds 73 cases per framework and no engine
dependency; see [the profile](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/core-admission-and-issuance.md)
and [earlier core qualification](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/core-hardening.json).
The earlier 554-case workflow qualification remains historical evidence.
HA-2 fresh candidate-package qualification passed on both frameworks; HA-3
remote CI and user-run Hufu publication remain open. No Hufu package or production
host is claimed published. See the [Hufu handoff](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/zhinu-authority-handoff.md) and
[qualification ledger](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/workflow-authorization.json).

The legacy preview.15 compatibility suite remains qualified and frozen.
Luban LW-1 remains optional neutral-host operation/requirement mapping; its
language core stays independent. Production host acceptance and any legacy
ZA-5B replacement retain separate resource/effect guarantees.

## Resume prompt

> Resume Hufu from HA-0A/B and HA-1 completion. WA-1/2/3 and Zhinu
> ZA-2/3A/3B/4/6 are complete and published; do not redo or republish them.
> Read the [Hufu handoff](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/zhinu-authority-handoff.md) and current qualification ledger.
> Local HA-2 package-backed qualification passed. Push the reviewed
> Hufu implementation/tooling, verify both OS CI jobs and let the user run
> HA-3 publication. Keep the six Hufu candidate packages distinct from
> experimental Biscuit and frozen legacy preview.15 profiles. Keep production
> trusted-host acceptance, deferred Luban integration and actual effect checks
> separate; local component tests never grant runtime permission.
