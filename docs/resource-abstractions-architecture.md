# Resource abstractions: architecture baseline and review

Delivery companion: [ordered corrective plan](resource-abstractions-corrective-plan.md)
and [Sol handoff](resource-abstractions-sol-handoff.md). They operationalize this
baseline; the delivery plan records current implementation and release evidence.

Status: intended architecture recorded 2026-10-02 at the user's request.
The original review findings below are retained for traceability. Corrective API
decisions are selected in [ADR 0003](decisions/0003-replaceable-resource-providers.md):
explicit provider sessions, preserved host bindings, Luban materialization,
conditional byte writes and a three-package release. It resolves G1-G5/G8 for
this slice. VFS gates remain deferred. The corrective plan governs qualification,
publication and actual consumer adoption; selected APIs alone do not complete it.

## Source of truth and reading order

1. This document defines the direction, scope, review findings and cross-project gates.
2. The [original user specification](resource-abstractions-proposal.md) preserves
   all 49 sections, including the VFS roadmap. Read it with the clarifications here.
3. The [public-type inventory](resource-abstractions-inventory.md) records what
   actually exists. Do not implement moves based only on illustrative examples.
4. Each project's roadmap records its delivery responsibility and completion evidence.
5. Existing qualified profiles describe current behavior until a versioned replacement
   passes its gates. Historical completion does not mean the new boundary is complete.

This direction refines the original concrete-resource design in
[ADR 0001](decisions/0001-shared-resource-boundary.md) and preserves the
admission guarantees in [ADR 0002](decisions/0002-preview-resolution-commit-barrier.md).
Older descriptions of a codec inside Abstractions or provider-internal authorization
describe today's implementation; they are not instructions to perpetuate that packaging.
References between repositories assume sibling checkouts. Future released documents
must use pinned repository/release links when a sibling checkout is unavailable.

## Assessment

The specification makes sense. Replaceable capability contracts, explicit workspace
instances and optional authority composition serve AI tool execution well. Hufu can
govern resource access without becoming a dependency of Luban or Local, and a future
virtual provider can exercise the same domain operations.

The correction is narrower than parts of the proposal imply: diff and merge already
live in Luban. Abstractions currently contains a Windows path implementation and a
Windows-dependent canonical request codec, as well as filesystem and HTTP contracts.
Luban still constructs Local readers and patchers and exposes physical workspace roots.
There is no standalone Hufu.IO resource decorator today; the Hufu.Luban language
authorizer is a different integration boundary.

## Intended ownership

| Responsibility | Owner |
| --- | --- |
| Bounded logical file/directory capabilities and neutral supporting values | Penghou.IO.Abstractions |
| OS mapping, links, handles, native consistency and physical persistence | Penghou.IO.Local |
| Diff, merge, text patch interpretation, language/IR, semantic effects and preview | Penghou.Luban |
| Authority evaluation, grants, revocation, approvals and authority evidence | Penghou.Hufu |
| Resource-to-authority mapping and interception | Optional Penghou.Hufu.IO integration, or another security integration |
| Provider choice, authenticated execution context, workspace binding and composition | Trusted host |
| Workflow declaration/control and declared effects | Fuwen |
| Durable starts, receipts, fencing and recovery | Zhinu or an equivalent host runtime |
| Snapshot, overlay, filtered and other virtual resource state | Future provider layer |

Dependency target: Luban's neutral runtime -> IO contracts; Local -> IO contracts;
Hufu.IO -> Hufu + IO contracts. The host composes Luban -> authorized resource
boundary -> selected provider. Hufu.Luban may still adapt semantic plan admission;
a resource decorator does not replace that language-level obligation.

Hufu is the Penghou authority implementation, not a mandatory dependency for every
consumer. A host may select another security mechanism or explicitly use resources
without Hufu. Such composition does not claim Hufu governance. AI-facing governed
hosts must not accidentally expose the undecorated provider or default to permit.
A narrow interface reduces available operations; possession of it alone does not
authenticate the caller or authorize every resource.

## Scope of the immediate correction

- Inventory every public abstraction type and verify its dependency closure.
- Remove substantive implementation from the contract assembly.
- Separate neutral logical identity from OS paths and Local configuration.
- Inject resource capabilities into neutral Luban paths and move Local composition
  into a host or integration boundary.
