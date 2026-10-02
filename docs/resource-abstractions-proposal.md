# Penghou Resource Abstractions Architecture

## 1. Purpose

The Penghou abstraction packages define **replaceable capability boundaries** between domain components and external resources.

The immediate focus is filesystem and directory access.

The goal is to allow components such as Luban to perform resource operations without knowing:

- how the operation is physically implemented;
- whether the resource is local or virtual;
- whether Hufu is enforcing authority;
- whether execution is real or simulated;
- whether writes are direct, staged, or in-memory.

This enables:

- loose coupling between Penghou packages;
- Hufu enforcement without contaminating domain packages;
- provider substitution;
- deterministic testing;
- future virtual filesystems;
- future WhatIf execution against isolated resources.

The abstraction projects are **contract assemblies**, not shared utility libraries.

---

# 2. Core architectural rule

For a resource operation, separate four responsibilities:

```text
WHAT operation is requested
    -> Penghou resource abstractions

WHY the operation is performed
    -> domain component such as Luban

WHETHER the operation is allowed
    -> Hufu

HOW the operation is physically performed
    -> provider such as Penghou.IO.Local
```

These responsibilities must remain separate.

---

# 3. Intended dependency structure

For filesystem access:

```text
                     Luban
                       |
                       v
             Penghou.IO.Abstractions
                       ^
                       |
            +----------+----------+
            |                     |
      Penghou.Hufu.IO       Penghou.IO.Local
       authorization          implementation
        decorator
            |                     ^
            +----------+----------+
                       |
                 provider chain
```

A typical runtime composition may be:

```text
Luban
  ↓
HufuAuthorizedWorkspace
  ↓
LocalWorkspace
  ↓
System.IO
```

Luban must not know that Hufu or Local exists.

Local must not know that Hufu or Luban exists.

Hufu should be able to wrap any conforming provider.

---

# 4. Abstractions are contract-only

`Penghou.IO.Abstractions` may contain:

- interfaces;
- request records;
- response/result records;
- logical path types;
- resource identities;
- resource versions;
- metadata DTOs;
- operation enums;
- capability descriptions;
- bounds;
- preconditions;
- neutral error/outcome types.

It must not become literally interface-only if supporting contract types are required.

It must not contain substantive implementation behavior.

---

# 5. What does not belong in the abstraction package

Do not place the following in `Penghou.IO.Abstractions`:

- diff algorithms;
- merge algorithms;
- patch application;
- text transformation;
- generic helper implementations;
- filesystem utility classes;
- OS path helpers;
- HTTP helpers;
- retry algorithms;
- authorization logic;
- Cedar logic;
- Biscuit logic;
- Hufu grants;
- workflow logic;
- Git commands;
- provider implementation;
- global service locators.

In particular, public static implementation classes are generally a design smell in an abstraction project.

Examples that should not exist there:

```text
MergeHelper
DiffUtility
FileHelpers
PathHelpers
HttpHelpers
WorkspaceFile
VirtualFileSystem.Current
```

Move behavior to the domain that owns it.

---

# 6. Luban ownership

Luban owns:

- diff;
- merge;
- patches;
- language semantics;
- command parsing;
- IR;
- effect semantics;
- conflict resolution;
- text transformations;
- preview semantics.

Luban may use I/O abstractions to obtain or persist data.

For example:

```text
Luban merge operation

read base     -> IWorkspaceReader
read ours     -> IWorkspaceReader
read theirs   -> IWorkspaceReader

perform merge -> Luban

write result  -> IWorkspaceWriter
```

The merge algorithm remains Luban functionality.

The I/O abstraction knows nothing about merge semantics.

This agrees with the earlier assessment that diff/merge are bounded text-change semantics rather than resource authority.

---

# 7. Hufu ownership

Hufu owns:

- authority;
- grants;
- delegation;
- revocation;
- approval;
- policy evaluation;
- Cedar;
- Biscuit;
- workflow authority;
- authority diagnostics;
- enforcement.

Hufu should integrate with resource abstractions using decorators/adapters.

Conceptually:

