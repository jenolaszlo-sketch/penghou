# Roadmap

## Scaffolded

- Neutral typed contracts for bounded workspace reads, metadata, directory
  listing, writes, patches, deletes, directory creation, file moves, and bounded
  web reads.
- Explicit invocation, effect-scope, request identity, action, and resource
  authorization request types.
- Provider contract and architecture documentation, including authorization
  ordering, conditional mutation semantics, and race limitations.

## Pending

- Host/Luban preview resolution and immutable resolved plans, with explicit
  coverage and observed/proposed/unresolved effects; this library owns resource
  requirements rather than the plan API or language executor.
- Whole-known-set admission and a trusted commit barrier before previewable
  mutations; final per-resource checks remain mandatory. See
  [preview/commit requirements](docs/preview-commit-contract.md).
- Qualification for WhatIf with no requested mutations/tool dispatch, incomplete
  coverage blocking, frozen manifests, stale observations, post-start partial
  outcomes and restart revalidation. No new runtime feature is implemented.

- A real provider for workspace files and directories with platform-specific
  path handling, bounds, preconditions, and per-candidate authorization.
- A real web provider with URL and redirect policy, response bounds, and
  authorization of the effective destination.
- Conformance coverage for denial, path binding, per-resource batch checks,
  bounds, and conditional mutations.
- Any Hufu integration, implemented separately from these neutral contracts.

No runtime behavior or enforcement is claimed by this scaffold.
