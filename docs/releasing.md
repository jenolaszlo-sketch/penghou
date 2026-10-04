# NuGet release process

The current input-free **Publish transport abstractions to NuGet** workflow
publishes `Penghou.Model.Abstractions` and `Penghou.Http.Abstractions`, initially
`0.1.0-preview.1`, read from both projects' coordinated checked-in versions.
Only manual runs from `main` are accepted. No version or package-selection input
is required. Existing IO `0.1.0-preview.1` and workflow `0.1.0-preview.2` are
published immutable history and are not packed/pushed by this release workflow.

The existing NuGet OIDC configuration uses repository
`jenolaszlo-sketch/penghou`, workflow `.github/workflows/publish.yml`, environment
`nuget` and secret `NUGET_USER`. This delivery reuses that workflow identity.
The NuGet publisher's allowed package scope must include both new IDs; if the
existing policy is scoped narrowly, extend it on NuGet.org before running release.
No credentials are exposed to build/test/consumer/verification jobs.

## Publish

1. Commit and push the qualified changes to `main`, and verify CI passes.
2. Run **Publish transport abstractions to NuGet** on `main`. The workflow binds
   to the selected exact commit, resolves equal checked-in versions, tests the
   Windows solution and both neutral packages on Windows/Linux/macOS with
   .NET 8/.NET 10, then packs only the two transport packages.
3. The Transport profile requires exact IDs/filenames, DLL/XML assets, symbols,
   README, repository metadata, zero runtime dependencies and isolated exact
   package-only consumers. Only after those checks can the publish job request
   an OIDC token and push the validated artifacts.
4. Confirm the verification job succeeds after public indexing. It compares
   every ZIP entry against the same artifact, excluding NuGet's repository
   signature. Only then update Baize to the exact published versions. Hufu's
   optional adapters follow Baize's separate migration/coverage qualification.

Packages and symbols are pushed separately: nupkg uses `--no-symbols`, then
each snupkg is sent once. Duplicate-safe retries never replace immutable content.
Rerun a failed publish job in the same run to reuse validated artifacts. A
different existing package payload fails comparison and requires a new version.
If indexing times out, rerun only the verification job; it never uploads/logs in.
The shared indexing deadline covers both packages.

## Profile compatibility

`eng/verify-package-set.ps1`, `eng/smoke-package-consumer.ps1` and
`eng/check-nuget-version.ps1` retain IO as the default and Workflow as an explicit
profile. New `-Profile Transport` accepts exactly the two neutral transports.
CI retains IO/workflow build, test, packing and isolated-consumer regressions.
The release targets the current transport delivery, so it does not rerun an
already-published workflow package upload.

The [release checkpoint](model-http-release-handoff.md) records evidence and open
publication/adoption gates. Neutral conformance fixtures prove contract usage,
not real Baize coverage, credential custody, authority fencing or OS isolation.
