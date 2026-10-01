# Penghou I/O roadmap

Status: Revised 2026-10-01. Neutral .NET 8/.NET 10 I/O interfaces and provider
requirements, canonical identity codec and the Windows read-only Local provider
are implemented. Luban uses the shared reader. Writer/web, authority policy,
plan/barrier executor and Hufu adapter remain pending. See the [implementation plan](docs/implementation-plan.md).

## Delivered foundation and next gates

Versioned [request identity](docs/canonical-request-identity.md) and the
[Windows read-only profile](docs/local-reader-profile.md) now support Luban
Read/Find/SearchText. The next Luban slice is typed IR/static preflight; the next
resource-provider slice is a qualified writer after resolution and barrier readiness.

## Ordered gates

1. **Contract and read provider — complete.** One neutral canonical identity codec; bounded
   reads/file metadata/direct listing; explicit host authorizer and parent
   bindings; candidate exclusions, paging, versions, cancellation and honest
   path/race guarantees. Real workspace tests on .NET 8 and 10.
2. **Luban migration — complete.** Existing effect checks and typed outputs preserved on
   the real shared reader. Document a pinned sibling-checkout integration build
   rather than depend on unpublished packages or duplicate contract code.
3. **Qualified writer.** After Luban resolution/admission/barrier is available,
   implement one existing-file original-version byte patch and prove supported
   check/version/commit consistency. Missing guarantees remain Unsupported.
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
