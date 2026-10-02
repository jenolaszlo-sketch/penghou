# Resource abstraction corrective delivery plan

Status: corrective implementation underway, 2026-10-02. RA-1 decisions are
selected in [ADR 0003](decisions/0003-replaceable-resource-providers.md);
the delivery ledger separates candidate qualification from publication.
This plan turns the
[architecture baseline](resource-abstractions-architecture.md) into bounded
deliveries; it does not select unresolved public APIs by implication.

## Success condition

A domain consumer uses logical resource capabilities without importing Local,
Hufu or OS paths. IO.Abstractions contains contracts and small value invariants.
Local supplies qualified physical behavior. An optional authority integration
can govern a conforming provider, including discovered resources and the final
mutation boundary. The existing safety/consistency guarantees survive migration.
Completion requires all of the following:
1. Correct the Penghou abstraction repository and move any Luban-owned semantic
   implementation found there into Luban. Diff/merge already in Luban stays there.
2. Set up working CI for build, meaningful tests, contract/API checks and package creation.
3. Set up and exercise NuGet publication of the corrected IO.Abstractions and IO.Local
   release through a controlled release workflow. IO.Protocols releases with
   them as the shared versioned codec/profile dependency selected in ADR 0003.
4. Migrate Luban and Hufu to the published package versions, with real runtime
   integration and passing consumer checks; sibling source references are not the final result.

Local package-feed smoke tests are an intermediate gate. Success is corrected code,
working CI/release delivery, and both real consumers using the published packages.

The first proof is a bounded read/list plus one conditional existing-file write
through the corrected seams. General file operations and a production virtual
filesystem are not prerequisites. A provider that cannot meet required semantics
must return an explicit unsupported outcome.

## Reading order and authority of documents

1. [Sol handoff](resource-abstractions-sol-handoff.md): scope and exact resume task.
2. [Architecture baseline](resource-abstractions-architecture.md): intended direction,
   review gates and deferred VFS ownership.
3. [Original user specification](resource-abstractions-proposal.md): original intent.
4. [Public-type inventory](resource-abstractions-inventory.md): all 53 current public
   types, including nested cases; update it if the working tree changes.
5. Current [read](local-reader-profile.md), [patch](local-patch-profile.md),
   [identity](canonical-request-identity.md) and
   [preview/commit](preview-commit-contract.md) profiles: regression obligations.
6. [Luban plan](../../Penghou.Luban/docs/implementation-plan.md) and
   [Hufu roadmap](../../Penghou.Hufu/docs/roadmap.md): integration responsibilities.

The older [implementation plan](implementation-plan.md) records delivered slices.
Its old order and codec placement are not the corrective work queue. Keep one
canonical architecture; add decision records linked from it instead of copying
competing specifications into each project.

## Delivery ledger

| Work item | Depends on | Status | Required output |
| --- | --- | --- | --- |
| RA-0 | — | Documentation complete | Reviewed source specification, architecture and public-type inventory |
| RA-1A | RA-0 | Complete | Dependency/API inventory and ADR 0003 protocol traces |
| RA-1B | RA-1A | Complete | G1-G5/G8 selected in ADR 0003; opaque versions and existing enforcement retained |
| RA-2 | RA-1B | Corrected code qualified | Contract-only assembly, Protocols relocation, Local conditional byte writer and semantic materialization in Luban; both consumer regression suites pass |
| RA-3 | RA-1B; coordinated RA-2 | Candidate packages qualified; published adoption pending | Provider-independent Luban composition; 337 tests pass on each framework, including non-Local providers and exact semantic/memory bounds |
| RA-4 | RA-1B; corrected provider seams | Candidate packages qualified; published adoption pending | Hufu.IO, retained sessions/frozen writes, real Local allow/deny/revocation and lifecycle qualification |
| RA-5A | RA-2; candidate consumer validation | Complete; local and remote CI qualified | CI and release workflows, three package candidates, artifact checks and isolated local-feed consumption |
| RA-5B | RA-5A | Pending | Working release workflow and corrected package release published on NuGet |
| RA-5C | RA-5B; RA-3/4 | Pending | Luban and Hufu consume published versions with passing integration evidence |

Delivery order: resolve decisions -> correct repository/ownership -> qualify CI
and package candidates -> publish corrected packages -> finish published-package
migration in Luban and Hufu -> verify the complete integration. Consumer changes
can be prepared against candidate packages before release; completion must prove
the real published versions. RA-3/RA-4 are not complete on sibling-source builds.

