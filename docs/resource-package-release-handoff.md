# Resource package release checkpoint

Updated 2026-10-03. RA-5B publication and RA-5C public-feed adoption remain open.

## Exact IO release

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

Configure NUGET_USER in this repository's nuget environment and a NuGet trusted
publisher for owner jenolaszlo-sketch, repository penghou, workflow publish.yml,
environment nuget. Retry only the failed job:
gh run rerun 37084905564 --failed --repo jenolaszlo-sketch/penghou.
This reuses the validated artifacts. Do not move the release tag or substitute a
locally rebuilt archive.

## Consumer qualification

The finalized Luban working tree passes 338 tests on each framework with these
exact IO artifacts, packs with its final API namespace/baseline, and passes
isolated package smoke on both frameworks. Its new release workflow and checker
are committed/pushed; final API changes are being preserved in the parallel
Luban review. [Luban release handoff](../../Penghou.Luban/docs/release-handoff.md)
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

1. Complete both repositories' NuGet publisher configurations.
2. Retry IO publication and verify all three downloadable package contents.
3. Qualify Luban against fresh public IO dependencies, commit/CI-qualify its final
   reviewed API, and publish its exact version tag.
4. Switch Luban's ordinary CI from the explicit candidate feed to public IO.
5. Run Hufu's default isolated public qualification and refresh source manifests.
6. Close RA-5B/RA-5C in the corrective ledger and owner roadmaps only when those
   actual publication and public-feed checks pass.
