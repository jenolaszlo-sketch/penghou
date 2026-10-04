# Penghou.Model.Abstractions

Neutral, dependency-free contracts for replaceable model transports. The API describes a semantic model invocation, its host-selected provider/model/endpoint, correlation context, usage intent, optional opaque operation handle, and actual reported usage. It includes separate one-shot and streaming interfaces. Operation names are validated strings so providers can add operations without changing this package.

The contract snapshots metadata and per-item invocation descriptors and bounds those collections. Generic payload types are intentional: payloads must be immutable snapshots of bounded data accepted by the consuming host profile. This library cannot inspect or impose limits on arbitrary `TRequest`, `TResponse`, or `TEvent` values. Consumers must reject unsupported payload types/profiles before dispatch. Payloads must not contain callbacks or executable capabilities. Context values and identifiers are descriptive; they do not authenticate callers or grant access. Operation handles are descriptive references only, and hosts must verify target and context bindings before using them. Credential profile values are opaque references, never credentials.

The package has no runtime dependencies and does not perform provider, HTTP, credential, authority, or budget work.