Do not mark an item complete because its interfaces compile. If a provider cannot
meet a required guarantee, return Unsupported or propose a separately versioned
profile; do not weaken existing consumer guarantees.

## Current evidence and external release gate

- Remote [CI run 37030577295](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37030577295)
  passes for commit `89858631e4f9220bf40506cff34bb9b6ee8912dd`: both Windows
  regression jobs, both Linux neutral builds, and package closure verification,
  isolated consumption and artifact upload. RA-5A is complete. This CI version is
  `0.1.0-preview.1-ci.37030577295.1`, not the published release candidate version.
- The migration is installed in the original IO, Luban and Hufu repositories;
  the original checkouts reproduce the counts below. The AI tool host sample
  builds without warnings on both frameworks against candidate packages.

- IO: 73/73 tests pass with no skips on Windows, both net8.0 and net10.0.
- Three candidate packages pack and pass metadata/dependency/content checks;
  an isolated package-only consumer restores and runs on both frameworks.
- Luban: 337/337 tests pass on each framework against candidate packages;
  neutral source imports no Local/OS API and an injected test provider exercises
  read, preview and single/batch execution with opaque version tokens.
- Hufu: 19/19 IO integration, 95/95 existing Cedar/SQLite/Zhinu/Luban and
  80/80 Biscuit tests pass on each framework. IO integration uses real Local
  pagination, excluded-child filtering, conditional writes and final revocation
  checks. The IO adapter composes the host-selected async authority authorizer,
  preserving concurrent Biscuit integration and exact request/evidence checks.
  A test-only journal exercises Local's locked start/outcome hook;
  production Hufu mutation-start transactions remain separate qualification.
- Consumer IO references are exact `[0.1.0-preview.1]` package ranges by default.
  Candidate-feed IO references are package assets, not sibling source projects.
  Capture catalogue v2 explicitly binds the changed retained-content contract;
  its golden vectors were independently regenerated and tested.
- GitHub authentication works and the repository `nuget` environment exists.
  `NUGET_USER` and the matching NuGet.org Trusted Publishing policy are still
  required before publication. The policy tuple is owner `jenolaszlo-sketch`,
  repository `penghou`, workflow `publish.yml`, environment `nuget`.
- No corrected package version has been published. RA-5B and RA-5C remain open;
  candidate-feed or source builds must not be reported as published adoption.

## Luban leaf checkpoint — 2026-10-03