```csharp
public sealed class AuthorizedWorkspaceReader : IWorkspaceReader
{
    private readonly IWorkspaceReader inner;
    private readonly IAuthorityEvaluator authority;
}
```

For each operation:

```text
request
  ↓
authorize exact resource operation
  ↓
deny OR delegate to provider
```

A denied operation must never invoke the wrapped provider.

---

# 8. Hufu must not leak into neutral contracts

Do not put Hufu-specific types into `Penghou.IO.Abstractions`.

For example, do not require:

```text
Grant
BiscuitToken
CedarPolicy
WorkflowRevision
Approval
AuthorityEnvelope
```

in filesystem requests.

Instead:

```text
I/O abstraction
    describes the operation

Hufu adapter
    maps the operation into authority semantics
```

This allows the same provider to run:

- with Hufu;
- without Hufu;
- under tests;
- under another security mechanism.

The existing assessment already identified this boundary: shared resource contracts should not depend on Hufu, Luban, Baize, durable stores, or a UI.

---

# 9. Do not wrap System.IO one-for-one

Avoid designing contracts as thin versions of:

```csharp
File.ReadAllText(...)
File.WriteAllText(...)
Directory.GetFiles(...)
new FileStream(...)
```

The abstraction represents **logical resource capabilities**.

It must remain implementable by systems that do not have physical operating-system files.

Avoid neutral APIs such as:

```csharp
Stream OpenRead(string physicalPath);

FileInfo GetFileInfo(string path);

DirectoryInfo GetDirectoryInfo(string path);
```

These leak physical filesystem assumptions.

Prefer explicit asynchronous requests and results.

Conceptually:

```csharp
ValueTask<FileReadResult> ReadAsync(
    FileReadRequest request,
    CancellationToken cancellationToken);
```

---

# 10. Logical workspace paths

Public APIs must distinguish logical workspace paths from physical OS paths.

A logical path might be:

```text
/src/Penghou.Luban/Runtime.cs
```

It must not require a corresponding path such as:

```text
C:\Users\...\Runtime.cs
```

A future virtual provider may have no physical path.

Use or introduce an explicit value concept such as:

```csharp
WorkspacePath
```

with defined semantics for:

- normalization;
- root;
- equality;
- traversal;
- case behavior;
- canonical representation.

Do not implicitly resolve against:

```text
Environment.CurrentDirectory
```

or another ambient process concept.

---

# 11. Physical path mapping belongs to providers

The Local provider may translate:

```text
WorkspacePath
      ↓
physical path
```

This translation belongs to `Penghou.IO.Local`.

It must not become part of neutral resource identity.

This matters because future providers may map the same logical resource into:

- memory;
- a Git tree;
- an overlay;
- remote storage;
- a container;
- another provider.

---

# 12. Resource identity and versions

Do not assume that resource identity is merely a string path.

Preserve explicit concepts for:

```text
logical path
resource identity
resource version
```

where existing contracts already provide them.

This becomes especially important for:

- concurrency;
- overlays;
- snapshots;
- approval delays;
- Git-backed resources;
- remote resources.

Mutation operations should support version/precondition checks where relevant.

Example:

```text
replace Foo.cs
only if Foo.cs is still version 123
```

Do not silently downgrade this to an unconditional write.

---

# 13. Capability-oriented interfaces

Prefer narrow interfaces.

Conceptually:

```csharp
IWorkspaceReader
IWorkspaceWriter
IWorkspaceDirectoryReader
IWorkspaceDirectoryWriter
IWorkspaceMetadataReader
IWorkspaceMutator
```

An aggregate interface may exist:

```csharp
IWorkspaceFileSystem :
    IWorkspaceReader,
    IWorkspaceWriter,
    IWorkspaceDirectoryReader,
    IWorkspaceMetadataReader
```

but consumers should depend upon the smallest useful capability.

For example:

```csharp
DiffEngine(IWorkspaceReader reader)
```

rather than:

```csharp
DiffEngine(IWorkspaceFileSystem everything)
```

This improves:

- loose coupling;
- least privilege;
- testing;
- future provider composition.

---

# 14. Providers may support different capabilities

Do not assume every workspace implementation supports every operation.

Potential future providers include:

