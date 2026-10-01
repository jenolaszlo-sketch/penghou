# Penghou I/O roadmap

Status: Revised 2026-10-01. Neutral .NET 8/.NET 10 I/O interfaces and provider
requirements are scaffolded. No backend, authorizer, codec, plan/barrier executor
or Hufu adapter is implemented. See the [implementation plan](docs/implementation-plan.md).

## Next delivery

Freeze versioned canonical resource-request identity and provider profile, then
implement/test a real Windows read-only Penghou.IO.Local project alongside the
abstractions. Its first consumer is Luban Read/Find/SearchText. Keep implementations
out of Penghou.IO.Abstractions; no parser or Hufu/CedarSharp dependency blocks it.

## Ordered gates

1. **Contract and read provider.** One neutral canonical identity codec; bounded
   reads/file metadata/direct listing; explicit host authorizer and parent
   bindings; candidate exclusions, paging, versions, cancellation and honest
   path/race guarantees. Real workspace tests on .NET 8 and 10.
2. **Luban migration.** Existing effect checks and typed outputs preserved on
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
