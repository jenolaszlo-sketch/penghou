# Decision 0001: shared concrete-resource boundary

Status: accepted for the initial scaffold

## Context

Luban owns semantic effects and Hufu owns authority and grants. Shared
filesystem and web providers need an independent contract for checking the
concrete resource each operation will touch. A path reference alone cannot
grant access, and semantic authorization cannot by itself prevent a provider
from using a different concrete target.

## Decision

Create `Penghou.IO.Abstractions` as a neutral .NET 8 and .NET 10 library. Every
request includes a host-authenticated invocation with subject, effect, attempt,
scope references, and a concrete backend request identity. Root invocations may
have no parent scope. Request records are data only; they do not authenticate
identifiers or enforce checks. The provider checks each concrete action and
workspace path or URL through an injected authorizer, including reads,
metadata, listing candidates, and each mutation endpoint. Mutations use explicit
version or nonexistence preconditions and return typed results. No runtime
backend or permissive test authorizer ships in this repository.

The host validates the complete pipeline and preflights all known static
effects before I/O; a known denied write blocks upstream reads and artifact
creation. Luban owns semantic typed effects and finite dataflow. Fuwen and
Zhinu own workflow control and durability. This library supplies the final
concrete-target check. The provider snapshots mutable input and verifies the
concrete request identity before authorization. Parent scope ceilings are
retained through authenticated host mappings. A source `FileRef` is provenance
only. Hufu remains independent and may integrate through a separate adapter.

## Consequences

The contracts do not establish confinement against callers or local processes
that bypass the provider. A path-based implementation remains vulnerable to
time-of-check/time-of-use races unless it uses stronger handle-relative
operations. These limits must remain explicit until an actual provider
demonstrates stronger guarantees.

## Alternatives considered

- Put authority and provider behavior in Luban: rejected because other consumers
  need neutral shared I/O contracts and Luban owns semantic effects.
- Return raw streams/native handles: rejected because they escape provider
  bounds and authorization control.
- Use ambient user identity or permit-by-default: rejected because it cannot
  preserve explicit host invocation and inherited scope ceilings.
- Add Hufu as a dependency: rejected because authority integration must not
  couple this contract library to one grant system.