```text
LocalWorkspace
InMemoryWorkspace
SnapshotWorkspace
OverlayWorkspace
GitWorkspace
RemoteWorkspace
FilteredWorkspace
```

Examples:

```text
SnapshotWorkspace
    read       yes
    enumerate  yes
    write      no
    delete     no

OverlayWorkspace
    read       yes
    write      yes
    delete     yes
    commit     maybe

RemoteWorkspace
    read       yes
    stream     maybe
    atomic move maybe not
```

Providers must expose unsupported behavior honestly.

Never silently substitute weaker semantics.

---

# 15. Bounds are contract semantics

Agent-accessible resource operations must remain bounded.

Requests may need limits such as:

```text
maximum bytes
maximum result count
maximum directory entries
maximum traversal depth
maximum mutation size
```

These bounds must not disappear because a particular provider could technically process more data.

They are part of safe capability execution.

---

# 16. Explicit outcomes

Unsupported or incomplete behavior must produce explicit outcomes.

Potential examples:

```text
UnsupportedCapability
UnsupportedOperation
VersionConflict
ResourceNotFound
AccessDenied
LimitExceeded
IncompleteEnumeration
ResourceChanged
```

Use existing Penghou outcome conventions where applicable.

Never silently fall back to:

```text
System.IO
shell commands
another provider
weaker preconditions
unbounded access
```

---

# 17. Streams

Do not make raw `Stream` the default resource abstraction.

Prefer bounded data operations.

Raw streams create difficult questions around:

- lifetime;
- ownership;
- authorization duration;
- limits;
- partial reads;
- cancellation;
- concurrent mutation.

If streaming becomes necessary later, design an explicit streaming capability.

Do not introduce one simply to resemble `System.IO`.

---

# 18. Local provider

`Penghou.IO.Local` owns physical implementation.

It may contain:

- OS path resolution;
- link handling;
- physical metadata;
- native filesystem behavior;
- file handles;
- atomic filesystem operations;
- platform-specific behavior.

It depends on:

```text
Penghou.IO.Abstractions
```

It must not depend on:

```text
Penghou.Luban
Penghou.Hufu
```

---

# 19. Luban injection

Neutral Luban runtime code must not directly construct Local implementations.

Code such as:

```csharp
new LocalWorkspaceReader(...)
```

inside neutral runtime paths should be replaced with injected contracts.

Composition belongs in:

- host wiring;
- adapters;
- integration packages;
- dependency injection configuration.

The previous assessment already identified direct Local construction inside Luban as a dependency worth removing.

---

# 20. Immediate Hufu integration

Provide a Hufu I/O decorator or equivalent integration boundary.

Prove at minimum:

```text
authorized read
    -> underlying provider invoked

denied read
    -> provider never invoked

authorized write
    -> provider invoked

revocation between calls
    -> later call denied
```

This supports lazy operation-time authorization.

Preflight checks remain useful but do not replace runtime checks because dynamic resources may only become known during execution.

---

# 21. HTTP and other capabilities

The same architectural model may later apply to other resources.

Possible examples:

```text
Penghou.Http.Abstractions
Penghou.Process.Abstractions
```

For example:

```text
Luban/agent
   ↓
HTTP capability
   ↓
Hufu authorization
   ↓
HTTP provider
```

Do not make filesystem abstractions responsible for HTTP.

Do not put every contract into one giant `Penghou.Abstractions` assembly.

---

# 22. Routing

Routing may also benefit from contract extraction but remains domain-specific.

Examples:

```text
Penghou.Baize.Router.Abstractions
Qingniao execution-provider contracts
```

Do not create a universal `IRouter`.

LLM routing, resource routing, and execution-provider selection have different semantics.

The earlier assessment correctly distinguished Qingniao's delegated executor selection from Baize model routing.

---

# 23. Immediate scope

The immediate task is deliberately narrow.

Implement:

1. correct `Penghou.IO.Abstractions`;
2. remove misplaced implementation code;
3. move diff/merge back to Luban;
4. move Local behavior to `Penghou.IO.Local`;
5. inject I/O contracts into Luban;
6. preserve Hufu interception capability;
7. add architecture tests;
8. preserve existing bounds, identities, versions and failure semantics.

