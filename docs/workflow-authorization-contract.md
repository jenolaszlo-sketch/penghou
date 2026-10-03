# Workflow authorization contract, version one

Status: WA-1 selected contract decisions and WA-2 implementation profile,
2026-10-03. This specifies `Penghou.Workflow.Abstractions`; it does not claim
that Zhinu or Hufu.Workflow implements the protocol yet. Read the
[source review](workflow-contract-review.md) and [delivery plan](workflow-abstractions-plan.md).

## Values and identity

The package exports IExecutionAuthorizer, ExecutionIdentity,
ExecutionAuthorizationContext, ExecutionRequirement,
ExecutionAuthorizationDecision and ExecutionAuthorizationResult. No existing
Zhinu public type is moved or redefined. This is a new additive seam, so existing
binary/source consumers and serialized engine types retain their identities.
Actual runtime adoption is a later compatibility gate.

ExecutionId is unique within the configured trusted host namespace. OperationId
is stable within an execution; OperationPath is an optional logical location,
not an OS path or principal. Attempt is the one-based intended actual callback
attempt. ParentExecutionId identifies a distinct parent in the same namespace.
PlanId and PlanRevision are both supplied or both absent. They identify the exact
retained plan, not a human label or caller claim that a plan was approved.

ExecutionRevision is an opaque runtime freshness token. A runtime changes it
when ownership or effective execution state relevant to dispatch changes. The
runtime may use a generation/revision tuple, hash or other representation; the
shared package knows no lease, database, engine or counter type. After awaiting
the provider, the runtime checks current ownership/revision using its own
retained facts, not a provider's assertion that a fence remains valid.

AuthorizationRequestId is fresh for **every evaluation**, including after
approval, recovery or an infrastructure retry. The trusted runtime binds the
complete immutable context and trusted mapping identity to this ID before the
call. Never reuse an ID for another context or evaluation, even if Attempt has
not changed. A response must echo that exact ID. IDs and revisions are opaque,
ordinal, case-sensitive values; constructors preserve them without normalization.

Context/result SchemaVersion is one; other values are rejected, including during
JSON construction. Requirement SchemaId and positive SchemaVersion select an
explicit adapter vocabulary. A syntactically valid future requirement is data,
not a recognized permission. Unknown vocabulary/version/capability fails closed
in a configured authority adapter.

## Bounds and trusted mapping

All values are immutable records with validated constructors and getter-only
properties. Context construction snapshots the declaration collection; later
caller mutations cannot change what is evaluated. Reported collection counts
that change or disagree with enumeration are rejected. Requirements preserve
declared order; do not use record GetHashCode or ToString as durable identity.
The constructor's enumeration order defines the frozen order. Runtimes supply
a deterministic sequence from the retained definition (prefer an array/list),
not a HashSet's incidental traversal order; they hash/persist the actual snapshot.

| Value | Maximum UTF-8 bytes |
| --- | --- |
| IDs, revisions, scope references, diagnostic/evidence/correlation references | 256 |
| Requirement schema/capability | 128 |
| Requirement resource and logical operation path | 2048 |

At most 64 requirements are accepted. This bounds decoded context string value
bytes to 167,680, independently of transport escaping/overhead. Strings must
contain valid Unicode, no control characters and no surrounding whitespace.
Reject overlong UTF-16 inputs before encoding/scanning. Hosts separately bound
incoming transport/journal payloads before JSON deserialization; these value
constructors are not an untrusted transport parser or execution sandbox.
Strict versioned wire profiles also reject unknown properties, for example
using System.Text.Json's UnmappedMemberHandling.Disallow; the default serializer
can ignore extra members. A codec/parser change is a host protocol decision,
not an authorization interpretation of unrecognized metadata.

Resource and ScopeReference are opaque **logical retained references**. A
resource-scoped requirement schema specifies whether ScopeReference is mandatory
and its interpretation. Non-resource capabilities may have no scope reference;
their Resource identifies the logical execution target (for example a retained
operation reference), rather than a filesystem object. The requirement vocabulary
defines that convention; there is no universal wildcard or ambient target.
Missing required scope, unknown reference, path/profile mismatch or insufficient
parent ceiling never becomes broad authority. The trusted adapter resolves
these references and checks the actual declared capability, not arbitrary
workflow strings as raw OS paths.

Principal/tenant/session data is deliberately absent from the neutral values.
A configured trusted host service binds the full runtime-created request,
host namespace and retained execution identity to independently authenticated
actor/scopes. It must reject an absent/mismatched binding and preserve parent
ceilings. Neither ExecutionId, metadata nor ambient user state authenticates a
subject. Hufu.Workflow will obtain that binding from its trusted host composition,
without depending on Zhinu state or SQL. The authorizer interface does not
authenticate the caller and must not be exposed directly as an unauthenticated
grant service.

An empty requirement set is representable, but is never implicit permission.
The protected Hufu mapping denies/unavailable on empty or unsupported declarations.
If a future host needs an operation with no resource effect, it declares a
recognized execution capability and evaluates explicit policy for that capability.

## Closed outcomes and dispatch

Unavailable=0 is the safe default enum; Allowed=1, Denied=2,
ApprovalRequired=3 and Error=4 are explicit. Constructors reject unknown enum
values. Every response carries the exact request ID, configured ProviderId,
DecisionId and nondefault evaluation time (normalized to UTC). These references
make a response attributable; they are not proof that evidence was durably written.

