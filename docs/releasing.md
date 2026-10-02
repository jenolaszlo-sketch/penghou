# NuGet release process

The release is the coordinated package set `Penghou.IO.Protocols`,
`Penghou.IO.Abstractions`, and `Penghou.IO.Local`. All three use the checked-in
`PackageVersion` from `Directory.Build.props`; the initial candidate is
`0.1.0-preview.1`. The release workflow rejects a tag that does not match that
version and checks any existing NuGet.org version against the packed artifact
before publishing, so an immutable version cannot be
silently reused.

## Before the first publication

Complete the remaining publication setup before creating the release tag:

- The GitHub Actions environment named `nuget` exists. Add the `NUGET_USER`
  environment secret with the NuGet account name accepted by `NuGet/login@v1`.
- Configure NuGet.org Trusted Publishing for this repository, the
  `.github/workflows/publish.yml` workflow, and the `nuget` GitHub environment.
- Protect the `nuget` environment with the repository's release approval rule if
  one is used for package publication.

The publication workflow receives no NuGet credentials in pull request or CI
jobs. Only its validated publish job requests an OIDC token, and only after the
version tag has passed Windows tests, Linux builds for the neutral projects,
package-content checks, and a package-only restore and runtime proof.

## Publish

1. Confirm the selected version is new and all three projects have the same
   `PackageVersion`.
2. Push the matching tag, for example `v0.1.0-preview.1`, after the source and
   workflow changes have passed CI.
3. The workflow builds and tests on Windows for .NET 8 and .NET 10, builds
   Abstractions and Protocols on Linux for both frameworks, packs and audits the
   package set, then publishes Abstractions, Protocols, and Local in dependency
   order with NuGet OIDC.
4. Confirm the workflow run succeeded and the three package/version pages are
   visible on NuGet.org before updating package consumers.

The same workflow can be started manually only from an existing version tag; the
`release_tag` input must match the selected ref. If publication stops partway,
rerun the failed publish job in that same workflow run so it reuses the validated
artifact. Before pushing, the workflow compares every uncompressed package entry
with the artifact and allows NuGet.org's `.signature.p7s` repository-signature
entry. A content mismatch stops the retry. A fresh run repacks the tag, so it is
rejected if its package contents differ from an already-published version. NuGet
package versions are immutable; an artifact correction needs a new coordinated
version. The post-publish check allows a bounded indexing window before it asks
for a same-run retry.

## Package validation

`eng/verify-package-set.ps1` checks package IDs, versions, metadata, target
framework assets, README inclusion, and symbol package presence.
`eng/smoke-package-consumer.ps1` creates a temporary consumer project with only
`PackageReference` entries and a NuGet configuration whose sole package source is
the local artifact feed. It compiles and loads the identity, path, contract, and
Local provider APIs on .NET 8 and .NET 10. It does not reference repository
projects or assemblies.