Do not currently perform a general Penghou reorganization.

---

# 24. Explicit non-goals for this iteration

Do not currently implement:

- virtual filesystem;
- overlay filesystem;
- snapshot filesystem;
- in-memory production filesystem;
- Git filesystem;
- remote filesystem;
- VFS mounts;
- VFS transactions;
- WhatIf execution through VFS;
- filesystem change journals;
- generic provider middleware;
- HTTP abstraction redesign;
- process abstraction redesign;
- Baize reorganization;
- SQLite provider restructuring;
- ecosystem-wide package migration.

These belong on the roadmap.

---

# 25. Why VFS belongs on the roadmap

A virtual filesystem is not merely a convenience provider.

It enables several future Penghou capabilities that fit the architecture especially well.

Most importantly:

## Controlled agent workspaces

Agents can operate on logical resources without receiving unrestricted access to the physical repository.

## In-memory execution

Workflows and tools can be tested without creating temporary physical files.

## Snapshots

A workflow can operate against a stable point-in-time resource view.

## Overlays

Writes can be captured without immediately altering the real workspace.

## WhatIf execution

A workflow can execute against a disposable virtual environment and reveal what it actually attempts to change.

## Authority validation

Hufu can compare declared authority with dynamically observed resource operations.

## Approval before application

Virtual changes can be reviewed before they are committed to physical resources.

Therefore VFS is a deliberate future architectural direction.

The current abstraction design must not prevent it.

---

# 26. VFS roadmap

## Phase V1: In-memory workspace

Implement a genuine in-memory provider conforming to the I/O contracts.

Goals:

- deterministic tests;
- no temp directories;
- no OS dependency;
- provider contract verification;
- isolated execution.

This is the first proof that abstractions have not leaked Local filesystem assumptions.

---

## Phase V2: Snapshot workspace

Provide an immutable point-in-time resource view.

Conceptually:

```text
Physical workspace
       ↓
Snapshot T
       ↓
workflow
```

Goals:

- stable reads;
- reproducibility;
- deterministic workflow execution;
- version consistency.

---

## Phase V3: Overlay workspace

Implement a writable layer over another workspace.

Conceptually:

```text
             Visible workspace
                    |
           +--------+--------+
           |                 |
       writable           read-only
        overlay              base
```

Lookup behavior:

```text
resource exists in overlay
    -> overlay wins

resource deleted in overlay
    -> appears absent

otherwise
    -> read base
```

The base provider remains unchanged.

---

# 27. Overlay change representation

The overlay should eventually be capable of producing a logical change set such as:

```text
Added
Modified
Deleted
Moved
```

For example:

```text
Added:
    /src/NewParser.cs

Modified:
    /src/Parser.cs

Deleted:
    /src/OldParser.cs
```

This change representation is not Luban diff/merge.

It represents resource state changes.

Luban may optionally convert resource changes into textual diffs later.

---

# 28. Operation journal

Do not rely solely upon before/after filesystem state.

A future virtual workspace should be able to record **attempted operations**.

Example:

```text
READ   /src/A.cs
WRITE  /src/A.cs
WRITE  /src/A.cs
DELETE /src/B.cs
WRITE  /.git/config -> denied
```

This matters because final state can hide behavior.

For example:

```text
create X
delete X
```

produces no final delta but is still meaningful execution evidence.

Therefore future VFS work should consider both:

```text
operation journal

and

resulting resource delta
```

---

# 29. WhatIf using VFS

A major planned VFS use case is realistic WhatIf execution.

Conceptually:

```text
Real workspace
      ↓
snapshot/base
      ↓
overlay VFS
      ↓
Hufu
      ↓
workflow execution
```

Filesystem mutations happen normally from the workflow's perspective.

They affect only the overlay.

After execution, Penghou can inspect:

```text
operations attempted
operations denied
resources read
resources written
resources deleted
resulting virtual state
final delta
```

No physical filesystem mutation is required.

---

# 30. WhatIf becomes dynamic conformance testing

The workflow plan already describes expected behavior and required authority.

VFS-backed WhatIf can compare that declaration with actual execution.

Three sets become important:

