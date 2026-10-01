# Provider contract

These requirements define intended provider behavior. They are design
contracts, not implemented or verified runtime guarantees in this repository.

## Preflight and access checks

The host validates the complete pipeline's syntax, types, and execution
profile before I/O. Preflight checks known static effects and exact targets for
all nodes before upstream reads or artifact creation. If preflight determines
a later write is denied, reject the whole document before upstream reads or
artifact creation. Luban owns semantic typed effects and finite dataflow; Fuwen
and Zhinu own workflow control and durability. This preview is
advisory and performs no protected target I/O; it may read trusted authority
state. It is not a grant or reservation, can become stale, and
does not replace any provider access check. This project intentionally does not
add a second preview method that duplicates Luban's semantic effect API.

Dynamic paths or destinations are checked after bounded, authorized discovery;
preflight must not perform eager unauthorized reads under another name. The
provider checks current authority and binds each final action to its concrete
resource immediately before protected access. Earlier reads can have occurred
if live authority changes later, but a denied write never occurs. Keep
intermediates lazy, bounded, and cancellable. Persistence and debug/release
artifacts require explicit authorization. There is no global atomicity guarantee
across a pipeline.

## Invocation and request identity

Request record constructors are data constructors; they do not authenticate the
invocation. The provider must snapshot every mutable input buffer and patch
collection first, derive the canonical concrete backend request identity from
that snapshot, and compare it with the host-authenticated
`HostInvocation.RequestIdentity` before asking for authorization or doing
resource I/O. Each authorization request carries both the authenticated
invocation and the identity recomputed from the stable snapshot. A mismatch
fails closed as `AuthorizationDenied`.

The host authenticates and resolves `SubjectId`, `EffectId`, `AttemptId`,
`InvocationId`, and effect-scope references. `RequestIdentity` identifies one
concrete backend request; `EffectId` resolves the admitted semantic effect,
parent digest, and ceilings through retained host mappings. A child read
request has its own identity and must be checked as fitting the retained parent
effect; it is not compared to the parent's semantic request digest. Scope IDs
are neutral references, not bearer tokens. The authorizer resolves them through
retained mappings, checks current fences and the inherited ceiling, and refuses
stale or unknown context. Parent scope may be absent for a trusted root
invocation. Source input cannot choose these values. The concrete request
identity covers operation kind, target, all limits and preconditions, and all
content bytes; it uses a versioned canonical encoding and collision-resistant
digest. An authorization call binds that identity to one action and one
concrete resource.

## Authorization ordering

An authorizer returns `Permit`, `Deny`, or `Unavailable`. Only `Permit` allows
the protected operation. `Deny`, `Unavailable`, unknown enum values, null
responses, exceptions, malformed bindings, missing invocation context, or
request-identity mismatch fail closed. Map an explicit denial to
`AuthorizationDenied`, an unavailable or invalid authorization service to
`AuthorizationUnavailable`, and an operating-system access error after a
permit to `AccessDenied`. Never map an unavailable result to denial or permit,
and never supply an implicit permit.

| Operation | Required concrete checks before protected access |
| --- | --- |
| Read file | `ReadFile` on the exact workspace file before opening or reading bytes |
| File metadata/existence | `ReadMetadata` on the exact workspace file before stat or existence probing |
| List directory | `ListDirectory` on the requested directory; then `ReadMetadata` on every candidate bound as `WorkspaceEntry` before inspecting its type or metadata. Bind it as a file or directory only after that check. |
| Write file | `WriteFile` on the exact destination before probing or mutation; check its precondition without leaking unauthorized state |
| Patch file | `PatchFile` on the exact file before reading bytes; enforce expected version and patch bounds before commit |
| Delete file | `DeleteFile` on the exact file before metadata/read or deletion; enforce expected version |
| Create directory | `CreateDirectory` on the exact directory before existence probing or creation; this API creates only one directory and does not create parents |
| Move file | `ReadFile` and `DeleteFile` on the source, and `WriteFile` on the destination; a `MoveFile` semantic check may be added but never replaces these role checks. Authorize `ReadMetadata` before metadata/existence probes. |
| Read web resource | `ReadWebResource` on the requested URL before DNS or network access; authorize each redirect URL and resolved connection endpoint before sending a request there |

