# Resource package release checkpoint

Updated 2026-10-03. IO RA-5B publication is verified; RA-5C public-feed adoption remains open.

## Verified public IO release

All three IO packages at 0.1.0-preview.1 are published and downloadable from
NuGet.org. [Release run 37093330455](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37093330455)
validated commit 69db191f3a36e2122a9301b2dcb017828e918ca6: Windows tests,
Linux builds, package checks and isolated consumption passed. All three package
uploads and all three symbol uploads succeeded. Only the final indexing check
failed after its approximately three-minute window.

The run's exact validated artifacts were subsequently downloaded and inspected.
All three public nupkg contents match those artifacts, excluding only NuGet's
repository-signature entry. This independently verifies publication despite
the historical run's red conclusion. Local artifact SHA-256:

| Package | Version | SHA-256 |
| --- | --- | --- |
| Penghou.IO.Abstractions | 0.1.0-preview.1 | b30a7d3ec9077ebe9a63d64111f1cff558b0fbd9e04c631a1e37c79d1a1efb0e |
| Penghou.IO.Protocols | 0.1.0-preview.1 | 71543f52065e2e3576ed23ea8f840c24f13a2007bcad2989352ad2aca8370724 |
| Penghou.IO.Local | 0.1.0-preview.1 | 97cd8180a14f83af276d2233df2ddd095c9519ed1b38df57916888c468a6148f |

Do not retry the older tag release or rebuild this immutable version.
The published artifacts came from the manual main run above, not the historical
tag below. Future runs keep upload and public verification in separate jobs.
Verification shares a one-hour indexing deadline and can be retried without
NuGet credentials or uploads. Workflow lint, real public-content comparison and
five checker probes pass: delayed indexing, missing index, missing download,
HTTP 500 and mismatched content. Missing availability never becomes successful
verification; content mismatches remain hard failures.

RA-5B's IO publication gate is complete in the
[canonical delivery plan](resource-abstractions-corrective-plan.md). RA-5C remains
open until Luban and Hufu record actual public-feed qualification. Provider
profiles and supported semantics are unchanged.

## Historical exact-tag candidate

The immutable v0.1.0-preview.1 tag points to
468dde33f0cda8f8f26a734abd0e512cea70d138. [Release run 37084905564](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37084905564)
passed exact-tag Windows build/tests, Linux neutral builds, package inspection
and isolated package consumption. Both Windows suites passed 73 cases with no
failures/skips. The publish job failed solely at the missing NUGET_USER guard,
before NuGet login or push. All three NuGet package endpoints remained absent
during this checkpoint; no publication is claimed.

The six validated archives were downloaded and independently passed
eng/verify-package-set.ps1. Candidate nupkg SHA-256:

| Package | Version | SHA-256 |
| --- | --- | --- |
| Penghou.IO.Abstractions | 0.1.0-preview.1 | c00a68a0d15bc3861ead0da2f32020a7fcd6379fd0501b1054a52407d8df7250 |
| Penghou.IO.Protocols | 0.1.0-preview.1 | b5ea0004e89dd69069953122c1eed054b6e74dddc846925b1e3ce8f22cbe25ff |
| Penghou.IO.Local | 0.1.0-preview.1 | da2a3b0c94b381f964c52d4a034dbd76e51a90b012b0f18594ac426b4a37ac63 |

The publishing identity is now configured and exercised by the verified manual
release above. This older tag candidate remains historical; its artifacts are
not the published content and must not be retried under the same package version.

## Manual release selection — 2026-10-03

Under RA-5B in the [canonical delivery plan](resource-abstractions-corrective-plan.md),
manual Publish to NuGet runs now accept the selected branch (normally main)
without a version/tag input. PackageVersion comes from the selected run's exact
commit; all three jobs explicitly check out github.sha. Tag runs still reject
a tag/version mismatch. Existing package-content collision checks, Windows
provider qualification, Linux neutral builds, isolated consumer proof and
same-run validated-artifact publication remain required.

The historical tag and artifact hashes above remain unchanged. A new manual
run validates and records its own commit and artifacts; the old hashes do not
attest that new run. This workflow change does not claim NuGet publication or
close RA-5B/RA-5C. See [releasing](releasing.md) for the input-free procedure.

Local workflow lint passes. Executing the actual version-resolution step with
the real MSBuild package version passes six cases: manual main, matching pushed
and manual tags, mismatched pushed and manual tags, and rejection of an ordinary
branch push. The latter three fail before emitting a release version.

## Consumer qualification

The finalized Luban working tree passes 338 tests on each framework with these
exact IO artifacts, packs with its final API namespace/baseline, and passes
isolated package smoke on both frameworks. Its final API is committed at 26f4ad943a0f4370627574c43d2a78bd2c14b54d with green CI 37085937530. The release workflow, checker and public-dependency smoke mode are committed/pushed. [Luban release handoff](../../Penghou.Luban/docs/release-handoff.md)
tracks public dependency qualification and subsequent Luban publication.

Hufu now selects exact Penghou.Luban [0.1.0-preview.1] by default; its normal
solution no longer includes Luban source. Explicit development source opt-in
remains available. The complete isolated candidate run passes 93 Biscuit, 101
core and 19 IO tests on each framework: 426 cases, no failures/skips. The snapshot
contains no IO or Luban source checkout and uses a fresh cache. This exercises
real read/list/conditional-write and current authority/revocation/evidence
behavior through the package boundary. The [JSON evidence](../../Penghou.Hufu/docs/qualification/candidate-resource-packages.json)
records all four archive identities and actual local restore sources.

Hufu's eng/Test-PublishedResourcePackages.ps1 defaults to NuGet.org-only IO/Luban
qualification; CandidateFeedPath explicitly selects separate candidate-only
evidence. Run without that option after both producers publish. The script
records public-resource-packages.json only after source checks and all six
suites pass. Zhinu remains an explicit source dependency and BiscuitSharp is the
exact qualified unpublished preview.2 artifact, not a public package claim.

Unsupported Luban v2 is rejected before authority lookup or decision recording.
Production Hufu host authentication/custody/capacity and governed mutation
start/outcome recovery remain separate gates. VFS remains deferred.

## Finish order

1. IO publication and public content verification are complete.
2. Complete or confirm Luban's separate NuGet publisher configuration.
3. Qualify Luban against fresh public IO dependencies, commit/CI-qualify its final
   reviewed API, and publish its exact version tag.
4. Switch Luban's ordinary CI from the explicit candidate feed to public IO.
5. Run Hufu's default isolated public qualification and refresh source manifests.
6. Close RA-5B/RA-5C in the corrective ledger and owner roadmaps only when those
   actual publication and public-feed checks pass.