```text
DECLARED

what the plan says it needs


OBSERVED

what execution actually attempted


RESULTING

what state actually changed
```

Example:

```text
Declared:

read  /src/**
write /src/Feature/**


Observed:

read   /src/**
write  /src/Feature/New.cs
delete /tests/OldTests.cs
write  /.git/config


Resulting:

created /src/Feature/New.cs
deleted /tests/OldTests.cs
```

Hufu can identify:

```text
delete /tests/OldTests.cs
write /.git/config
```

as authority or plan drift.

---

# 31. WhatIf enforcement mode

One future WhatIf mode should execute using the exact proposed Hufu authority.

Example:

```text
workflow attempts:
    write /.git/config

Hufu:
    DENIED
```

The provider is not invoked for that operation.

The workflow receives the same denial it would receive during real execution.

This answers:

> Would this workflow successfully execute with the authority currently proposed?

---

# 32. WhatIf observation mode

A second future diagnostic mode may record operations that would normally be denied while allowing the disposable virtual simulation to continue.

This must be clearly separate from enforcement mode.

Purpose:

> What authority would this workflow actually attempt to consume if permitted?

Example:

```text
Declared:

write /src/**


Observed:

write  /src/A.cs
delete /temp/cache.dat
write  /logs/debug.txt
```

This can reveal under-declared or unexpected behavior before real execution.

Observation mode must never accidentally become the semantics of real execution.

---

# 33. WhatIf report

A future report might contain:

```text
WhatIf Report

Workflow:
    parser-repair

Activity:
    update-parser

Declared authority:
    read  /src/parser/**
    write /src/parser/**

Observed operations:
    14 reads
    3 writes
    1 delete
    1 denied write

Resulting delta:
    2 modified files
    1 deleted file

Unexpected authority:
    delete /tests/**
    write /.git/config

Policy result:
    FAIL

Reason:
    execution attempted operations outside declared authority
```

This turns the workflow plan into something dynamically testable.

---

# 34. Why WhatIf should use the normal abstractions

Do not build WhatIf by sprinkling logic such as:

```csharp
if (dryRun)
{
    ...
}
```

through every Luban operation.

Instead:

```text
normal Luban behavior
       ↓
same abstractions
       ↓
different provider
```

For example:

```text
Real execution:

Luban
 -> Hufu
 -> Local


WhatIf:

Luban
 -> Hufu
 -> Overlay
 -> Snapshot
```

This minimizes special-case behavior and makes simulation closer to real execution.

---

# 35. Important limitation of VFS WhatIf

A VFS simulates only resource effects routed through the filesystem abstraction.

It does not automatically virtualize:

- HTTP;
- processes;
- databases;
- MCP calls;
- messages;
- arbitrary native code;
- external services.

Therefore do not claim:

```text
VFS WhatIf = complete workflow sandbox
```

It is one important layer.

---

# 36. Longer-term generalized WhatIf architecture

Other capabilities may later receive equivalent abstraction/provider layers.

Conceptually:

```text
              WhatIf Environment

Filesystem   -> overlay VFS
HTTP         -> intercepting/simulated provider
Processes    -> sandbox/provider
Database     -> disposable/transactional provider
MCP/tools    -> proxy/simulation layer

                    |
                    v

                  Hufu

                    |
                    v

         declared vs observed effects
```

VFS is the first practical resource domain for exploring this architecture.

---

# 37. Filtered workspace views

A future VFS capability may expose only a projection of another workspace.

Example underlying workspace:

```text
/src
/tests
/secrets
/.git
```

Visible workspace:

```text
/src
/tests
```

A filtered workspace complements Hufu but does not replace it.

Distinguish:

```text
Hufu
    decides whether an operation is authorized

Filtered VFS
    decides what resource view is exposed
```

Both can be composed.

---

# 38. Virtual mounts

Future work may explore mapping multiple providers into one logical workspace.

For example:

```text
/workspace/src
    -> Local provider

/workspace/generated
    -> In-memory provider

/reference
    -> Remote read-only provider
```

Do not implement mount semantics now.

However today's logical path model must not assume:

```text
one workspace == one physical root forever
```

---

