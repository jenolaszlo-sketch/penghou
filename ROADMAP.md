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

- A real provider for workspace files and directories with platform-specific
  path handling, bounds, preconditions, and per-candidate authorization.
- A real web provider with URL and redirect policy, response bounds, and
  authorization of the effective destination.
- Conformance coverage for denial, path binding, per-resource batch checks,
  bounds, and conditional mutations.
- Any Hufu integration, implemented separately from these neutral contracts.

No runtime behavior or enforcement is claimed by this scaffold.
