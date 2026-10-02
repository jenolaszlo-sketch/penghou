# Windows local reader profile v1

`Penghou.IO.Local.LocalWorkspaceReader` implements `IWorkspaceReader` on Windows.
It is read-only and requires an explicit `IResourceAuthorizer`. Host-selected
workspace identity and root are constructor inputs. No default authorizer,
writer, web backend, language executor, Hufu adapter or package release is included.

## Identity and authorization

Use the shared `ResourceRequestIdentity.Compute` overload for each request and
put its result in the trusted `HostInvocation.RequestIdentity`. The provider
recomputes it before authorization. The digest covers concrete operation inputs,
not authenticated invocation fields or its own digest. The authorizer must
authenticate IDs, retain semantic parent/request mappings and enforce current
scope ceilings. A matching hash is not authentication. Luban's semantic JSON
digest remains a separate identity.

Workspace paths use `/`, bounded validated segments and preserved display case.
The codec folds ASCII letters for identity and preserves non-ASCII spelling
exactly, avoiding assumptions about filesystem Unicode upcase tables.
Native metadata/component handles and the actual content handle must resolve to the admitted full path under ASCII case equivalence before attributes or content are used. Unicode case aliases and DOS short names cannot reinterpret the relative target; correct Unicode spelling and ASCII case variants remain supported. The comparison preserves the admitted spelling rather than expanding aliases through Path.GetFullPath. Native metadata opens use extended-length drive/UNC spelling, but UNC qualification remains pending.
The reader requires case-insensitive
directories, querying the Windows directory case-sensitivity flag after
authorization; case-sensitive or unqueryable directories are Unsupported.
It rejects invalid/traversing/absolute/device/ADS paths and observed reparse
points. Metadata probes on root and intermediate components receive explicit
ReadMetadata checks before inspection. Exact leaf actions are checked first;
list candidates receive ReadMetadata as generic WorkspaceEntry before type or
size inspection. Enumerating names internally requires directory permission;
no candidate is released before its own check. Only Permit proceeds. Deny is
AuthorizationDenied; null/unknown/unavailable/throwing authorization responses
are AuthorizationUnavailable. Errors have sanitized details.

## Read and observation semantics

ReadFile returns bounded arbitrary bytes, with no UTF-8 decoding. Its version
is `local-read-v1:sha256:<uppercase SHA256 of returned bytes>`. This identifies
observed content only, not a stable native object or an atomic mutation token.
GetFileMetadata returns existence/size after authorization; a missing file
returns Exists=false. Metadata and directory entries return no version, because
metadata authority does not authorize content reads. A directory is not file
metadata. Luban owns text decoding and its user-facing content hash.

The default and hard maximum file bound is 16 MiB. Default listing limits are
10,000 entries, 100,000 candidate inspections and 4 MiB logical output. Options
can lower caps; entry cap can rise to 100,000. All request bounds are positive
and independently checked before target I/O. Every enumerated candidate consumes
scan budget, including denied or malformed names.
The reader also maintains a private aggregate candidate allowance across all
list calls, default/hard maximum 100,000, configurable downward with
MaxTotalCandidatesScanned. This bounds a whole host-owned traversal without
disclosing excluded-candidate counts. It conservatively charges the final
end-of-enumeration probe and reinspection of a deferred output entry as work too.
An atomic reservation precedes each enumeration/inspection, including concurrent
calls. Exhausting it truncates the current page;
later list calls return TooLarge. It never resets when a token or directory changes.
Use one reader per bounded semantic invocation when that budget should be shared.

Logical output cost is UTF-8
name bytes plus 32 bytes per entry for fixed metadata/framing; versions are null.
An entry that cannot fit an empty page returns TooLarge, preventing endless
zero-progress output continuations.

## Paging, deadlines and lifetime

Pages contain only authorized entries, with no denied names, counts or omission
flags. IsComplete concerns the authorized view; it cannot establish all-match
coverage of the physical directory. Entry/candidate/output limits produce an
explicit truncation reason and opaque continuation. At an exact entry or scan
boundary, exhaustion may be discovered on the next page; a final empty complete
page is valid. Denial alone creates no truncation flag.

Continuations are random 256-bit references to private retained enumerators.
State lookup authenticates the token; it binds workspace, directory, all bounds
and every invocation/scope field except the per-page request identity. Tokens
are single-use, with fresh directory/ancestor/candidate checks on each page.
They grant no permission. Unknown, expired, reused or context-mismatched tokens
return AuthorizationDenied, and mismatched callers cannot consume valid state.
Changing page bounds requires a new enumeration. There is no stable filesystem
snapshot or ordering guarantee across pages.

Default retention is two minutes and at most 128 pending cursors per reader;
options permit at most ten minutes and 1,024 cursors. Expiry uses monotonic time.
Expired state is reclaimed at the next paging call; capacity is always finite.
Dispose the reader to release abandoned enumerators. In-flight operations own
their withdrawn cursor and dispose it on failure/cancellation. Capacity exhaustion
returns TooLarge, never unbounded retention.

The default operation deadline is 30 seconds, configurable up to five minutes.
Asynchronous authorization and reads are bounded by cancellation. Caller
cancellation throws OperationCanceledException; internal deadline expiry is a
sanitized ProviderFailure. Synchronous filesystem/OS calls are cooperative and
may outlast the deadline; the provider cannot preempt a blocked kernel call.

## Qualification limits

Path checks and later use can race with local replacement. Hard links, mount
boundaries and changes between observations are not confinement guarantees.
Case-sensitivity and reparse checks can themselves race. Host roots are trusted;
the profile does not validate ancestors outside the selected root. Tests prove
the documented boundary ordering and observed-link rejection, not hostile-process
containment. Native object-bound mutation consistency requires a separate profile.

The provider offers no preview execution or barrier. Hosts/Luban own static
preflight, authorized resolution, immutable plans, whole-plan admission and
commit ordering. A read permission, continuation or result cannot authorize a
later write.


Local qualification on 2026-10-01 passes 86/86 shared tests on each .NET target, including real Unicode leaf/directory/root alias regressions and an observed DOS short-name alias. Hufu passes 44/44 and Luban 167/167 on each runtime against this reader; see [the authority qualification record](../../Penghou.Hufu/docs/authority-profile-qualification.md). These observations do not close the replacement/confinement limits above.
