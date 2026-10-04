# Product-neutral workflow abstractions

Updated 2026-10-04. WA-1/2/3 and Zhinu ZA-2/3A/3B/4/6 are complete.
`Penghou.Workflow.Abstractions` 0.1.0-preview.2 and all seven Zhinu
0.2.0-preview.1 packages are published and indexed. Zhinu source commit
`2f02a2e91d87e6429fd17a3819308301ab91f17c` passed both OS jobs in
[CI 37137640422](https://github.com/jenolaszlo-sketch/penghou-zhinu/actions/runs/37137640422)
and [publication 37138352675](https://github.com/jenolaszlo-sketch/penghou-zhinu/actions/runs/37138352675).
All seven public packages were downloaded; hashes and exact repository commit
metadata are recorded in [public-release evidence](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/zhinu-public-release.json).

Hufu HA-0A/B review and test isolation are complete; HA-1 is implemented.
The optional adapter depends only on Hufu and the exact neutral contract.
The current local source suite passed 816 cases, 408 per .NET 8/10 framework
(210 core, 93 Biscuit, 19 IO, 22 legacy, 52 Workflow unit, 12 integration).
The bounded request-preflight telemetry slice adds 26 cases per framework.
A finite worker queue emits closed categories/timing without request metadata;
listener loss, saturation and shutdown preserve mandatory evidence/results. See
[telemetry profile](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/optional-telemetry.md)
and [current evidence](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/optional-telemetry.json).
The 764-case explanation checkpoint remains historical evidence. Earlier work
was committed as Hufu 42a045b, Penghou 77bac95 and Zhinu a1df6e9; the telemetry
delivery is local, with push/remote CI and user-controlled Hufu publication open.
The bounded typed-path explanation slice adds 32 cases per framework, with
actual evaluator capture and separately authorized redacted disclosure. See
[profile](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/decision-explanations.md)
and [explanation evidence](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/decision-explanations.json).
The earlier 700-case core checkpoint remains historical evidence.
Independent core admission/issuance adds 73 cases per framework and no engine
dependency; see [the profile](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/core-admission-and-issuance.md)
and [earlier core qualification](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/core-hardening.json).
The earlier 554-case workflow qualification remains historical evidence.
HA-2 fresh candidate-package qualification passed on both frameworks; HA-3
remote CI and user-run Hufu publication remain open. No Hufu package or production
host is claimed published. See the [Hufu handoff](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/zhinu-authority-handoff.md) and
[qualification ledger](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/workflow-authorization.json).

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
| ZA-2 | Zhinu | WA-3 | Complete. Zhinu source adoption pins exact published `Penghou.Workflow.Abstractions` `0.1.0-preview.2`; core package pin and source builds pass on .NET 8/10. The fresh-GUID empty-cache graph passed using seven exact CI packages at `0.1.0-preview.15-ci.wa3.20261003`; the workflow package and external dependencies restore from public NuGet only. Both builds and runs pass, and no Hufu package resolves. See the [adoption checkpoint](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/workflow-package-adoption.md). |
| ZA-3A/3B/4 | Zhinu | ZA-2 | Implemented and locally qualified in candidate `0.2.0-preview.1`; see the current [Zhinu authorization qualification](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/qualification/workflow-authorization.json). The 927-case ZA-2 record remains historical evidence for package adoption. |
| ZA-6 | Zhinu | Complete | All seven 0.2.0-preview.1 packages are indexed; exact source/CI/publication metadata verified in [public-release evidence](https://github.com/jenolaszlo-sketch/penghou-hufu/blob/main/docs/qualification/zhinu-public-release.json). |
| HA-1/2/3 | Hufu | WA-3 and ZA-6 complete | HA-0A/B, HA-1 and local HA-2 complete; remote CI and user-run HA-3 publication remain. Six Hufu candidates remain unpublished. |
| LW-1 | Luban integration owner | WA-3; relevant runtime/adapter candidates for composed tests | Review additive Luban operation/requirement mapping against the neutral contract through an optional host/adapter or neutral hook. Preserve language/IO boundaries, exact plan identity, semantic admission and live per-resource checks; no mandatory Zhinu/Hufu dependency in Luban |

The selected order remains published neutral contracts, published Zhinu,
then Hufu. The first two phases are complete. Hufu HA-0A/B and HA-1 are
complete; fresh package-backed HA-2 qualification also passed locally.
HA-3 remote CI and user-run publication remain. The old snapshot is reviewed by
change group, never bulk-installed. Luban LW-1 remains optional host integration
and never creates a mandatory engine or authority dependency in the language.

The [cross-project plan](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/authority-extension-plan.md)
and [activity queue](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/authority-extension-activities.md)
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

The original [user proposal](https://github.com/jenolaszlo-sketch/penghou-zhinu/blob/main/docs/proposals/2026-10-03-authority-extension.md)
remains unchanged as historical input. Its `Penghou.Zhinu.Abstractions` name
and Zhinu repository ownership are superseded by this clarification.
IO/Luban package boundaries and deferred VFS/WhatIf work are unaffected.

Read this plan for contract ownership, then the cross-project activity queue
and current Hufu handoff. Do not repeat published WA/ZA phases. Preserve
independent IO/Luban package boundaries, current resource checks and deferred
VFS/WhatIf scope. Package evidence is distinct from production host acceptance.
