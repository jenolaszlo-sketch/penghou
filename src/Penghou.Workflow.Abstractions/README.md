# Penghou.Workflow.Abstractions

Product-neutral execution authorization contracts for replaceable workflow
runtimes and authority adapters. Targets .NET 8 and .NET 10 with no package
dependencies. The package contains interfaces and immutable bounded values;
it contains no workflow engine, policy evaluator, SQL or default authorizer.

```csharp
var requirement = new ExecutionRequirement(
    "penghou.execution", 1, "workflow.operation.run", "urn:demo:operation:read");
var identity = new ExecutionIdentity("execution-1", "read", 1, "revision-1");
var context = new ExecutionAuthorizationContext(
    identity, "authorization-request-1", new[] { requirement });

ExecutionAuthorizationResult result =
    await configuredAuthorizer.AuthorizeAsync(context, cancellationToken);
```

The example ends at evaluation. A runtime must validate outcome/request/provider
binding, time, required evidence and its current fence before invoking protected
code. Allowed expires; Denied, ApprovalRequired, Unavailable and Error dispatch
no protected code. Exceptions, cancellation, malformed/unknown results or missing
protected configuration never imply permission. ApprovalRequired parks durably;
a wake-up obtains a fresh decision. Historical Allow is not permission for a retry.

Execution IDs and requirement resource/scope references are host-scoped opaque
values, not authenticated principals or bearer capabilities. A trusted adapter
resolves identity and retained scopes independently. Empty/unknown declarations
must not become implicit permission. Runtime-specific leases and generations
stay in the runtime; actual resource access requires separate provider checks.

Each request snapshots at most 64 requirements. IDs are bounded to 256 UTF-8
bytes, requirement schema/capability to 128, and resource/path to 2048. Strings
are valid Unicode without control characters or surrounding whitespace; values
are preserved without case folding or normalization. Plans require paired ID
and revision. Context/result schema version one rejects unsupported versions.
Use fresh one-use request IDs and never bind one ID to changed context data.

Zhinu is a planned runtime consumer and Hufu.Workflow a planned authority
adapter; neither is a dependency. Other engines can implement the same seam.
See the repository's workflow contract and conformance documents for obligations
and current qualification. Initial preview; publication does not establish
production host security or qualify an unimplemented runtime integration.