Luban's initial D1–D6 feature baseline is implemented and qualified independently
of Hufu and workflow engines. Its [completion ledger](../../Penghou.Luban/docs/leaf-completion.md)
records 337 tests per framework, opt-in language v2, exact file-change/application
profiles and green [CI run 37039062513](https://github.com/jenolaszlo-sketch/penghou-luban/actions/runs/37039062513)
for revision `4bbbd84bbaaf7a3376f85fc1e59da3fd13901693`. CI also verifies Linux
neutral compilation and an isolated Luban package-only consumer. The core's only
package dependencies are the corrected IO.Abstractions and IO.Protocols candidates.

The [consumer impact guide](../../Penghou.Luban/docs/consumer-impact.md) records
the package graph and migration owners. Hufu's counts above are the earlier IO
migration checkpoint, not new qualification against this Luban revision. Hufu
consumer qualification is next; no Hufu implementation was changed in this leaf
completion. RA-5B/RA-5C still require actual published IO package versions. A green
Luban package candidate does not satisfy public-feed adoption.

## RA-1A — Map consumers and work through the protocol

Owner: Sol, starting in the Penghou repository. Read-only source investigation
plus design documentation; no public API or production code edits in this first slice.

- Map references to WindowsWorkspacePath, ResourceRequestIdentity, HostInvocation,
  IResourceAuthorizer, IResourceMutationJournal and TextPatch/FilePatchRequest in
  Penghou, Luban and Hufu. Record assembly/public-signature dependencies, including
  batch executors and tests, rather than only constructor call sites.
- Distinguish semantic admission, neutral operation identity, concrete access checks,
  version/precondition checks and durable mutation starts. Identify who observes
  each event and which party owns every piece of mutable data.
- Trace these concrete cases end to end: permitted read; initial denial; list with
  one excluded child; revocation between pages; move source/destination roles;
  conditional write with a version conflict; revocation after preparation;
  cancellation/response loss after possible mutation; two simultaneous workspaces.
- Demonstrate how plain host use and a Hufu-governed host compose the same provider
  without fabricated workflow identities or an accidental permissive default.
- Propose a minimal neutral enforcement cooperation protocol. Compare a checked
  provider hook with a prepared-operation protocol where needed. Do not turn it
  into generic middleware, arbitrary callbacks or a new workflow runtime.

Exit: a concrete interaction/type sketch for each trace, a dependency closure map,
and unresolved tradeoffs with recommendations. Documentation sketches are sufficient
here; do not create shipping stub APIs to make the design appear complete.

## RA-1B — Record decisions before moving types

Owner: Sol; use the architecture's existing gate IDs. Add one or more numbered ADRs
under docs/decisions after checking existing numbers.

| Gate | Required decision | Guardrail |
| --- | --- | --- |
| G1 | How the authority boundary checks discovered children and prepared mutations | Initial deny has zero delegate calls; later deny protects that child/mutation |
| G2 | Required resource context versus host-authenticated execution envelope; immutable snapshot ownership | No ambient identity, forged authority, hash/use mismatch or duplicate codec |
| G3 | Canonical logical paths, equality, resource/view/version identity and lifetime | Do not silently reinterpret old Windows identity vectors |
| G4 | Luban materialization plus conditional provider persistence, or justified neutral byte edits | No unconditional fallback, reverse Local->Luban dependency or lost start ordering |
| G5 | Narrow capabilities, supported guarantee profiles, pagination/bounds and explicit outcomes | No universal filesystem promise; honest authorized-view completeness |
| G8 | Codec/profile owner, API/assembly migration, compatibility and release order | Keep the two IO package boundaries; add another only for a demonstrated need |

Resolve tiny result factories/value validation as allowed contract behavior.
Schedule HTTP contract separation explicitly; do not implement HTTP redesign.
G6/G7 describe future simulation/application and need only enough compatibility
analysis to ensure the immediate design does not prevent them.

Recommended first design bias: explicit injected instances, stable logical paths,
a single versioned codec outside Abstractions, Luban-owned text materialization,
conditional persistence, and policy-neutral enforcement hooks. These are
recommendations to test against the traces, not permission to weaken a current profile.

Exit: decisions cite alternatives and consequences, public type/package closure,
migration order, and executable acceptance scenarios. Update the canonical
architecture with the decisions and remaining limits. Ordinary design choices
can be resolved from the specification; ask the user only for a material product
tradeoff the specification cannot settle.

## RA-2 — Restore the contract assembly

Owner: Penghou. Coordinate required consumer changes under RA-3/4.

- Move any Luban-owned diff/merge/text transformation or patch materialization found
  in the abstraction repository into Luban. Record an inventory item as already
  correctly placed where applicable; do not manufacture moves for already-owned code.
- Relocate WindowsWorkspacePath behavior into the selected Local/profile owner.
- Relocate ResourceRequestIdentity implementation into the decided support/profile
  owner, retaining one implementation and the applicable golden vectors.
- Retain interfaces, neutral records/enums, bounds, preconditions and outcome types.
  Apply the recorded context/path/patch decisions with explicit version migration.
- Keep all physical mapping/native access in Local. Local must not reference Luban
  or Hufu. Do not introduce substantive helpers in Abstractions under a new name.
- Add architecture checks for assembly references, transitive dependencies and
  public signature closure. Confirm a contracts-only consumer can compile without
  Local/Hufu/Luban, even when tests also exercise all adapters.

Evidence: identity and path compatibility tests plus the real Local regression
suite; unsupported old/new combinations reject explicitly. Document assembly/type
compatibility rather than silently break consumers.

## RA-3 — Make Luban consume capabilities

Owner: Luban integration. Change only files needed for this migration.

- Inject reader/directory/metadata capabilities into FileEffectRuntime,
  LanguageRuntime and PreviewRuntime. Isolate WorkspaceReference physical roots
  and Local configuration in composition.
- Include SinglePatchExecutor, BatchPatchExecutor and their public admission
  requests in the dependency closure. Hiding new LocalWorkspaceReader behind a
  factory in the same neutral layer is not provider independence.
- Keep diff/merge/materialization in Changes. Replace the current locked patch path
  only after the conditional persistence/start protocol is qualified.
- Preserve semantic preflight/admission, immutable parent-child request bindings,
  bounded traversal/output, candidate exclusions and release checks.
- Run read and capture behavior with a minimal test-only provider and real Local.
  Keep current capture-only WhatIf read-only and CanCommit=false.
- Place any convenience Local composition in a deliberate integration boundary;
  record the version/compatibility effect before moving public types.

Evidence: neutral assembly/API dependency checks, two concurrent workspace tests,
read/preview regression suites and single/batch execution tests. No new production
virtual provider or language syntax.

## RA-4 — Prove optional Hufu interception

Owner: Hufu integration, using the neutral cooperation protocol selected in RA-1.

- Introduce the optional Hufu.IO boundary only with real tested behavior. It depends
  on Hufu and neutral IO contracts; keep Hufu.Luban semantic admission separate.
- Prove allow read/write, initial denial without calling the provider, denial of
  discovered child resources before protected inspection/disclosure, current
  revocation between calls/pages, exact target/payload/precondition binding and
  failed mandatory decision recording.
- Exercise the final mutation start, including preparation-to-start revocation
  and uncertainty after possible completion. Preserve the current advertised
  block-new-starts semantics; draining in-flight work is a separate guarantee.
- Prove another explicit test authority implementation can use the same neutral
  seam. Test fixtures do not become a shipping allow-all default.
- Record unsupported provider profiles honestly; a decorator cannot manufacture
  consistency or interception guarantees an inner provider does not implement.

Evidence: boundary tests plus the relevant real Local/Hufu integration and
start/outcome regression suites. Mark read-only and mutation qualification
separately if delivered in separate slices; partial progress is not RA-4 completion.

## RA-5 — CI, NuGet publication and published-package adoption

Owner: Penghou with Luban/Hufu consumers.

- Run reusable contract tests for supported capabilities; retain native provider
  tests for path/link/namespace/locking behavior that a test double cannot prove.
- Record target framework, OS/filesystem, command, pass/fail/skip counts and
  unsupported guarantees. Historical test counts are not current evidence.
- Configure both existing IO projects for packaging with deliberate metadata and
  dependency versions. Audit package contents and public API compatibility.
- Build a small consumer in an isolated directory using only the local package
  feed and PackageReference entries, with neither sibling ProjectReferences nor
  accidental access to source outputs.
- Configure PR/main CI to restore, build, run supported .NET 8/.NET 10 tests,
  enforce architecture/API checks and pack validated artifacts. Qualify the Windows
  native profile on Windows; report platform skips honestly.
- Configure a controlled release workflow with explicit versions, required checks,
  package/symbol artifacts, least-privilege publication access and credentials kept
  outside source. Untrusted PRs must not receive publication credentials.
- Exercise publication and verify package identity/version/dependencies from NuGet.
  Record the release and CI run evidence; enabling IsPackable is not publication.
- Replace IO sibling ProjectReferences in the normal Luban/Hufu consumer builds
  with pinned PackageReferences to the published versions. Keep any development
  source integration explicitly opt-in, not a hidden normal-build dependency.
- Build/test both consumers without the Penghou source checkout and prove actual
  read/list/conditional-write and Hufu allow/deny/revocation integration through
  the package boundary. Do not count a toy consumer as both production consumers.
- Record CI/release runs, published versions, restore sources, consumer test results
  and remaining limitations. Missing registry permissions/credentials or an
  unavailable upstream dependency is an explicit blocker, not a completed gate.

Baseline commands, to run during authorized implementation, from each repository:

```powershell
# Penghou
dotnet test Penghou.IO.slnx -c Release -f net8.0
dotnet test Penghou.IO.slnx -c Release -f net10.0

# Penghou.Luban
dotnet test tests/Penghou.Luban.Tests/Penghou.Luban.Tests.csproj -c Release -f net8.0
dotnet test tests/Penghou.Luban.Tests/Penghou.Luban.Tests.csproj -c Release -f net10.0
```

Select affected Hufu test projects after checking their dependencies and native
requirements. Do not treat missing Cedar/native dependencies or platform skips
as passing qualification. No builds were run for this documentation handoff.

## Scope and handoff discipline

Allowed future correction scope: Penghou IO contracts/provider/codec/tests/docs,
the necessary Luban integration, Hufu integration, CI and NuGet release setup,
publication qualification, and published-package adoption. Read other repositories for
consumer analysis; do not migrate them as part of RA.

Deferred: VFS-1 through VFS-10, broader mutation families, HTTP/process redesign,
Baize routing extraction, SQLite restructuring and ecosystem-wide migrations.
Keep their roadmap references; do not implement them to justify an abstraction.

Preserve all pre-existing work. Inspect status before each repo edit; use narrow
patches, never blanket restore/reset. Hufu currently has an untracked working tree:
untracked does not mean disposable. Do not include unrelated work in release commits.
NuGet publication is part of the delivery goal after package and CI qualification;
required external access must be available before release can be marked complete.

Each delivery report records: RA ID, decisions/profile versions, changed files,
meaningful verification and unverified claims, remaining blockers, and the next
exact work item. Update this ledger and the linked owner roadmap together.
