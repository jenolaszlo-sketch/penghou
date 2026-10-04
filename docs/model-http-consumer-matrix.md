# Model/HTTP consumer closure reviewed for GM-0

Reviewed 2026-10-04 at Baize source `442397ecf25f335309e7f3ab121e9c4359fda100`.
The .NET 8 non-live solution baseline is recorded in the release qualification.
This is source inventory, not evidence that Baize already uses the new contracts.
Canonical owner queue: [Baize plan](../../Penghou.Baize/docs/governable-model-http-plan.md).

| Existing entry point | Provider execution paths | Migration obligation |
| --- | --- | --- |
| `ILlmClient.StreamAsync` | OpenAI/Claude/Gemini/Ollama through `LlmClientBase.StreamAsync`, request/auth construction, named `llm` `SendAsync(ResponseHeadersRead)`, SSE/NDJSON parser | Typed semantic envelope before secret resolution/auth/HTTP; preserve exact streaming events, cancellation/disposal and terminal integrity. |
| `LlmStreamingExtensions.CompleteAsync`, `ILlmCompletionClient.CompleteAsync` | Stream collection ordinarily; native completion branch available to custom clients and forwarding decorators | Cover both branches. Current built-in provider completion generally streams; do not omit the custom/native capability. |
| `LlmRouter.StreamAsync`, `StreamRouteAsync`, completion extensions | `StreamThroughAsync` selects physical endpoints and performs fallback. `Resolve*`/`Explain*` look up routes without inference. Deferred clients resolve secrets before constructing clients. | Each actual dispatch uses wire-model/endpoint metadata and fresh decision. Move governed credential resolution behind admission; registration/decorator construction is not the enforcement point. |
| Structured output/tool repair | `StructuredOutputRepairingLlmClientDecorator`, `LlmStructuredOutputRepairer`, tool repair client/callback extensions | Each additional physical model call crosses the same boundary; policy denial is not availability and does not silently continue fallback/repair. Opaque custom repair callbacks have explicit coverage requirements. |
| `IBaizeBatchClient.SubmitAsync/GetStatusAsync/GetResultsAsync/CancelAsync` | OpenAI, Claude and Gemini batch clients; `BaizeBatchClientBase`, coordinator, endpoint-bound and deferred clients | Per-item contexts and known effects preflight; uploads, submit, polls, result download and cancel use fresh envelopes. Bind handles to physical endpoint/provenance. |
| OpenAI batch submission/results | `/files` multipart JSONL upload followed by create; result-file retrieval and batch cancel/status | Bounded multipart and file reads, all HTTP requests injected. No duplicated paid submission after ambiguous outcome. |
| Gemini batch submission/results | Resumable-upload start, byte upload, batch create; separate result-file download, poll/cancel | Every step respects limits/current bound profile. No arbitrary upload/result URL can inherit trust just from being in a payload. |
| Claude native batches | Direct submit, status/result retrieval, cancel | Preserve result stream/usage and independent operation coverage. |
| `IGenerationClient.SubmitAsync/GetAsync/CancelAsync` | `GenerationClientBase`, OpenAI/Gemini/Runway/fal.ai providers | Cover each supported job/media operation, polling, uploads/results/assets; preserve asynchronous job semantics, idempotency and provider diagnostics. Gemini unsupported lifecycle methods stay explicit. |
| OpenAI generation | Image/image-edit/video/speech submit, video status/delete and asset URL mapping/download | Retain typed media payloads and all provider HTTP/upload/download routes. |
| Runway generation | Text/image-to-video submit, task poll/cancel, ephemeral-upload create and multipart upload | Fresh authorization for actual operations and qualified upload/asset destinations. |
| fal.ai generation | Queue submit/status/result/cancel and generated asset URLs | No uncovered queue/result route; trusted profiles bind effective destination. |
| `GenerationExecutor`, `GenerationBatchExecutor` | Submit and repeated polls; individual/grouped jobs and result aggregation | Mixed item contexts/usage survive batching; no cached policy assumptions in poll loops. |
| Extensions.AI `BaizeChatClient.GetResponseAsync/GetStreamingResponseAsync`, `BaizeImageGenerator.GenerateAsync` | Forward to chat/generation; image generator polls `GetAsync` | Preserve neutral context, intent and actual usage through adapter conversions; never bypass the semantic seam. |
| Diagnostics HTTP handler/content/stream wrappers | Named client pipeline captures HTTP bodies where opted in | Preserve composition/lifetime without default credentials/prompt/response disclosure. Diagnostics do not authorize work. |

Provider HTTP needs JSON and binary GET/POST/DELETE, explicit headers, multipart,
resumable-upload sequences, SSE/NDJSON streaming response reads, bounded assets
and result files, redirects/decompression validation and owned cancellation.
There are no Baize Hufu source/project references in the inspected closure.

Existing `LlmUsage` distinguishes prompt/completion/total/cache-hit/cache-miss/
thinking counts. Preserve those nullable fields individually. Do not add thinking
or caching to already inclusive totals or turn missing values into zero.

GM-2/3/4 must turn these source rows into an executable coverage matrix using
real adapter paths and fake providers. A chat-base edit or injected factory alone
does not satisfy B1-B12 or qualify the consumer's complete authority boundary.
