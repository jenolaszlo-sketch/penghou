# Product-neutral workflow abstractions

Status: 2026-10-03; WA-1 design is selected and WA-2 implementation is locally
qualified for the preview.2 source release; remote CI remains pending. WA-3 publication remains pending. See the
[contract manual](workflow-authorization-contract.md), [review](workflow-contract-review.md)
and [release checkpoint](workflow-package-release-handoff.md).
This records the user's clarification after the authority-extension
proposal: shared abstractions live in the **Penghou repository** and use a
capability/domain name, not a product name.

## Ownership and dependency boundary

Create `Penghou.Workflow.Abstractions` under
`C:/Users/Laszlos/source/repos/Penghou/src/Penghou.Workflow.Abstractions`.
Its package ID and contract namespace are `Penghou.Workflow.Abstractions`.
This is a new workflow contract package alongside the existing IO packages;
workflow contracts do not belong in `Penghou.IO.Abstractions`.

The package contains the reviewed workflow authorization interfaces, bounded
context/requirements/outcome values and small value invariants. It contains no
workflow engine, policy evaluator, SQL, persistence, DI/default implementation
or provider algorithms. It has no dependency on Zhinu, Hufu, Luban, Cedar,
Biscuit, a host UI or an agent runtime. Any proposed shared-package dependency
must be justified in the contract review; do not assume an IO dependency.

```text
Penghou.Zhinu --------------------> Penghou.Workflow.Abstractions
Penghou.Hufu.Workflow --------------> Penghou.Workflow.Abstractions
             +------------------> Penghou.Hufu
Other workflow engine ----------> Penghou.Workflow.Abstractions
Other authority adapter --------> Penghou.Workflow.Abstractions
```

Zhinu is one runtime implementation. Hufu is one optional authority
implementation. Neither defines the shared contract by product-specific types
or implementation details. Existing Zhinu leases/generations/attempts may inform
required context semantics, but the API expresses neutral execution identities
and fencing requirements. Another conforming runtime can provide equivalent
facts without importing Zhinu internals. Durable state, storage and runtime
validation belong to each runtime, not to the abstractions package.

An authorization outcome is not a native-code sandbox or permission for an
actual resource effect. Resource providers retain their independent current
authority checks and concrete mutation-start guarantees. ApprovalRequired is
an explicit neutral outcome; each runtime owns its durable suspension/resume
implementation and each adapter obtains approval from trusted orchestration.

## Neutral API direction

The selected names are `IExecutionAuthorizer`, `ExecutionAuthorizationContext`,
`ExecutionAuthorizationResult`, `ExecutionRequirement` and execution identity
contracts. The following preserves the user's original design sketch. The
implemented immutable constructor-based API adds a neutral freshness identity
and one-use request binding; see the contract manual and packaged README:

```csharp
public interface IExecutionAuthorizer
{
    ValueTask<ExecutionAuthorizationResult> AuthorizeAsync(
        ExecutionAuthorizationContext context,
        CancellationToken cancellationToken = default);
}

public sealed record ExecutionAuthorizationContext
{
    public required string ExecutionId { get; init; }
    public string? ParentExecutionId { get; init; }
    public required string OperationId { get; init; }
    public string? OperationPath { get; init; }
    public int Attempt { get; init; }
    public string? PlanId { get; init; }
    public string? PlanRevision { get; init; }
    public IReadOnlyCollection<ExecutionRequirement> Requirements { get; init; } = [];
}
```

Zhinu maps a run to ExecutionId/ParentExecutionId and an activity to OperationId,
OperationPath, Attempt and Requirements. The contract exposes neither a Zhinu
activity type nor a database handle. Penghou.Hufu.Workflow implements the
authorization side against these neutral values; another engine can invoke it
without importing Zhinu or changing the adapter. Adapter-specific workflow SQL
or runtime state access is forbidden.

WA-1 defines identifier scope, parent semantics, attempt numbering,
schema/requirements versioning, immutable bounded collection snapshots and
canonical plan/requirements identity. IReadOnlyCollection alone does not make
caller-owned data immutable. Required strings still need validation; null or
empty requirements have explicit fail-closed adapter behavior, not implicit
permission. Bind context to trusted host identity and define neutral freshness/
fencing facts where required. Runtime-specific lease/generation/revision checks
remain runtime-owned. The sketch does not remove these obligations.

