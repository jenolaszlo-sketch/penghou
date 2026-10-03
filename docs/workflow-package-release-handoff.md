# Workflow abstractions: release candidate checkpoint

Updated 2026-10-03. Repository: `C:/Users/Laszlos/source/repos/Penghou`.
Candidate: **Penghou.Workflow.Abstractions 0.1.0-preview.2**.

WA-1 contract design and ZA-1 design input are selected in the
[contract manual](workflow-authorization-contract.md) and
[source review](workflow-contract-review.md). WA-2 source, tests, API inventory
and CI are implemented and locally qualified. This source release includes the
preview.2 version bump; remote Windows/Linux CI remains a publication gate.
WA-3 NuGet publication is pending and is
performed by the user. Do not resume runtime implementation before WA-3.

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
- The public NuGet version checker reports this workflow ID/version absent at
  qualification time. It rechecks actual release bytes in CI; this is not a
  claim that publication has happened or that availability cannot change.

## Source push and publication

Review the working-tree diff and include the new source/tests/manuals plus
workflow/script changes. Preserve existing IO planning updates. No source was
changed in Zhinu or Hufu by this package delivery; sibling activity/handoff
documents record the new checkpoint only. No public engine API moved, so old
binary forwarding/migration is not part of this additive package release.

The package has its own checked-in PackageVersion; future shared IO version
changes cannot silently change it. Existing IO preview packages remain immutable.
Ordinary CI still qualifies IO using unique CI versions, and separately packs
and consumes Workflow artifacts. The same existing **Publish to NuGet** workflow
publishes **only Workflow.Abstractions**, from an input-free manual run on `main`,
using the existing NuGet environment/OIDC identity. It validates the exact
commit and artifact before uploading; it never repacks/reuploads existing IO
preview versions. See [release instructions](releasing.md).

After push, wait for remote CI. The user then runs publication. Record the
public package/version and fresh-cache public-feed consumer proof before closing
WA-3 and resuming **Zhinu ZA-2**. Hufu.Workflow implementation follows the
completed Zhinu phase. The user's next integration scope also includes Luban:
additive neutral operation/requirement mapping through an optional host/adapter
or reviewed neutral hook, with no mandatory Zhinu/Hufu dependency in the language.
Keep Luban semantic admission and actual provider checks. Actual durable approval/dispatch/resource enforcement,
host production qualification and legacy ZA-5B disposition remain separate.

## Resume prompt

> Qualify remote CI for the Penghou.Workflow.Abstractions preview.2 source release in Penghou.
> Read this checkpoint and the contract manual; do not redo WA-1 or extract engine
> types. Await remote CI and user NuGet publication, then qualify exact public
> package adoption and close WA-3. Only then resume Zhinu ZA-2/3/4/6; Hufu.Workflow
> follows later. Never bulk-install the older Hufu staging snapshot or republish
> existing immutable IO packages for this release.