For a batch operation, check every source and destination separately. Parent
scope authorization is not a substitute for exact-resource checks. Directory
candidate denials may abort the page with `AuthorizationDenied` or be omitted
from an authorized-only view. A denied candidate's name, metadata, and count
must never be included or emitted as telemetry. `IsComplete` means the
authorized view was exhausted within the requested bounds; it does not prove
that the underlying directory contains no excluded resources. Denied omissions
alone do not cause truncation or a fabricated continuation. Every later page
repeats the directory and candidate checks using current authority.

## Directory pages and bounds

`MaxEntries`, `MaxCandidatesScanned`, and `MaxOutputBytes` are independent finite
positive bounds and are validated independently before I/O. Candidate scan
budget applies even when no entries match or candidates are denied. Candidate
scan counts are internal budget state only: do not expose denied counts or
count-based telemetry. Output-byte accounting is defined by the provider
profile and includes serialized entry names and returned metadata.

Every page states `IsComplete`, `TruncationReason`, and continuation
consistently. A complete authorized view has no continuation and no truncation
reason. An incomplete view caused by a finite limit has an opaque continuation
and an explicit reason such as entry, candidate-scan, or output-byte limit.
No-match pages that exhaust the authorized view are explicitly complete.
Continuation tokens are authenticated and bound to workspace, directory/query,
and subject; reject a token used in another context and repeat fresh
authorization for all operations on a continued page. Continuation does not
promise a stable filesystem snapshot across pages.

Input byte limits are finite and positive. Patch count, aggregate replacement
bytes, and output bytes have independent finite positive limits; each is
validated before reading or mutating. Providers reject oversized inputs or
outputs and never silently truncate writes, patches, reads, or web responses.
Providers must define and enforce their accounting for encoded and decoded web
response bytes. Directory pages return no more than `MaxEntries` and
`MaxOutputBytes`.

## Patch and mutation semantics

Patch offsets and delete lengths are UTF-8 byte offsets against the exact
original version named by `ExpectedVersion`; every range is evaluated against
that same original snapshot, not a progressively edited buffer. Require strict
valid UTF-8 in the original and replacements, scalar-boundary start/end offsets,
nonnegative in-bounds ranges, and ordered nonoverlapping patches. Providers
must bound patch count, aggregate replacement bytes, and final output bytes.
Any malformed or oversized patch fails before mutation.

`MustNotExist` and `MustMatchVersion` must be enforced atomically with commit.
Patch, delete, and move source versions have the same requirement. Each
provider profile must state which atomic consistency guarantees it supplies;
if a request's precondition cannot be honored, return `Unsupported` without
mutating. If the expected state differs, return `PreconditionFailed` and make
no mutation. Successful writes and patches return a new provider-issued
version. Version tokens are provider-specific and are not portable hashes
unless documented by that provider.

Cancellation is honored before the commit point. If cancellation or provider
failure occurs after a mutation may have committed and the outcome cannot be
determined, return `AmbiguousOutcome`. Callers must not blindly retry that
operation. Do not report ordinary success or failure when commit status is
unknown.

## Web access

The current contract supports GET only. There are no caller-controlled headers,
cookies, credentials, or implicit credential forwarding. Require an absolute
HTTP or HTTPS URL without embedded user information. Authorize the requested
URL before DNS lookup. For each redirect, enforce a finite provider-profile hop
limit, authorize the redirect URL, resolve its connection endpoint, authorize
the exact IP address and port, and connect only to that authorized endpoint.
Bind the connection to the checked resolution to prevent DNS rebinding between
check and connect. Do not follow a redirect or retry at a different endpoint
without repeating these checks.

Profiles must state finite DNS/connect, response-header, and total-operation
time bounds, redirect limits, and wire and decoded body limits. Decompression
must not bypass `MaxBytes`; reject oversized wire or expanded content. Apply
SSRF address policy to every resolved address and endpoint, including private,
loopback, link-local, and reserved ranges according to the host profile. Return
the actual status code, effective URL, and bounded content; non-success HTTP
status is still a web result, not filesystem `NotFound`. These interfaces do
not include a web backend or claim SSRF protection.

## Failure and concurrency limits

Invalid paths or requests map to typed failures without revealing unrelated
filesystem details. Unexpected provider faults map to `ProviderFailure`; raw
exceptions, streams, and native handles are not part of these contracts.
Operating-system access errors remain distinct from authorization denials.

Path checks and subsequent path-based operations can race with concurrent local
filesystem changes. A provider without handle-relative APIs cannot guarantee
that the object used is the exact object previously authorized. A provider that
cannot uphold required version atomicity must return `Unsupported`. No provider
implementation exists here, and no runtime security property is claimed.