- Specify and qualify Hufu interception without losing existing resource checks.
- Preserve bounds, immutable request binding, preconditions, exact admission,
  outcome uncertainty and recovery requirements.
- Add architecture and focused conformance tests with test-only spies/providers.
- Set up CI and controlled NuGet publication for IO.Abstractions and IO.Local,
  publish qualified corrected versions, and migrate the real Luban/Hufu consumers
  to those versions. Publication is a distinct delivery gate, not deferred scope.

No production VFS, mounts, transactions, HTTP/process redesign, Baize extraction,
SQLite split or general package migration is part of this correction. Existing
HTTP contracts require an explicit migration/deprecation plan; recording their
future owner does not authorize implementing a new HTTP stack.

## Design gates to resolve before changing contracts

### RA-G1: interception of discovered resources and the physical commit boundary

A check-before-delegate decorator is sufficient only when the request completely
describes every protected operation the provider will perform. Today's listing
discovers candidates inside Local and checks them before metadata/type inspection.
Moves have source/destination rights; path traversal probes also have checks.
The patcher rechecks authority and records a start at its locked-object boundary.
Replacing these with one outer check would weaken the existing profile.

Choose a neutral cooperation protocol for these boundaries. Options include a
provider-neutral enforcement hook or a prepared-operation/start protocol; the
specific API is unresolved. This is an enforcement mechanism, not Hufu policy
inside Local. All governed providers must conform; an arbitrary filesystem
implementation is not automatically governable.

An initial denial must invoke the wrapped provider zero times. A later denial
during an allowed list must prevent the denied child's protected metadata/content
access and disclosure; it cannot mean that the containing list was never invoked.
A denial at a mutation start must prevent mutation even if authorized preparation
already happened. Keep these three assertions distinct in tests.

Test revocation between calls and specify its in-flight semantics. Blocking new
starts is different from cancelling/draining already admitted work. A second
policy lookup does not itself serialize revocation with commit. Preserve the
[current mutation profile](local-patch-profile.md) until a replacement proves
its start ordering and ambiguous-outcome behavior.

### RA-G2: neutral request context and immutable operation identity

HostInvocation is Hufu-independent, but mandatory EffectId/AttemptId/scope fields
couple every filesystem caller to today's execution model. Decide which values
belong to neutral resource requests and which belong to a trusted host-bound
invocation/enforcement context. Plain library use must not fabricate workflow
IDs, while a governed request must retain its exact authenticated parent binding.
No blank IDs, ambient identities or forged records may bypass checks.

Freeze all mutable buffers/collections before authorization and use the identical
snapshot for execution, hashing and journaling. ReadOnlyMemory is not proof of
immutable backing storage. Results also need explicit ownership/lifetime rules.

ResourceRequestIdentity is substantive encoding/hashing behavior. Move its
implementation outside Abstractions into an explicitly owned support/profile
implementation; do not duplicate codecs in each caller. Preserve existing vectors
and version the change from Windows-normalized identities. The destination and
compatibility mechanism remain open; do not create a generic utility package by habit.

### RA-G3: logical paths, resource identity and workspace lifetime

The proposal uses /src/X.cs; today's API uses src/X.cs with an empty root.
Choose one canonical representation and define root, segment validation, Unicode,
case/equality and comparison semantics. Physical reserved names, drive mapping and
link policies belong to Local. Providers must never silently conflate logical
names, and the authority matcher must use the same identity semantics as execution.

Separate logical path, workspace/view identity, provider instance/incarnation,
resource identity and opaque resource version. Current read versions are content
hashes, not proof of stable native object identity. Define whether a precondition
compares content, object generation or both; account for delete/recreate and rename.
Bind continuations and approvals to the appropriate workspace/view and query.

Specify workspace scope, thread safety, lifetime, cancellation/disposal, cursor
expiration and owned-versus-borrowed provider dependencies. No global current
workspace or implicit current directory.

### RA-G4: patch ownership and conditional persistence

Diff/merge and pure patch materialization already belong to Luban. The IO contract
currently exposes UTF-8 TextPatch/FilePatchRequest, and Local applies those edits
while holding its version-checked handle. Moving that method mechanically would
break the existing check/compute/write relationship.

