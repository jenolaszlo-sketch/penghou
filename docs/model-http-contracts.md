# Model and HTTP transport contracts

The first candidate profile supplies two independent .NET 8/.NET 10 packages at
`0.1.0-preview.1`: `Penghou.Model.Abstractions` and `Penghou.Http.Abstractions`.
They are contracts, not providers or a policy engine. Read [ADR 0004](decisions/0004-model-http-transport-contracts.md)
and the [release checkpoint](model-http-release-handoff.md) before adoption.

## Model invocation

`IModelTransport<TRequest,TResponse>` and `IStreamingModelTransport<TRequest,TEvent>`
use the consumer's typed payload and result/event schema. The generic type
closure stays in the consumer; Penghou never references Baize or provider SDKs.
Requests describe provider, wire-model, endpoint profile, operation, request ID,
one-based actual attempt, optional credential profile, context and usage intent.
Responses and stream events carry the request ID they belong to.

Identifiers are nonblank, whitespace/control-free valid Unicode with a 256-byte
UTF-8 limit. Opaque job IDs allow 1024 bytes. Metadata allows 32 entries, 64-byte
keys and 1024-byte values; batch item descriptors allow 1000 entries. Snapshots
are read-only. Invalid values/counts, nulls and inconsistent metadata collections
fail before publication/dispatch. Payload size, schema, nested collections and
immutability must be enforced by the consumer's qualified data profile before
authorization. The envelope itself cannot inspect or freeze arbitrary generic
payloads. Streams, callbacks and executable capability objects are unsupported
payload profiles. No generic escape hatch grants permission to execute one.

Operations are extensible validated names. Known values cover stream/generate/
complete, batch submit/status/results/cancel, job submit/status/results/cancel,
upload/artifact access, embedding/rerank/moderation. Defining a name does not
implement its operation. A transport must reject unsupported operations and
payload profiles rather than silently execute through another route.

Context workflow/activity/agent/correlation IDs are data, not authenticated
subjects or grants. The host binds request/context/target and every batch item
to retained authenticated mappings and an independently established parent
ceiling. A job handle is also data: its target must match the invocation, and
the host verifies origin, current context and authority before access. A denied
status/results request cannot undo a remote job already accepted.

Usage values remain nullable. `CachedInputTokens` preserves reported cache hits/
reads; `CacheMissInputTokens` preserves reported misses/writes/creation separately.
Thinking and total tokens retain provider semantics. Do not manufacture zero or
compute a total from missing fields, or double-count cached/thinking categories.
Cost values are nonnegative decimals paired with uppercase three-letter currency;
intent estimates are descriptive, not reservations or guarantees. Accounting
remains in an existing host budget service.

Neutral access/budget denial is distinct from availability/provider failure and
ordinary cancellation. Exceptions carry bounded request ID/reason code and no
raw provider diagnostic cause. Do not log a raw payload or provider exception as
part of the neutral result. The caller must verify result/event request identity;
for returned handles it also verifies target/context provenance. Default envelope
descriptions omit prompts/responses.

Each physical attempt is checked at the execution boundary. Routing/registration
checks or cached prior permits are insufficient. A lazy stream must check when
enumeration actually opens it. Initial revocation blocks new calls/opens; an
active stream may finish. Stronger active cancellation/drain has separate gates.
Credential profile resolution happens only in the trusted implementation after
the semantic check, before private provider HTTP. No credential is required in
the public model payload/envelope.

## HTTP invocation and ownership

`IHttpTransport.SendAsync` receives an exact request ID, case-sensitive method,
absolute HTTP/HTTPS URI, immutable headers/body/context and explicit limits.
User-info/URI fragments and caller-controlled Host/Content-Length/Content-Type/
Transfer-Encoding headers are rejected. The provider derives framing/media type
from the body, safely encodes multipart fields and enforces effective destination
policy. Explicit request headers may contain credentials in a trusted provider
composition; they never become public model-request fields or default telemetry.

Headers have 64 field names, 16 values per field, 8192 bytes per value, and a
65536-byte aggregate UTF-8 name/value bound. Request and response header names
are case-insensitive snapshots; values preserve order. Header/control/Unicode
injection and inconsistent collection counts fail. Request binary/multipart
bytes are copied; returned byte arrays are detached copies. Multipart has at most
64 parts and a logical basename, never a filesystem path to open.

Finite request payload and decoded response ceilings are 1 GiB; both default
limits are 64 MiB. Multipart framing is separately bounded by the provider.
Response reads default to at most 16 KiB, with a 64 KiB hard chunk maximum.
Timeout defaults to two minutes, includes body consumption, and may be configured
up to one day. Redirects default to zero, with at most ten explicitly requested;
a nonzero value grants no destination access. Each redirect needs current
validation, credential stripping and effective size/deadline enforcement. No
ambient cookies, default credentials or automatic network fallback is implied.
Unsupported bounds/redirect/upload profiles must fail explicitly.

`HttpTransportResponse` owns `IHttpResponseBody`. Dispose the response after
success, failure, early exit or cancellation. Read sequentially with a positive
bound within `MaximumReadBytes`. Every chunk is a copied snapshot and identifies
whether it completes the body; empty nonterminal chunks are invalid. Providers
enforce the requested read bound, aggregate decoded bytes/header limits, deadline,
cancellation and idempotent disposal. Headers arriving successfully do not prove
the body completed. Do not call this process-local capability a serializable job
or broker record. A future broker uses its own bounded wire protocol.

Raw `Stream`, `HttpContent`, `HttpClient`, arbitrary callbacks and native handles
do not appear in the HTTP contract. Initial uploads are finite binary/multipart
snapshots. Larger or streamed request sources need a separate reviewed profile;
they are not silently buffered into the existing one.

## Composition boundary

Model invocation and arbitrary HTTP are distinct capabilities. A host can permit
one model and deny caller HTTP, while its trusted model implementation uses a
separate injected provider HTTP transport. No caller-set trusted flag or endpoint
URI can select that internal authority channel. The host validates profile mappings
and required evidence. An in-process decorator alone cannot contain arbitrary
agent code, protect its secrets from that code or stop raw sockets.

The packages include no credential resolver, HTTP client, authorization default,
billing store, sandbox or provider implementation. Contract conformance tests and
package-only consumers qualify the seam. Real Baize coverage, Hufu enforcement,
host custody and physical isolation are separate evidence streams.
