# IO identity graduation release handoff

Updated 2026-10-14. The exact RC artifacts and public packages passed all
Windows/Linux/macOS qualification gates on .NET 8/10. Final stable publication
and its public-package gates remain open.

## Release set and lineage

| Package | Candidate | Stable target | Scope |
| --- | --- | --- | --- |
| Penghou.IO.Identity.Abstractions | 1.0.0-rc.1 | 1.0.0 | Exact 18-type data-only prerequisite |
| Penghou.IO.Protocols | 1.0.0-rc.1 | 1.0.0 | Existing stateless v1 identity/path algorithms |
| Penghou.IO.Abstractions | 0.1.0-preview.2 | Remains preview | Forwarding facade plus existing provider/capability contracts |
| Penghou.IO.Local | Existing 0.1.0-preview.1 | Remains preview | No implementation or publication change |

Stable source promotion changes Identity/Protocols versions to 1.0.0 and uses
a new facade preview version (preview.3) if its dependency metadata changes.
Never repack/reuse the published preview.2 facade after its dependency changes.
The graduated family contains only Identity and Protocols; the facade is a
compatibility bridge outside the stable promise.

## Preview migration notes

Namespaces, all 18 data declarations and helper overloads are preserved. V1
digests/path canonicalization are unchanged. The record types move to the
new assembly; the new preview facade forwards old type references. All DLLs
are deployed together, including the new Identity DLL. Old assembly-qualified
type names resolve; newly emitted names require the new dependency and do not
work on old preview installations. JSON data shape preservation is qualified;
arbitrary external serializer contracts are not claimed.

Luban's published preview.1 exact package pins still select original IO packages.
The probe deploys the original unchanged public Local/Luban DLLs with updated
dependencies and unchanged previously compiled application code. This proves
the exercised binary/behavior compatibility, not ordinary Luban NuGet adoption.
A metadata-only future Luban preview can update pins independently, without
reactivating its parked feature work or holding this foundation release open.

## Qualification and supported claims

Windows 11 build 26200, .NET 8.0.31/10.0.12, SDK 10.0.401:

- Protocol-only source suite: 38/38 on each framework, no skips.
- Existing Local source suite: 73/73 on each framework, no skips. Only its
  architecture test changes to allow the exact stable data-only dependency.
- Independent Python encoding: 15 fixed byte/hash vectors covering all eight
  opcodes, Unicode, continuation, preconditions, bounds and URL canonicalization.
- Exact source-bound package graph: stable Identity + Protocols only, 15 vectors
  on both frameworks, fresh isolated package cache and local feed.
- Original public Local/Luban DLLs: read, metadata/list, conditional byte write,
  journal start/completion and denial behavior on both frameworks.
- All 18 old assembly-qualified types, read-request JSON shape/restoration and
  unchanged app DLL hash: passed on both frameworks.
- Updated shared CI preview closure: four ephemeral packages verified; fresh
  package-only smoke passed both frameworks. No Local stable release is implied.

Full machine-readable artifacts/hashes/commands are retained under the local
`artifacts/io-exact-source-proof` evidence directory. Those local checks do not
prove public candidate availability or Linux/macOS execution.

## Remaining release gates

RC source `9ff78f2b840444c717a281464351e200171357a2` is qualified by
[release run 38439907657](https://github.com/jenolaszlo-sketch/penghou/actions/runs/38439907657),
successful attempt 2. Attempt 1's public jobs reached the one-hour indexing
deadline after successful uploads; only failed verification jobs were retried,
using the original qualified artifacts. Public nupkg entries match exactly,
excluding only NuGet's repository signature. Permanent RC evidence and raw
public/qualified archive hashes are in [rc-release.json](io-qualification/rc-release.json)
and the three OS records in `io-qualification/rc`.

Strict SDK package validation against public `1.0.0-rc.1` passed locally for
the final metadata candidate. Its API/code/vector files are unchanged. Stable
qualification additionally preserves and runs an unchanged RC-compiled consumer
alongside the original preview-compiled consumer.

1. Review and integrate the final metadata after coordinated Workflow main changes.
2. Dispatch trusted publisher `publish.yml`, family `IOProtocols`, from selected main.
3. Reusable `io-graduation.yml` packs once, then qualifies those same archives on
   Windows/Linux/macOS, both frameworks. Native Local/Luban is Windows-only.
4. Publish only qualified Identity/Protocols candidates and preview forwarding facade.
5. Compare public payloads with the exact artifact (NuGet signature excluded), then
   run fresh public package restoration and unchanged-binary qualification on all OSes.
6. Only after RC gates pass: promote versions, require compatible baseline, qualify
   exact final artifacts, publish stable and record tags/remote run evidence.

No Hufu/Gagamba implementation, IO.Local graduation, capability redesign,
additional providers, VFS, new codecs or features are part of this release.
