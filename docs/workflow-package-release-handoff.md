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
[adoption checkpoint](../../Penghou.Zhinu/docs/workflow-package-adoption.md).

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
seven-package consumer passed on both TFMs. See the [adoption checkpoint](../../Penghou.Zhinu/docs/workflow-package-adoption.md)
and [authorization qualification](../../Penghou.Zhinu/docs/qualification/workflow-authorization.json).
The unchanged preview.15 compatibility suite also passed on .NET 8/10. Next is
committing/pushing and user-run **ZA-6 publication** of Zhinu `0.2.0-preview.1`
with remote CI. No publication is claimed. Hufu HA-1/2/3 follows ZA-6. HA-0A/B cleanup can proceed independently; keep the older staged
Hufu snapshot on hold and preserve the frozen legacy-profile decision.
Luban LW-1 is optional neutral-host operation/requirement mapping; the language
core stays independent of workflow authorization, Zhinu and Hufu. Durable
approval/dispatch/resource enforcement, production host qualification and
legacy ZA-5B disposition remain separate gates.

## Resume prompt

> Continue from the completed WA-1/2/3 checkpoint for Penghou.Workflow.Abstractions
> 0.1.0-preview.2. Read this handoff and the contract manual; do not redo WA-1 or
> extract engine types. Zhinu ZA-2 exact published-package source adoption and
> fresh seven-package consumer closure are complete on .NET 8/10; see the
> adoption checkpoint. ZA-3A/3B/4 and the unchanged preview.15 compatibility
> suite are locally qualified; commit/push and user-publish Zhinu 0.2.0-preview.1
> through ZA-6 with remote CI. Hufu HA-1/2/3 follows the publication.
> HA-0A/B cleanup is independent; keep the older Hufu staging snapshot on hold
> and preserve the legacy-profile decision. Luban LW-1 is optional neutral-host
> integration, with the language core independent. Do not treat contract-package
> evidence as runtime acceptance; current runtime evidence is recorded in the
> Zhinu authorization qualification. Do not claim publication of Zhinu
> 0.2.0-preview.1 or Hufu packages until their user-run workflows pass, and do
> not republish existing immutable IO packages.
