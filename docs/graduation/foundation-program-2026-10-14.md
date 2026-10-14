# Penghou foundation graduation program

Program date: 2026-10-14, Asia/Manila. This records the completed foundation graduation
program following the earlier dependency assessment. All four foundations are
published as stable 1.0.0, indexed, qualified and tagged. Each progressed through
a public RC, exact artifact checks, clean public consumers and unchanged-binary
proofs. No graduation blocker remains.

| Component | Stable version | Release source | Public package status | Compatibility evidence | Consumer evidence | Deferred work |
|---|---|---|---|---|---|---|
| Siming family: core, Sqlite, Cryptography, Testing, Verify | 1.0.0 | v1.0.0 → e1c116df26e83f2944cf521909494e603aa282b7 | Published, indexed, qualified and tagged; artifact/publication run 38485011888 and public-only run 38487368097 | 492 shipped entries; RC API baseline; net8 exact artifacts on Windows/Linux/macOS; retained-data and unchanged-binary proofs; 108 contract tests | Guyabano 14 original session/checkpoint tests and unchanged adapters; published Hongxian.Sqlite preview.5; actual Marang fingerprint path | Native AOT linking, new engines/formats/signers, replication and retention policy |
| Workflow.Abstractions | 1.0.0 | workflow-v1.0.0 → 1c856745c58339b0ea190ff6d639f97dc65c0861 | Published, indexed, qualified and tagged; run 38484433010 passed | 80 shipped entries; fixed schema vectors; owned immutable requirements; ordered structural equality; RC binary proof, three OSes/net8+net10 | Neutral package consumer and published Zhinu authorization boundary, exact-pin override explicitly qualified | Engine/authority semantics, canonical digest, downstream runtime graduation |
| Cangjie current store: core, Sqlite, Testing | 1.0.0 | v1.0.0 → 3af06c58e2b283d8286e53e85c663144a39bab51 | Published, indexed, qualified and tagged; run 38484745880 passed | Bounded profile; shipped API and actual RC SDK baseline; schema4; independent searches/pinned snapshot traversal; multi-process and killed-process proofs; Windows/Linux/net10 | Eight original Guyabano tests; unchanged cross-store restart consumers; current published Zhinu probe | Cursor API, new storage engines, migrations, retention policy, macOS certification |
| IO.Identity.Abstractions → IO.Protocols | 1.0.0 each | io-identity-v1.0.0 → b6a2c4acd301508afcb465e4f96836d3aaa0913c | Published, indexed, qualified and tagged; run 38485095225 passed | Exactly18 data types; stable-only two-package graph; fixed canonical byte/hash vectors; lexical platform-independent identity; three OSes/net8+net10; unchanged preview and RC binaries | Legacy Local/Luban assemblies, old assembly-qualified type names and JSON shape; deployment proof qualified separately from ordinary NuGet adoption | Providers, writers, web operations, mutation recovery and IO.Local |

## Decisions and release boundaries

- Use 1.0.0 for an explicit source, binary and behavioral compatibility promise;
  persisted/wire schemas have their own stable versions. AssemblyVersion remains
  1.0.0.0 in these package lines. Breaking constructor/interface/semantic changes
  require a deliberate major or separately versioned format, not routine minor bumps.
- Preserve existing Siming epochs1/2/3, canonical JSON1/2 and SQLite formats.
  Close mutable aliases, append ownership, cleared-key lifetime and ambiguous
  checkpoint input before RC. Verifier embedding APIs are intentionally stable.
- Stabilize Workflow's bounded independent value boundary without importing Zhinu
  engine or authority behavior. Equality compares requirements structurally in
  declared order; serializer permissiveness is explicitly separated from strict
  host authorization profiles.
- Stabilize Cangjie's current bounded local store. No cursor was invented: separate
  searches remain separate selections; one persisted ordered snapshot supports
  deterministic traversal/restart. Application retention remains application-owned.
