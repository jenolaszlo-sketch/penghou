# Architecture

Penghou.IO.Abstractions defines contracts at the concrete resource boundary.
It provides a read-only `IWorkspaceReader`, mutation-only
`IWorkspaceWriter`, a combined `IWorkspaceFileSystem`, and a bounded web GET
contract. Consumers can depend on the narrower interface they need. Interface
narrowing is an API composition choice; it does not authenticate a host or
prove enforcement. This repository includes no provider or authorizer
implementation.

Requests carry a host-authenticated invocation with subject, effect, attempt,
scope references, and a request identity. The request records themselves are
data only: constructors do not authenticate these values. The trusted host
resolves scope references against retained mappings and preserves parent scope
ceilings. A root invocation may have no parent scope. The authorizer resolves
the effect binding and current fences; identifiers are not bearer tokens and
source input cannot choose them.

The provider snapshots all mutable input buffers and patch collections before
authorization. It derives the canonical identity of the concrete backend
request from that stable snapshot and matches the authenticated host request
identity before proceeding. This child request identity is distinct from its
admitted semantic effect digest; the authorizer verifies that the concrete
request fits the effect and inherited ceiling. Every access then receives a
final authorization check for its exact action and resource. Only an explicit
`Permit` authorizes access. Denial and authorization service unavailability
have distinct typed failures, separate from operating-system access errors.

The host validates syntax, types, and execution profile before I/O. Luban owns
semantic typed effects and finite dataflow; Fuwen and Zhinu own workflow
control and durability. Preflight checks all nodes' known static effects and
exact targets before upstream reads or artifact creation, so a known denied
write blocks the document early. This static preflight reserves no authority and cannot
replace the provider's final current-authority check. Dynamic targets require
bounded, already-authorized discovery. Intermediate dataflow should remain
lazy, bounded, and cancellable. Persistence and debug/release artifacts require
explicit authorization. No global pipeline atomicity is promised.

[ADR 0002](decisions/0002-preview-resolution-commit-barrier.md) and the
[preview/commit contract](preview-commit-contract.md) add planned authorized
read-only resolution and whole-known-mutation-set admission before an executor
barrier. Resolution performs real authorized observations; it is different from
static preflight and pure policy simulation. The host binds each concrete
mutation to an immutable admitted plan/segment, without adding a language-plan
dependency to this library. WhatIf leaves requested mutations and opaque/lazy
tools unexecuted. Required incomplete coverage blocks, and final live checks
remain. This admission boundary is not an atomic transaction or race closure.

Workspace paths are relative to an opaque workspace identity and use `/` as
the contract separator. Providers validate segments and reject absolute,
escaping, or otherwise invalid paths under their platform policy. References
do not carry capabilities. Reading requires `ReadFile`; metadata or existence
checks require `ReadMetadata`; listing requires `ListDirectory` on the
requested directory and `ReadMetadata` on a generic `WorkspaceEntry` before
examining candidate type or metadata. The provider may then bind an authorized
candidate as a file or directory. Authorized-only pages never disclose denied
names or counts; completeness describes only exhaustion of the authorized
view, not the absence of excluded resources. Continuation tokens bind workspace,
query, and subject, while every page repeats checks under current authority.

Mutations check concrete resources by role. A file move requires `ReadFile` and
`DeleteFile` on the source and `WriteFile` on the destination; `MoveFile` may
add a semantic check but cannot replace those role checks. Metadata permission
is checked before metadata or existence probes. Writes use either
`MustNotExist` or an exact provider-issued version. Patches use UTF-8 byte
offsets against one exact original version. A provider validates patch ordering,
bounds, scalar boundaries, and encoding before mutation. Preconditions must be
atomic with commit; the provider profile documents guarantees and the provider
returns `Unsupported` when it cannot meet them. Uncertain committed outcomes
return `AmbiguousOutcome` and must not be blindly retried.

Web access supports bounded GET only, with no caller-controlled headers,
cookies, credentials, or implicit credential forwarding. A provider validates
HTTP or HTTPS URLs, authorizes requested and redirected URLs, and authorizes
each resolved connection IP and port before connecting to that exact endpoint.
The provider profile documents DNS, connect, response-header, total-operation,
redirect, wire-byte, decoded-byte, and SSRF address limits. The result contains
status, effective URL, and content, without a filesystem version token.

Path-based authorization cannot alone close time-of-check/time-of-use races.
Unless a provider implements stronger handle-relative operations, a concurrent
local process may replace a checked path between authorization and use. Direct
`System.IO` calls, other process access, and APIs that bypass a provider are
outside this library's coverage. The contracts do not claim sandboxing or
confinement. No web provider or SSRF protection is implemented here.

Hufu owns grants, revocation, and authority evidence and remains independent.
An adapter can be added as a separate integration package; no Hufu dependency
belongs in this project.