Recommended target: Luban computes desired bounded bytes; a provider performs a
conditional resource write against the captured version. Qualify version checking,
target binding, proposed-content identity and start/outcome evidence together.
If a neutral byte-edit capability remains necessary, explicitly justify and name
its resource semantics separately from Luban text patches. No reverse dependency
from Local to Luban, arbitrary transform callback or unconditional write fallback.

Treat mutation semantics as capabilities: create-only, replace-if-version,
delete-if-version, atomic destination publication, crash durability and multi-resource
atomicity are different guarantees. Keep current Unsupported and AmbiguousOutcome
behavior. Cancellation or response loss after a possible commit is not NoMutation
and must not trigger blind retry.

### RA-G5: capability and conformance contract

Specify required and optional capabilities plus versioned guarantee profiles,
including path behavior, version/precondition semantics, consistency and limits.
A capability report is descriptive, not authority. Unsupported required semantics
must fail explicitly before mutation; stale capability discovery cannot promise success.

Narrow file, directory and metadata ports where real consumers benefit. Define
enumeration ordering/progress, duplicate handling, continuation validity and whether
completion refers to an authorized view, live scan or stable snapshot. Denied entries,
counts and errors must not reveal excluded resources. Effective limits cannot exceed
caller/host bounds; define aggregate quotas as well as per-call limits and deadlines.

Tiny value invariants, named result factories and enum definitions are compatible
with a contract assembly. The prohibition targets algorithms and implementation
helpers, not every property getter or factory. No raw stream is required.

Conformance gates include allowed/denied reads and writes, zero delegate calls on
initial denial, revoked later calls, candidate exclusion, exact immutable request
binding, conflicting versions, unsupported guarantees, cancellation/uncertain
completion, two concurrent workspaces and both supported target frameworks.
Architecture checks cover transitive assembly dependencies and neutral public API
signatures, not only namespace naming. A test-only provider/spy is sufficient now;
it does not count as the future production in-memory provider.

### RA-G6: WhatIf modes and trustworthy evidence

Keep today's capture-only WhatIf read-only with CanCommit=false. Future overlay
WhatIf is a separately named/versioned execution mode: it dispatches normal writes
to disposable virtual state, never to the real destination. Do not reinterpret old
WhatIf entry points or remove their existing no-mutation tests.

Enforcement mode uses proposed authority. Observation mode records would-deny
decisions but may continue only within a separately authorized simulation envelope.
That outer envelope still governs real base reads, sensitive data and every other
effect channel. A disposable overlay does not make an unauthorized read harmless.
Reject/block unsupported real HTTP/process/database/MCP effects, or supply explicit
simulation providers; never pass them through implicitly.

Record attempts at the outer execution/enforcement boundary so denied operations
are visible even though they never reach VFS. Record actual completion and resource
deltas separately. Include run/node/attempt/request/view identities, policy/profile
versions, mode, decision and outcome with bounded/redacted data. Required evidence
failure blocks the protected operation where the profile requires it. Logs are
not authority; authoritative decision/start receipts retain their existing owner.
A provider-local journal alone cannot record initial denied calls.

Observed behavior is evidence about this execution and these inputs. Unexecuted
branches, model nondeterminism, clock/randomness and external dependencies prevent
it from proving every future run. Report unsupported effects, truncation and coverage.
Snapshots of filesystem data alone do not make a whole workflow deterministic.

### RA-G7: snapshots, overlay identity and approved application

A snapshot must be a genuinely consistent provider-supported or fully materialized
view, or explicitly report a weaker consistency level. A live base with lazy caching
is not automatically a point-in-time snapshot. Define overlay quotas, tombstones,
read-your-writes, parent-directory behavior and a frozen base before application.
Treat move intent separately from identical final bytes; a state comparison cannot
always reconstruct the operations that occurred.

Applying approved changes is a fresh governed execution. Freeze a bounded delta
with base/view/provider profile, original versions, exact output bytes/hashes,
targets and operation order. Bind approval to that exact delta; recheck real
authority and preconditions and obtain fresh durable starts. Simulation permissions
or success receipts never become real write authority. Define conflicts, partial
application, uncertain outcomes, resumability and compensation without claiming a
global transaction. Relevant read dependencies may also need revalidation.

