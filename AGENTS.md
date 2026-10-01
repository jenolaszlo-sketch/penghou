# Penghou.IO contributor guidance

Keep this repository an independent, neutral I/O contract library. Do not add
dependencies on Luban, Hufu, Fuwen, Zhinu, a host UI, MCP, or an agent runtime.
Luban owns semantic typed effects and finite dataflow; Fuwen and Zhinu own
workflow control and durability. This library defines contracts for a
provider's final checks against concrete resources.

Every operation requires host-authenticated invocation, subject, effect,
attempt, scope references, and request identity. Contract records are data only:
they do not authenticate those IDs or enforce checks. The host resolves scope
references against retained mappings, preserves the parent ceiling, and
preflights all known static effects before I/O. The provider snapshots mutable
inputs, verifies the exact request identity, and makes a final current-authority
check for each concrete resource. There is no ambient-user lookup or default
permit. Batch operations authorize each source and destination separately.
File references establish provenance only; they do not convey permission.

Keep APIs bounded and typed. Do not expose raw streams, native handles, shell or
process escape hatches, arbitrary callbacks, or an unbounded System.IO mirror.
Do not add a runtime backend or a fake authorizer as scaffolding. Document
provider preconditions and limits before introducing behavior. Hufu remains
independent; any adapter belongs in a separate integration package.
