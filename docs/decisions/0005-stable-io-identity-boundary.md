# ADR 0005: Stable data-only IO identity boundary

Status: accepted 2026-10-14 for foundation graduation. Qualification and public
release gates remain open. Supersedes ADR 0003's coordinated three-package release
requirement for identity graduation only; provider cooperation remains unchanged.

## Decision

Extract the exact 18-type public dependency closure of IO.Protocols into
`Penghou.IO.Identity.Abstractions`. Preserve `Penghou.IO.Abstractions` namespaces
and declarations. The broader preview IO.Abstractions references this package
and forwards every moved type. IO.Protocols references only Identity.Abstractions.

Graduate Identity.Abstractions and Protocols through `1.0.0-rc.1` to `1.0.0`.
The forwarding IO.Abstractions is `0.1.0-preview.2`; Local stays preview.
All involved assemblies preserve the already published `1.0.0.0` assembly
version. Package versions determine releases, not assembly-version changes.

The new package contains data constructors and value semantics only. It has no
provider, authorizer, writer, web reader, journal, result, physical root, runtime,
or policy interface. Encoding a delete/move/web request does not establish an
implementation or stable operational semantics for those capabilities.

## Rationale and alternatives

Freezing the whole existing Abstractions package would promote unqualified
writer/web/mutation/VFS-facing contracts. Converting that package into a minimal
stable package would force every current capability consumer to migrate before
Protocol graduation, including excluded security-sensitive workstreams.
Removing existing codec overloads would unnecessarily discard versioned data
encodings and consumer compatibility. The demonstrated signature closure
justifies one narrow contract package rather than a generic utility package.

## Consequences

Source namespaces and constructors remain unchanged; old binary references are
resolved by type forwarding in the updated preview facade. Runtime assembly
qualification of moved types now reports Identity.Abstractions. Existing stored
old assembly-qualified names must resolve through the facade; new assembly-qualified
names need the new dependency and are not backward-readable by old packages.
No universal serializer compatibility is promised: JSON data shapes are tested,
and unsupported serializers must be independently qualified by their owner.

V1 bytes, opcodes and path rules remain unchanged. Future identity formats need
distinct version markers and APIs; they must not reinterpret v1. Broad provider
contracts can evolve in preview without changing these fixed data shapes.

Publication of the two stable packages and preview facade is separately gated.
Existing immutable preview versions are never rebuilt or republished.