### RA-G8: packaging and migration

Keep the existing IO package pair. Define API/assembly compatibility, contract and
profile versions, reference changes, package-only consumer verification and release
order across IO, Luban and Hufu integration. Moving public types may need forwarding,
adapters or an explicit preview breaking change. Preserve golden identity vectors.
The correction is not complete merely because both projects pack successfully.

## Cross-project delivery and acceptance

RA-0 is this review and public-type inventory (documentation complete).
All RA-1 through RA-5 implementation work below is pending.

| ID | Work | Owner | Completion evidence |
| --- | --- | --- | --- |
| RA-1 | Resolve G1-G5 and G8; freeze contracts, ownership and compatibility | IO + Luban + Hufu reviewers | Recorded API/profile decisions and executable conformance vectors |
| RA-2 | Move implementation out of Abstractions; separate Local and codec/profile behavior | Penghou | Contract-only dependency closure; unchanged qualified behavior or versioned replacement |
| RA-3 | Inject narrow resource capabilities; isolate physical roots/Local composition | Luban + host | Neutral API/assembly tests and read/preview/executor regressions |
| RA-4 | Optional Hufu.IO enforcement composition | Hufu + IO provider | Allow/deny/revocation, discovered-resource and commit-boundary tests under G1 |
| RA-5A/B/C | Provider conformance, CI, NuGet publication and real package adoption | IO + Luban + Hufu | Passing CI, published versions and Luban/Hufu restores/builds/integration tests without IO source siblings |

The success condition is corrected repository ownership/code, working CI and NuGet
publishing, followed by Luban and Hufu consuming the published packages. Local-feed
qualification is intermediate evidence; design completion is not the finish line.

RA-1 precedes implementation that depends on its decisions. Work behind independent
gates can proceed in parallel later. Existing governed mutation/start work remains
required but must converge on these boundaries before new coupling is added.

## Deferred VFS roadmap

These IDs are cross-project milestones, not package versions or current release gates.
Detailed intent remains in original-spec sections 25-48.

| ID | Delivery | Primary owner / dependency |
| --- | --- | --- |
| VFS-1 | Real bounded in-memory provider and common conformance suite | Penghou providers; after RA contracts |
| VFS-2 | Immutable, consistently identified snapshot provider | Penghou; explicit consistency/capture guarantees |
| VFS-3 | Writable overlay with bounded state, tombstones and frozen base | Penghou; VFS-1/2 and mutation semantics |
| VFS-4 | Attempt/decision/outcome evidence plus resulting resource delta | Hufu/host interception + providers; optionally project evidence through Hongxian |
| VFS-5 | Normal workflow execution in an isolated snapshot/overlay environment | Host + Luban + Fuwen/Zhinu adapters; G6 and VFS-2/3/4 |
| VFS-6 | Declared-versus-observed effect/authority analysis | Fuwen declarations + Hufu decisions + host evidence; explicit coverage |
| VFS-7 | Apply an approved immutable delta to a real provider | Luban/host + Hufu + Zhinu + provider; G7 |
| VFS-8 | Filtered resource views | Penghou provider; separate from authority, truthful coverage |
| VFS-9 | Immutable Git-tree/staged views | Optional provider; Git concepts stay outside base contracts |
| VFS-10 | Remote/container workspaces and containment integration | Optional provider + host; explicit isolation guarantees |

VFS-8/9/10 can develop independently when prerequisites exist; the list is not a
requirement to implement every earlier feature first. No VFS is an OS sandbox.

Baize routing remains Baize-owned and deferred. Guihua file prompts and Hetu
repository reads are future IO consumers; their domain contracts stay put.
Guihua artifact publication and Baize diagnostics require qualified mutation/storage
capabilities. SQLite backend extraction is separate work, outside this correction.

## Handoff checklist

Every implementing handoff must cite this document, its RA/VFS ID and the applicable
provider profile; state current versus intended behavior; list unresolved gates;
identify owned files and dependencies; and state concrete completion evidence.
Record decisions here or in a linked ADR before changing interpretation.
Do not mark a roadmap box complete on the strength of design prose or a mock alone.