Results explicitly distinguish allow, deny, approval-required and unavailable/
error. Unknown/malformed results, provider failures and missing required evidence
never execute protected callbacks. Do not infer approval from a denial reason.

## Related domains and naming scope

The same philosophy applies to Penghou.IO.Abstractions (replaceable physical,
virtual and memory providers or authority decorators). Penghou.Policy.Abstractions
and Penghou.Model.Abstractions are possible future domain contracts for authority
implementations and Baize/direct model providers/routers. They are not authorized
extractions, current delivery gates or new dependencies of Workflow.Abstractions.
Select them only after separate consumer and compatibility reviews.

## Sequential delivery

| Gate | Owner | Dependency | Acceptance |
| --- | --- | --- | --- |
| WA-1 | Penghou contracts; Zhinu/Hufu review input | None | Review neutral types, bounds, exact identity/requirements, closed outcomes, error/cancellation semantics, compatibility and alternative-runtime implementability. Use ZA-1 dispatch/state review as evidence; do not ship runtime mechanics as contracts |
| WA-2 | Penghou | WA-1 and reviewed ZA-1 | Implement the isolated project/package, API inventory, compatibility/conformance probes, .NET 8/10 builds and negative dependency checks. A consumer implementing the seam without Zhinu or Hufu must compile. Integrate build/test/pack and input-free main publication into Penghou CI |
| WA-3 | Penghou release owner | WA-2 | Qualify package contents and fresh-cache consumers, then the user publishes the contract package through Penghou CI. Record the actual immutable version; do not assume a version or treat local candidates as published |
| ZA-2/3/4/6 | Zhinu | WA-3 | Consume the exact published contract package, implement and qualify default/provider registration, dispatch checks, durable approval/retry/resume, evidence/fencing, then release Zhinu runtime/provider packages separately |
| HA-1/2/3 | Hufu | WA-3 and completed Zhinu runtime phase ZA-6 | Implement the optional adapter against only Hufu and the published neutral contracts, run separate runtime integration tests, qualify and release Hufu separately |
| LW-1 | Luban integration owner | WA-3; relevant runtime/adapter candidates for composed tests | Review additive Luban operation/requirement mapping against the neutral contract through an optional host/adapter or neutral hook. Preserve language/IO boundaries, exact plan identity, semantic admission and live per-resource checks; no mandatory Zhinu/Hufu dependency in Luban |

This is the selected work order: **contracts and their publication, then Zhinu,
then Hufu integration**. Contract design uses source review and minimal isolated
consumer probes before publication; that review is not concurrent runtime or
adapter implementation. Existing independent Hufu cleanup may remain ready,
but does not advance the workflow integration ahead of these phases.

The [cross-project plan](../../Penghou.Zhinu/docs/authority-extension-plan.md)
and [activity queue](../../Penghou.Zhinu/docs/authority-extension-activities.md)
retain ZA/HA compatibility, dispatch, approval and legacy-profile gates.
WA owns shared contract delivery; ZA owns Zhinu implementation; HA owns Hufu.
No bulk installation of the old Hufu staging snapshot is authorized.

## Compatibility and scope

Do not blindly rename or relocate all existing public Zhinu types. Inventory
each extraction candidate and preserve existing consumers with viable type
forwarders and old-binary/source probes, or record intentional preview breaks.
The neutral namespace is the target for new shared contracts; retained legacy
namespaces/forwarders are compatibility mechanisms, not new product-bound
abstractions. Keep engine behavior such as WorkflowContext in its runtime.

The original [user proposal](../../Penghou.Zhinu/docs/proposals/2026-10-03-authority-extension.md)
remains unchanged as historical input. Its `Penghou.Zhinu.Abstractions` name
and Zhinu repository ownership are superseded by this clarification.
IO/Luban package boundaries and deferred VFS/WhatIf work are unaffected.

Read this plan first for ownership, naming and publication order, then the
cross-project plan for behavioral requirements. Update all three roadmaps and
handoffs with actual WA/ZA/HA evidence. The current implementation request ends
with the preview.2 version bump, commit and push; the user publishes NuGet before
integration implementation resumes. The next scope includes Zhinu, Hufu.Workflow
and the additive Luban LW-1 integration, keeping product implementations separate.