Allowed requires ExpiresAt strictly after EvaluatedAt. A runtime verifies that
the evaluation is not unacceptably in the future, expiry is still current and
validity does not exceed its configured finite policy limit. It then verifies
request/provider binding, independently required provider evidence, mandatory
runtime outcome evidence and its current fence, followed by a cancellation check,
**before any protected activation or callback**. A valid object alone is never
sufficient to dispatch. If required evidence cannot be persisted, zero user
callbacks run.

Denied executes zero protected callbacks and records a terminal authorization
denial. ApprovalRequired executes zero callbacks and requires ApprovalRequestId;
other outcomes forbid that field. ReasonCode is bounded diagnostic data and
never selects approval or permission. EvidenceId is an optional provider evidence
reference; the configured profile determines which evidence must exist and be
validated before dispatch. Optional reference syntax does not waive that rule.

Unavailable/Error, null/malformed responses, unknown schemas, provider exceptions
and provider timeouts execute zero callbacks. Caller cancellation propagates
as cancellation; provider-internal timeout is an infrastructure outcome. The
runtime records infrastructure/authorization evaluations separately from actual
callback attempts. No automatic authorization retry is the default; a host can
choose a finite retry/time budget, with a fresh request ID each time. Exhaustion
parks/fails according to the explicit host profile; it never falls back to Allow
or spins indefinitely. Callback retry allowance is not consumed by provider
outages before a callback starts.

## Durable approval and recovery design

The neutral package contains no durable state machine or storage implementation.
The selected runtime protocol is:

1. Bind the exact immutable context, provider/enforcement mode and trusted mapping
   identity to a fresh request ID; evaluate without holding a database writer
   transaction or protected user instance alive.
2. Persist mandatory outcome evidence. For ApprovalRequired, persist a versioned
   pending checkpoint with request/context/plan/requirements identity, provider,
   approval correlation and runtime revision. Park and release the worker/lease.
3. Accept only trusted, matching approval wake-ups. Persist the wake idempotently;
   duplicate, stale or different-context events never directly dispatch work.
4. Reconstruct current context and authenticated binding, reacquire current
   runtime ownership, and evaluate again with a fresh request ID. Current deny,
   revocation, unavailable, changed requirements/revision or expired decision
   still blocks dispatch. An approval wake is a trigger, not a bearer permit.
5. Check the post-await fence, persist the actual start and execute only the
   current allowed callback. Preserve interrupted-effect/idempotency semantics;
   recorded authorization never proves that an external effect completed.

Pending state survives restart and does not retain a human-duration execution
lease. Lost decision/evidence replies are reconciled from retained state before
any dispatch; missing/ambiguous state blocks fresh starts. Historical completed
result reconstruction does not execute the operation again. A new attempt or
resumed callback always gets a fresh decision. Result disclosure may require a
separate host authorization policy.

Canonical plan/context digests remain owned by the runtime/approved-plan service.
That versioned encoding binds the host namespace, trusted mapping identity,
schema, exact ordinal IDs/revision/path, ordered complete requirements (including
scope), plan identity and fresh request ID. Approval and evidence compare the
same retained snapshot/codec version. Changing encoding or a mapping version
invalidates reuse; no fallback comparison drops unknown fields. The abstractions
package does not ship a hash algorithm or introduce a protocols dependency.

## Zhinu design decisions reserved for the next implementation phase

The source review identifies future insertion points; none is implemented here.

| Path | Selected design |
| --- | --- |
| Step delegate/class | Add explicit immutable requirements to StepOptions and retained approved definition identity. Gate each acquired actual callback attempt; historical Reused returns existing output. Gate before resolver activation and user constructors; dispose through existing ownership semantics |
| Declarative activity | Requirements belong to the versioned descriptor and exact definition identity. Move protected executor resolution behind the common gate; avoid authorizing one operation twice |
| Compensation | Separate declared requirements and attempt identity; authorize before protected callback/activation. Forward permission never authorizes compensation |
| Loops/body/predicate | Audit every user callback as its own declared operation, including loop body/predicate where these execute caller code. Nested step gates do not cover arbitrary body code. Declaration/identity additions require runtime API and durable schema qualification |
| Direct/hosted/child/restart | One effective configured authorizer. Persist enforcement mode/provider identity; a previously protected execution cannot downgrade to no-provider Allow after recovery |
| Pending/claim/retry accounting | Versioned pending checkpoint separate from historical completed output; authorization evaluation attempts tracked separately from actual callback attempts. Reconcile current claim increments during implementation; qualify crash/retry/schema compatibility before release |

The named no-provider allow-all implementation remains Zhinu-owned and preserves
ordinary unprotected behavior. A configured provider failing/disappearing cannot
select it. No default authorizer is supplied by Workflow.Abstractions.

## Legacy effect profile disposition (ZA-5A)

Retain the existing Hufu.Zhinu.Sqlite profile, frozen and isolated, until its
required actual-effect/atomic-start guarantees have an explicitly qualified
replacement or deliberate consumer retirement. It must not become a dependency
of Hufu.Workflow or Workflow.Abstractions. Neutral preflight provides no shared
SQL transaction or atomic revocation/start guarantee. Resource/IO/Luban checks
remain independent, including discovered resources and final locked mutation
binding. ZA-5B stays open; this package does not retire legacy behavior.

WA-1 contract design is resolved by this document; WA-2 qualification records
actual candidate evidence separately. ZA runtime/HA adapter implementation and
integration acceptance remain open until their own phases and tests complete.
