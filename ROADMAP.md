# Penghou I/O roadmap

Status: Revised 2026-10-01. Neutral .NET 8/.NET 10 I/O interfaces, canonical
identity codec, Windows read-only Local provider, and a narrow existing-file
Local NTFS patch profile are implemented. Luban uses the shared reader and its
standalone exact-target executor. The full IWorkspaceWriter surface, web,
general multi-target admission/barrier, production authority policy, and Hufu /
Zhinu adapters remain pending. See the [implementation plan](docs/implementation-plan.md).

## Delivered foundation and next gates

Versioned [request identity](docs/canonical-request-identity.md) and the
[Windows read-only profile](docs/local-reader-profile.md) now support Luban
Read/Find/SearchText. Luban's typed read language/static preflight and capture-only WhatIf are
implemented. The narrow Local patcher plus Luban's separate single-target
executor implement one standalone write slice; broad writer APIs, batch
admission/recovery, and governed adapters remain future gates.

## Ordered gates

1. **Contract and read provider — complete.** One neutral canonical identity codec; bounded
   reads/file metadata/direct listing; explicit host authorizer and parent
   bindings; candidate exclusions, paging, versions, cancellation and honest
   path/race guarantees. Real workspace tests on .NET 8 and 10.
2. **Luban migration — complete.** Existing effect checks and typed outputs preserved on
   the real shared reader. Document a pinned sibling-checkout integration build
   rather than depend on unpublished packages or duplicate contract code.
3. **Single-file patch profile — implemented.** The Local provider supports one
   existing-file original-version UTF-8 byte patch on qualified fixed-drive
   NTFS. Luban's standalone executor requires host admission, live resource
   checks, a serialized start fence and journaled outcomes. It is not a general
   IWorkspaceWriter implementation. Broader batches, recovery, and governed
   Hufu execution remain pending; missing guarantees remain Unsupported.
4. **Broader resource profiles.** Additional metadata/mutations and both-endpoint
   rights, preconditions and recovery; opaque versions are not assumed content hashes.
5. **Governed/durable adapters and web.** Hufu/Zhinu integration after their own
   prerequisites; bounded GET with qualified URL/endpoint/redirect/SSRF/decompression
   and deadline behavior. Supervisor support is separately qualified.

Luban/the host owns language, static preflight, resolution, immutable plans,
whole-batch admission and commit barrier. Providers preserve concrete request/
plan bindings and final live resource checks. WhatIf invokes no mutations or
opaque tools; incomplete coverage and denials never downgrade to lazy execution.
The barrier is not a multi-file transaction. See the
[preview/commit contract](docs/preview-commit-contract.md).

Use Luna for bounded implementation/tests where possible, review identity and
native consistency gates, and release packages only after qualification and
separate release authorization. No enforcement claim follows from interfaces.
