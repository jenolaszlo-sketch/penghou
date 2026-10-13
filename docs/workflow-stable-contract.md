# Workflow.Abstractions 1.0 stable contract and compatibility policy

The stable package is a dependency-free net8.0/net10.0 value/interface seam. Its six public types (80 analyzer entries, including record infrastructure) are intentional. No runtime, persistence, authority implementation, default permission, DI or experimental surface is included.

## Values, ownership, order and errors

Constructors own an immutable snapshot of up to 64 requirements. Order and duplicate declarations are preserved; ordered structural context equality compares identity, request ID and every requirement. Identity/requirement/result equality is record value equality; strings compare ordinal, case-sensitive, without normalization. Context `with { }` shares only immutable owned state. Hash codes are process-local, and ToString is diagnostic; neither is a persisted identity/codec. Equality is not an authorization decision.

IDs/revisions/correlation/evidence/reasons/scope references: 256 UTF-8 bytes maximum; requirement schema and capability: 128; resource and operation path: 2048. Valid Unicode, no controls or surrounding whitespace, and nonblank values are required. Optional null strings mean absence; empty strings are invalid. Attempt is 1..Int32.MaxValue, requirement vocabulary version is positive. Parent differs from execution ID; plan ID/revision are paired. Requirements may be empty, never implicit permission. Constructors reject invalid/null inputs with ArgumentException subclasses (ArgumentNullException for required nulls); exact message text is unspecified.

Results accept only Unavailable=0, Allowed=1, Denied=2, ApprovalRequired=3, Error=4. Nondefault evaluatedAt is normalized to UTC. Expiry must be later, mandatory for Allowed; approval correlation is present exactly for ApprovalRequired. Current-clock validity, finite authorization lifetime, trusted identity/evidence and dispatch fencing are runtime responsibilities.

Immutable instances permit concurrent reads. Callers must not concurrently mutate input collections while constructing a context. Changed/inconsistent counts are rejected, but no atomic snapshot of a concurrently mutated source is promised. There are no locks, retries, crash/reopen or storage guarantees in this package.

## JSON version-one value schema

Fixed interoperable vectors live in `tests/Penghou.Workflow.Tests/vectors`. Use System.Text.Json's default case-sensitive property names and numeric enum representation shown there. Context/result SchemaVersion is 1; missing schemaVersion uses constructor default 1, unknown versions reject. Requirement schemaVersion and identity attempt have no valid missing default. Required references/strings and evaluatedAt are required by constructor invariants; missing required fields/nulls reject. Optional strings, dates, parent/path/plan fields default to null. Missing result Decision defaults to Unavailable, never Allowed. Missing Identity or Requirements rejects; an explicit [] is accepted.

Default System.Text.Json deserialization ignores unknown fields; this is supported only as ordinary value construction, never evidence that an unknown field was understood. Strict cross-process/persisted authorization profiles must use `UnmappedMemberHandling = Disallow` (and default case-sensitive properties/numeric enums), validate duplicate properties before deserialization where ambiguity is forbidden, bound incoming payload bytes, and reject malformed/unknown schema data fail-closed. Ordinary System.Text.Json duplicate matching properties use its last-value behavior; hosts must not interpret that as a canonical encoding. JSON syntax failures throw JsonException; constructor validation exceptions may propagate. Serializer custom naming/converters/permissive numbers are host-specific protocols and outside this schema guarantee.

JSON object property order/whitespace/escaping are unspecified. Requirement array order is meaningful. Date instants retain value after UTC normalization. There is no canonical JSON byte/digest profile. Schema changes/new closed enum outcomes need an explicitly new version/major contract; unknown requirement vocabulary versions remain representable data, requiring adapter recognition before authorization. No 1.x writer silently emits context/result schema 2 or a new decision integer to a schema-one consumer.

## Compatibility and migration

1.x preserves source and binary signatures, constructors (including parameter order/defaults), namespaces, nullable annotations and implemented interfaces. AssemblyVersion remains 1.0.0.0 throughout 1.x; package/file versions identify releases. Minor additions must preserve existing valid input/output behavior, bounds, error categories, ordering and equality and must not add interface members required from existing implementers. Patch releases fix defects within this promise. Breaking signatures, behavior, stricter existing accepted-input validation or schema/equality/bound changes require a major version. Hash/ToString text, allocation and timing are unspecified.

Preview.2 to 1.0 preserves source/binary API, with an intentional context-equality correction: separately constructed equal ordered requirements now compare equal. Consumers using context as Dictionary/HashSet keys must re-evaluate equality assumptions; never persist hashes. Update exact package pins explicitly. Preview.2 and RC unchanged-binary probes prove loading and selected behavior only, not every consumer implementation. Future releases must validate against the published 1.0 package API baseline; the existing preview package validation baseline covers this graduation.

Version-one serialized values require no data migration. Host-owned engine/journal/digest envelopes keep independent versions/migrations. No downgrade of host envelopes or engine storage is promised. Adapters can implement IExecutionAuthorizer without importing Zhinu/Hufu; cancellation, failures and malformed outcomes never grant permission.

## Non-goals and release notes

No Zhinu/Fuwen redesign, authority/security implementation, runtime durability, codec/hash algorithm, transport parser, new framework or capability is included. Hufu/Gagamba are excluded from implementation. Existing runtime/provider resource checks remain independent. Qualification evidence is separate from production-host security acceptance.

1.0 graduation moves all intentional API entries into Shipped, fixes ordered context equality, fixes the assembly compatibility policy, adds permanent schema/ownership/bound tests, and validates exact packed/public artifacts plus unchanged compiled consumers and published Zhinu.
