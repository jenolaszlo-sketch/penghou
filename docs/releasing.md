# NuGet release process

The current **Publish to NuGet** workflow publishes only
`Penghou.Workflow.Abstractions`. Its initial package version is
`0.1.0-preview.2`, read from that project's checked-in `PackageVersion`.
Manual publication has no version or package-set inputs.

The existing `Penghou.IO.Protocols`, `Penghou.IO.Abstractions`, and
`Penghou.IO.Local` packages at `0.1.0-preview.1` are already published and
immutable. The workflow does not pack, compare, or push those IO packages.
Windows solution builds/tests and their ordinary CI package smoke checks remain
regressions only. The IO package-set validator and consumer scripts keep their
IO profile by default; the publishing workflow selects the explicit `Workflow`
profile, which accepts exactly the workflow package and requires no dependencies.

## Before publication

Complete the existing publication setup:

- The GitHub Actions environment named `nuget` exists. Add `NUGET_USER` as a
  repository or environment secret containing the NuGet account name accepted
  by `NuGet/login@v1`.
- Configure NuGet.org Trusted Publishing for this repository, the
  `.github/workflows/publish.yml` workflow, and the `nuget` GitHub environment.
- Protect the `nuget` environment with the repository's release approval rule
  if one is used for package publication.

Pull request, CI, validation, and verification jobs receive no NuGet
credentials. Only the publish job requests an OIDC token, after Windows
regression tests, Linux workflow build/tests, package-content checks, and a
package-only consumer proof have passed.

## Publish

1. Confirm `Penghou.Workflow.Abstractions` has the intended checked-in version
   and its source and workflow changes have passed CI.
2. In GitHub Actions, select **Publish to NuGet**, choose **Run workflow** on
   `main`, and run it. There are no inputs. The workflow reads the version from
   that exact commit. A manual run from another branch is rejected.
3. The workflow tests the Windows solution on .NET 8 and .NET 10, builds and
   tests the workflow contracts on Linux for both frameworks, then packs,
   audits, and consumes only `Penghou.Workflow.Abstractions` before publishing
   it with NuGet OIDC.
4. Confirm the run succeeded and the package page is visible on NuGet.org
   before updating consumers.

Publication is manual from `main` only. It requires no version/tag inputs and
does not create or move tags. Existing IO release tags remain historical refs;
pushing a tag does not start this workflow-only release.

If publication stops partway, rerun the failed publish job in that same
workflow run so it reuses the validated artifact. Before pushing, the workflow
compares every uncompressed package entry with the artifact and allows
NuGet.org's `.signature.p7s` repository-signature entry. A content mismatch
stops the retry. A fresh run repacks its selected commit and is rejected if
that package version already has different content. NuGet package versions are
immutable; a correction needs a new package version. After package and symbol
uploads, a separate verification job downloads the same validated artifact and
compares the publicly downloadable package contents. It allows one shared
indexing deadline for the package. Upload success and public availability are
separate evidence. If indexing or verification times out, rerun only that
verification job; it has no NuGet login, credentials, or upload steps.

## Package validation

`eng/verify-package-set.ps1` and `eng/smoke-package-consumer.ps1` default to the
existing IO profile. CI and publication pass `-Profile Workflow` with a
separate artifact directory. The profile validates exactly
`Penghou.Workflow.Abstractions`, verifies its .NET 8 and .NET 10 assets and
dependency-free metadata, then restores a temporary consumer from a local
artifact-only feed. That consumer implements `IExecutionAuthorizer`, checks
that the denied response matches its request, and proves the denied-only
consumer does not invoke protected work. Its package graph contains only the
exact workflow package; it has no repository project, Zhinu, or Hufu reference.
The denied-only probe checks package consumption and response binding; runtime
fencing, evidence requirements, approval durability, and actual resource
authorization remain host/runtime responsibilities.

`eng/check-nuget-version.ps1` also defaults to the existing IO package set;
publication passes `-Profile Workflow` so immutability checks inspect only the
new workflow package. No existing IO preview package is republished as part of
this release.
