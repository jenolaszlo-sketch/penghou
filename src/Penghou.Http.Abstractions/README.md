# Penghou.Http.Abstractions

Independent, product-neutral .NET 8/.NET 10 contracts for replaceable outbound
HTTP transports. They contain no HttpClient implementation, authorization system,
credential store or dependency on Baize/Hufu/IO/workflow packages.

`IHttpTransport.SendAsync` accepts an immutable exact request with finite binary
or multipart body snapshots, bounded headers, descriptive context and explicit
limits. Responses own an asynchronously disposable body capability; reads return
finite copied chunks and explicit completion. No raw streams or HttpContent
objects cross the contract. The initial body ceiling is 1 GiB with 64 MiB default
request/response limits; chunks are at most 64 KiB. Providers must enforce total
decoded bytes, requested chunk/header limits, timeouts, redirects and cancellation.

Redirects default to disabled. Explicit redirect counts do not grant destination
access; each redirect still requires current concrete policy. No ambient cookies
or credentials are implied. Secret headers/URLs/bodies must not be logged by
default. Context/request IDs are data and never authenticate a caller.

This generic HTTP capability is distinct from model invocation. A host can deny
arbitrary HTTP while permitting model calls through a separately trusted provider
broker. Contracts alone do not enforce that topology or isolate arbitrary code.

Implementations must deny before dispatch, preserve exact request/result binding,
fail unsupported guarantees explicitly and dispose underlying HTTP resources on
every path. Large streaming upload sources require a separate reviewed profile;
the finite multipart contract never reads a local filename or callback.

See the repository's `docs/decisions/0004-model-http-transport-contracts.md` and
the model/HTTP release checkpoint for qualification and consumer adoption status.