# 39. Git-backed workspace

A future Git provider could expose:

```text
tree at commit
staged overlay
immutable historical snapshot
```

through generic resource contracts.

Git-specific concepts remain outside the base abstraction:

```text
branch
HEAD
index
commit
rebase
```

The generic workspace abstraction should not become a Git API.

---

# 40. Remote workspace

Future providers may represent:

- container workspaces;
- remote agent sandboxes;
- object stores;
- HTTP-backed repositories;
- remote development environments.

This is another reason to keep APIs asynchronous and avoid physical file handles.

Do not assume:

- local latency;
- POSIX/Windows locking;
- physical paths;
- local atomic rename;
- unbounded enumeration.

---

# 41. VFS is not a security sandbox

A virtual filesystem can reduce exposure and provide controlled resource views.

It is not sufficient to contain arbitrary executable code.

Longer-term containment may involve:

```text
Agent
   ↓
Luban
   ↓
Hufu
   ↓
VFS / filtered workspace
   ↓
OS/container sandbox
   ↓
physical host
```

Each layer solves a different problem.

Do not treat logical filesystem virtualization as proof of process containment.

---

# 42. Workspace instances must not be global

Different workflows may eventually need different workspaces simultaneously.

Example:

```text
Workflow A

snapshot A
overlay A
authority A


Workflow B

snapshot B
overlay B
authority B
```

Therefore do not create:

```text
Workspace.Current
VirtualFileSystem.Current
GlobalFileSystem.Instance
```

Workspace instances must be explicitly supplied.

---

# 43. Provider conformance testing

Create reusable contract tests where practical.

Possible common behaviors:

```text
path semantics
read
missing resource
bounds
metadata
enumeration
write
create
delete
move
version conflict
unsupported capability
cancellation
```

Each provider can then prove conformance to the portions of the contract it supports.

Provider-specific semantics receive separate tests.

---

# 44. Architectural review questions

For every public type or method added to `Penghou.IO.Abstractions`, ask:

### Could an in-memory provider implement this honestly?

If not, investigate the abstraction leak.

### Could the resource exist without an OS path?

If not, investigate the path model.

### Could a read-only snapshot implement the relevant subset?

If not, determine whether the capability should be optional.

### Could an overlay intercept this without touching physical storage?

If not, investigate provider assumptions.

### Could Hufu authorize the request without understanding Local?

If not, the neutral operation probably lacks sufficient resource identity.

### Could a remote implementation implement this asynchronously?

If not, inspect local assumptions.

### Could two workspaces use this API concurrently?

If not, eliminate ambient/global state.

These questions are architectural tests.

They do not require implementing the future providers now.

---

# 45. Immediate corrective procedure

## Step 1: Inventory

List every public type currently in `Penghou.IO.Abstractions`.

Classify each as:

```text
Contract
Local implementation
Luban semantic behavior
Hufu behavior
Other implementation
```

Do not change code during this first pass.

---

## Step 2: Correct ownership

Move:

```text
diff / merge / patch
    -> Luban

physical filesystem logic
    -> Penghou.IO.Local

authorization implementation
    -> Hufu integration

generic helper implementation
    -> owning domain or internal implementation
```

Leave neutral contracts in Abstractions.

---

## Step 3: Verify contract closure

Ensure interfaces bring only the neutral DTO/value types necessary to use them.

Avoid pulling implementation assemblies transitively into the abstraction package.

---

## Step 4: Inject into Luban

Replace neutral Luban runtime construction of Local providers with injected contracts.

---

## Step 5: Add Hufu decorator tests

Verify:

```text
allow
deny
provider not invoked when denied
mid-run revocation
exact requested resource preserved
```

---

## Step 6: Protect future provider independence

Add architecture tests preventing accidental dependencies from Abstractions into:

```text
Penghou.IO.Local
Penghou.Luban
Penghou.Hufu
Penghou.Baize
```

---

# 46. Immediate acceptance criteria

The correction is complete when:

1. `Penghou.IO.Abstractions` contains contracts rather than algorithms.

2. Diff and merge behavior is owned by Luban.

3. Generic static implementation helpers are absent from the abstraction package.