- Extract only the IO identity DTO closure. Type forwarding preserves the old
  namespace/assembly resolution through the preview facade. Protocols has no stable
  dependency on Local or speculative operational interfaces.
- Pack once per release source; qualify that retained artifact set on every claimed
  platform before trusted publication. Compare all public ZIP entry names/bytes,
  excluding only NuGet's repository signature; qualify clean public caches and
  unchanged compiled consumers again. Indexing timeouts never permit promotion or
  replacement of a published package. Tags follow final public proof.

## Deliberate exclusions and remaining previews

The four stable runtime graphs contain no Penghou preview prerequisite. The broad
IO.Abstractions facade remains 0.1.0-preview.3 as a compatibility bridge; IO.Local
remains preview. Luban's old exact pins require explicit adoption work: its bounded
unchanged-assembly deployment probe is SUPPORTED WITH QUALIFICATION, while ordinary
NuGet adoption is NOT PROVEN. Zhinu's old Workflow exact pin has the same distinction.

Fuwen, Zhinu, Qingniao, Guihua, Hetu and Hongxian retain their existing preview
status and independent architectural work. They were consumers/context only.
Hufu and Gagamba received no implementation changes. No new features, storage
engines, frameworks, consensus, replication, proofs or performance rewrite belongs
to this program.

## Evidence and scope

Repository documents are normative; immutable release runs and per-platform JSON
identify the exact source/package/runtime/consumer scope. Source tests, local packs,
remote qualified artifacts and signed public archives are distinguished. An archive
signature changes the raw ZIP hash, so payload equality is reported separately.
Consumer proofs certify the exercised boundaries, never entire downstream products.

- Siming: [exact artifacts and publication](https://github.com/jenolaszlo-sketch/penghou-siming/actions/runs/38485011888), [successful public requalification](https://github.com/jenolaszlo-sketch/penghou-siming/actions/runs/38487368097), [per-platform records](https://github.com/jenolaszlo-sketch/penghou-siming/blob/572a08b61766d902b9b16fad0b955ddb3e66fabb/docs/evidence/1.0.0/summary.json).
- Workflow: [successful release](https://github.com/jenolaszlo-sketch/penghou/actions/runs/38484433010), [per-platform records](https://github.com/jenolaszlo-sketch/penghou/blob/b6bf9fceafb56ccc144a02ae0e12e7371cb7d797/docs/qualification/workflow-1.0.0/summary.json).
- Cangjie: [successful release](https://github.com/jenolaszlo-sketch/penghou-cangjie/actions/runs/38484745880), [qualification report](https://github.com/jenolaszlo-sketch/penghou-cangjie/blob/58afc674b0b756c21b13e98bf5a994bc5becc078/docs/graduation-evidence.md).
- IO: [successful release](https://github.com/jenolaszlo-sketch/penghou/actions/runs/38485095225), [per-platform records](https://github.com/jenolaszlo-sketch/penghou/blob/b6bf9fceafb56ccc144a02ae0e12e7371cb7d797/docs/graduation/io-qualification/stable-release.json).

Siming's original final public Ubuntu/Windows jobs failed only because NuGet's
tool-discovery metadata lagged its public archive availability; macOS passed.
A public-only controller checked out the exact original release source,
downloaded the retained hash-bound artifacts, and passed every full public gate
on all three OSes. It packed/published nothing and preserved the failed records.
The original release-run failure is retained honestly beside the successful
supplemental proof. No contract/test/payload failure was retried or bypassed.

Release evidence bookkeeping commits are Siming
572a08b (release tag remains e1c116d), Workflow ee2f1c5, Cangjie 58afc674 and
IO/shared Penghou b6bf9fce. Release worktrees are clean under Solo/graduation;
original dirty documentation checkouts were preserved. Tags identify exact
artifact sources rather than subsequent evidence-only commits.

Maintainer attention is required only for a real defect, deliberate future contract
change or downstream adoption that is explicitly outside these scoped proofs.
No maintainer action is required to finish this graduation program. The
foundations are closed; deferred capabilities do not block their stable contracts.
