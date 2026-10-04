# ADR 0004: neutral model and HTTP execution transports

Date: 2026-10-04. Status: accepted for the first candidate contract profile;
publication and real Baize/Hufu adoption are separate qualification gates.

## Decision

Ship two independent, product-neutral packages, `Penghou.Model.Abstractions`
and `Penghou.Http.Abstractions`, initially 0.1.0-preview.1 for .NET 8 and .NET 10.
Neither references Baize, Hufu, workflow contracts, provider SDKs or the other
transport package. Existing published IO and workflow APIs/versions stay intact.
`IWebResourceReader` is a bounded credential-free GET resource capability and
has no equivalent authenticated POST/multipart/stream-response contract to reuse.
It remains unchanged; general HTTP belongs in its own capability domain.

The model seam uses generic, strongly typed data payloads and response/event
types. A consumer closes the generic interface over its existing semantic data
types. This avoids moving all Baize chat/media/tool/diagnostic types into shared
contracts or sending `object`/provider HTTP through a model policy boundary.
Payload schemas, byte/count limits, ownership and provider extensions are part
of the consumer's qualified profile. These envelopes cannot enforce an arbitrary
generic payload's immutability or size: providers must snapshot/validate it
before admission. Unsupported payload profiles fail explicitly; no delegate,
stream or execution callback is an admitted model payload. Package qualification
does not claim the as-yet-unimplemented Baize adapters enforce this requirement.

Separate invocation and streaming interfaces preserve existing lifecycle and
lazy enumeration. Requests include request/attempt identity, target provider,
wire-model and host-selected endpoint profile, extensible operation, opaque
credential profile, optional correlation context and usage intent. Bounded item
descriptors preserve batch preflight information. Job handles retain opaque
identity plus target/provenance; a handle and context fields grant no permission.
Stream events use the consumer's typed event schema without flattening it into
completion text. Streaming adapters must check again at actual enumeration/open.

Neutral operation values are extensible strings with known lifecycle names.
There is no promise that every transport supports every operation. Consumer
coverage includes chat, native-completion extension branches, native batches,
generation jobs, uploads and result/asset retrieval. Unsupported operations
cannot fall back to ungoverned dispatch. Payload/result schemas preserve current
chat tool/media/terminal/usage events and asynchronous job semantics in Baize.

Contexts snapshot bounded string metadata. Workflow/activity/agent/correlation
IDs are descriptive, propagated unchanged and authenticated separately by the
host. HTTP context contains request/correlation metadata appropriate to HTTP;
it is deliberately not the same model/workflow authority context. Model-to-HTTP
mapping belongs to the private provider adapter after semantic authorization.
Costs and token counts are nonnegative, nullable and explicitly descriptive;
missing or partial usage never means zero. Monetary values carry a currency.
No accounting, policy evaluator, credential resolver or concrete default
transport is shipped in these packages.

HTTP requests snapshot finite binary or multipart data and bounded headers.
No `HttpContent`, `Stream`, `HttpClient`, arbitrary factory/delegate or native
handle crosses the shared contract. Response bodies expose a bounded chunk-read
interface with explicit cancellation and asynchronous disposal. Each returned
chunk is a finite byte snapshot. A concrete transport must enforce requested
chunk, total body/header and deadline limits; partial responses cannot quietly
become success after violating them. Initial finite upload/body hard bounds are
documented and configurable within the contract maximum. Larger/streamed request
sources require a separate qualified profile; they are never silently buffered
without a bound. Multipart construction, resumable upload sequences and SSE/
NDJSON parsing remain provider implementations; each HTTP request still crosses
the injected seam. HTTP responses/streams are process-local capabilities, not
durable or broker-serializable records.

Redirects are disabled by default. An explicit bounded follow mode is only a
request to a supported provider profile; that provider must re-check every
destination and strip sensitive cross-origin headers. Cookies/default credentials
are never implicit. Decompression limits apply to decoded output. Denied model
operations do not resolve credentials or begin HTTP; caller-supplied trust flags
cannot bypass generic HTTP policy. A model-enabled broker and arbitrary HTTP
client use separate injected compositions. A permitted model does not grant
arbitrary networking to its caller.

Policy denial exceptions are neutral and distinct from provider failures and
cancellation. Existing provider 401/403 remains a provider error, not proof of
host policy. Baize must map neutral denial to no default retry/fallback, with a
fresh decision required for any explicitly approved alternative. Response loss
after an accepted paid request does not authorize blind duplicate submission.

## Protocol traces

Model: caller builds typed envelope -> host-bound semantic decorator validates
current request/target/context and every item -> deny with zero credential/HTTP
calls, or allow -> trusted provider resolves its opaque credential profile ->
provider performs injected HTTP operations -> typed result/events preserve usage.
Every new retry, repair, fallback or stream open crosses the model boundary again.
Routing filters and request/handle/context IDs are not authority.

HTTP: caller builds bounded request -> HTTP decorator validates current exact
method/destination/context -> deny without inner dispatch, or allow -> implementation
owns HTTP request/response, enforces timeout/redirect/size policy -> caller reads
bounded chunks until explicit completion and disposes the response on every path.

Job: semantic submit -> admitted provider upload/submit -> bound operation handle;
later status/results/cancel use new envelopes and current authorization. Denying
later access cannot undo a job already accepted. Remote-job cancellation and
active stream revocation/drain require separately reviewed provider guarantees.

## Consequences and qualification

GM-1 proves API/bounds/snapshot behavior, independent dependency closure, neutral
decoration and isolated package consumption. It does not prove a secure Baize
adapter, budget enforcement, credential containment, network isolation or active
stream revocation. Baize migration follows exact indexed package publication;
Hufu adapters follow complete Baize route coverage. Current IO/workflow suites
remain regression checks. New contract tests run on Windows, Linux and macOS
for both frameworks before release; package inspection rejects extra libraries
or runtime dependencies. Publishing uses the existing input-free main workflow,
exact commit/artifacts and NuGet OIDC controls. The user triggers publication.

The [Baize plan](../../../Penghou.Baize/docs/governable-model-http-plan.md) and
[source inventory](../../../Penghou.Baize/docs/governable-model-http-inventory.md)
retain the consumer acceptance gates; the model/HTTP consumer matrix records
the reviewed operation families. The sandbox-parent experiment is already
completed elsewhere and is not part of this delivery.
