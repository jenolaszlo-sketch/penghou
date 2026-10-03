# Product-neutral workflow abstractions

Status: 2026-10-03; WA-1/2/3 are complete. Exact package version
`0.1.0-preview.2` is published; CI, publication and fresh-cache public-feed
consumer qualification passed. See the
[contract manual](workflow-authorization-contract.md), [review](workflow-contract-review.md)
and [release checkpoint](workflow-package-release-handoff.md), plus the
[public-package qualification record](workflow-public-package-qualification.json).
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
| WA-2 | Penghou | WA-1 and reviewed ZA-1 | Complete. Source commit `5a76b7c`; all seven CI jobs passed in [run 37115430526](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37115430526). |
| WA-3 | Penghou release owner | WA-2 | Complete. Published `0.1.0-preview.2`; all four publication jobs passed in [run 37116694209](https://github.com/jenolaszlo-sketch/penghou/actions/runs/37116694209). Exact public package contents match CI apart from the repository signature; fresh-cache NuGet-only consumers pass on .NET 8/10. See the [qualification record](workflow-public-package-qualification.json). |
| ZA-2 | Zhinu | WA-3 | Complete. Zhinu source adoption pins exact published `Penghou.Workflow.Abstractions` `0.1.0-preview.2`; core package pin and source builds pass on .NET 8/10. The fresh-GUID empty-cache graph passed using seven exact CI packages at `0.1.0-preview.15-ci.wa3.20261003`; the workflow package and external dependencies restore from public NuGet only. Both builds and runs pass, and no Hufu package resolves. See the [adoption checkpoint](../../Penghou.Zhinu/docs/workflow-package-adoption.md). |
| ZA-3A/3B/4 | Zhinu | ZA-2 | Implemented and locally qualified in candidate `0.2.0-preview.1`; see the current [Zhinu authorization qualification](../../Penghou.Zhinu/docs/qualification/workflow-authorization.json). The 927-case ZA-2 record remains historical evidence for package adoption. |
| ZA-6 | Zhinu | ZA-3A/3B/4 and preview.15 compatibility qualified | Commit/push and user-publish qualified Zhinu `0.2.0-preview.1` through its NuGet CI workflow with remote CI. Publication remains pending. |
| HA-1/2/3 | Hufu | WA-3 and completed Zhinu runtime phase ZA-6 | Implement the optional adapter against only Hufu and the published neutral contracts, run separate runtime integration tests, qualify and release Hufu separately |
| LW-1 | Luban integration owner | WA-3; relevant runtime/adapter candidates for composed tests | Review additive Luban operation/requirement mapping against the neutral contract through an optional host/adapter or neutral hook. Preserve language/IO boundaries, exact plan identity, semantic admission and live per-resource checks; no mandatory Zhinu/Hufu dependency in Luban |

This is the selected work order: **published contracts, completed Zhinu ZA-2,
then locally qualified Zhinu ZA-3A/3B/4, ZA-6 publication, then Hufu
HA-1/2/3**. ZA-2's exact
published-package source adoption and fresh seven-package consumer closure
passed. The runtime qualification is recorded in the Zhinu candidate; publication
remains pending.
Independent Hufu HA-0A/B cleanup may proceed, while
the older staged Hufu snapshot remains on hold. Luban LW-1 is optional
neutral-host integration; its language core remains independent of this contract
and of Zhinu/Hufu. Package qualification does not establish runtime acceptance.

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

Read this plan first for ownership and naming, then the cross-project plan for
behavioral requirements. WA-1/2/3 and Zhinu ZA-2 source adoption are complete;
commit/push and user-publish Zhinu through ZA-6, then Hufu HA-1/2/3. Keep package
qualification distinct from implementation/runtime acceptance. Luban LW-1 is
optional neutral-host integration and does not make workflow authorization a
language-core dependency.