4. Luban uses injected I/O contracts.

5. Neutral Luban code does not construct Local providers.

6. `Penghou.IO.Local` depends only on the contracts it implements, not Luban/Hufu.

7. Hufu can wrap a provider without requiring changes to the provider.

8. Denied operations never reach the wrapped provider.

9. Mid-run authority changes affect subsequent operations.

10. Public resource identity does not require physical absolute paths.

11. Existing bounds remain explicit.

12. Existing resource versions/preconditions remain explicit.

13. Unsupported semantics return explicit results.

14. No global current workspace abstraction is introduced.

15. A future in-memory implementation is structurally possible.

16. A future snapshot implementation is structurally possible.

17. A future overlay implementation is structurally possible.

18. No VFS implementation is required to complete this correction.

---

# 47. Roadmap

Record the following future work explicitly.

## VFS-1: In-memory workspace

Build a complete provider suitable for deterministic testing.

Primary purpose:

- prove provider independence;
- eliminate temp-file-heavy tests;
- establish provider conformance suite.

---

## VFS-2: Snapshot workspace

Provide immutable stable resource views.

Primary purpose:

- reproducible execution;
- stable workflow inputs;
- version-aware analysis.

---

## VFS-3: Overlay workspace

Capture writes/deletes/moves over a base provider.

Primary purpose:

- safe staged agent changes;
- no immediate mutation of underlying resources.

---

## VFS-4: Operation journal and resource delta

Record:

```text
attempted operations
authorization decisions
resulting state changes
```

Primary purpose:

- evidence;
- diagnostics;
- authority analysis;
- later provenance integration.

---

## VFS-5: WhatIf execution

Run workflows against snapshot + overlay composition.

Primary purpose:

- execute realistic workflows without physical filesystem effects;
- observe actual resource behavior before approval.

---

## VFS-6: Declared vs observed authority analysis

Compare:

```text
workflow-declared resource needs

versus

resource operations actually attempted
```

Primary purpose:

- detect unexpected agent behavior;
- detect under-declared plans;
- validate least-privilege authority requests.

---

## VFS-7: Apply approved overlay

Apply an accepted virtual delta to the real provider.

Investigate:

- version conflicts;
- authority revalidation;
- partial failure;
- atomicity;
- compensation;
- durable recovery.

---

## VFS-8: Filtered workspace

Provide resource projections/restricted views.

Primary purpose:

- expose only relevant resources;
- reduce accidental access;
- complement Hufu logical authority.

---

## VFS-9: Git-backed provider

Explore immutable Git tree views and staged workspaces.

Primary purpose:

- agent work against repository versions without mandatory checkouts;
- integration with Luban diff/merge and later Git tooling.

---

## VFS-10: Remote/sandbox provider

Explore remote agent workspaces and OS/container sandbox integration.

Primary purpose:

- move from logical containment toward stronger physical isolation.

---

# 48. Why VFS is worth doing

VFS should not be viewed as infrastructure for its own sake.

It creates an important progression:

```text
today:

agent asks to modify real files
        ↓
Hufu decides whether to allow it
```

Later:

```text
agent executes against virtual resources
        ↓
Penghou observes what actually happened
        ↓
Hufu compares execution with the plan
        ↓
human/system can inspect resulting delta
        ↓
approved effects are applied to reality
```

That changes WhatIf from:

> What does the agent say it intends to do?

into:

> What did the workflow actually try to do when executed in a controlled environment?

That is substantially stronger evidence.

---

# 49. Final ownership rule

Use these questions when placing code.

### Does it describe a resource capability?

Put it in the relevant abstraction package.

### Does it physically perform the capability?

Put it in a provider.

### Does it decide whether the capability is permitted?

Put it in Hufu.

### Does it interpret the capability as part of language/domain semantics?

Put it in Luban or the relevant domain.

### Does it stage, overlay, snapshot, project or virtualize resource state?

It likely belongs in a future VFS/provider layer.

### Is it simply reusable implementation code?

That alone is not sufficient reason to put it in an abstraction package.

The abstraction boundary exists so Penghou components can ask for capabilities without knowing who implements them, who governs them, or whether the resources are physical at all.