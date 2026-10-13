# Stable IO identity and protocol contract

Target: Identity.Abstractions and Protocols `1.0.0-rc.1` → `1.0.0`.
No stable/public release is claimed before exact-artifact and public-consumer gates.

## Stable profile

Identity.Abstractions contains exactly the 18 data types inventoried in
[Phase 0](io-phase0.md). They do not validate operational authorization, select
providers, authenticate IDs, perform I/O or guarantee mutation outcomes. Record
constructors retain the current public parameter order, names and defaults.
Strings use ordinary ordinal record equality. `Uri` uses framework Uri equality;
ReadOnlyMemory<byte> uses segment equality, not byte-content equality.
Record equality is not canonical request identity equality.

Constructors intentionally permit default/null/invalid data. Codec methods
validate fields they encode; providers must validate every operational field.
HostInvocation is data only and may be null for computation because its fields
are excluded. It is never permission. `FileWriteRequest.Content` borrows caller
memory: callers must not mutate its backing storage during computation; Compute
copies it once and retains no aliases. A provider must independently freeze the
input it authorizes and executes. `with` does not copy borrowed buffers.

## Protocol v1

[Canonical encoding](../canonical-request-identity.md) is normative. SHA-256
input, opcodes and per-operation field ordering are fixed; opcode 05 stays
reserved. Digest identities are lowercase 64-digit hexadecimal under the exact
`penghou-io:request:v1:sha256:` prefix. No decoder is exposed: a cryptographic
identity is not reversible. Encode→decode claims therefore do not apply.

Paths use an empty root and slash-relative segments, preserving original
spelling in Normalize. ToIdentityPath folds ASCII a-z to A-Z only. No Unicode
normalization, percent-decoding, escape interpretation or filesystem lookup
occurs. Non-ASCII case and canonically equivalent Unicode spellings remain
distinct. Both helpers apply Windows-profile syntax identically on every OS;
they do not mean that a Unix provider supports those identity equivalences.
Limits, reserved names, separators and rejection rules are executable vectors.
Paths are at most 2048 UTF-16 code units; strings require valid Unicode scalars.

Workspace IDs preserve exact spelling, are nonblank/control-free and at most
256 UTF-16 code units. Required delete/move version strings additionally have a
1024-byte UTF-8 ceiling. General encoded strings, including continuation and
precondition version, have a 16384-byte UTF-8 ceiling. File/web MaxBytes accepts
0 through 16777216. Write content is at most MaxBytes and 16777216 bytes. This is
a payload bound, not a bound on the entire envelope, which also contains fields.
List bounds and precondition enum values are encoded as int32 without semantic
validation. Nullable precondition versions preserve null versus empty. Operational
providers reject invalid bounds/combinations independently.

Null requests/required subobjects throw ArgumentNullException; malformed encoded
values throw ArgumentException (including strict UTF-8 fallback exceptions).
Exception text and parameter labels are unspecified. No network or filesystem
access occurs. Calls retain no state and can run concurrently with frozen inputs.
Crash/reopen semantics are not applicable to a stateless codec.

Web encoding accepts absolute HTTP/HTTPS Uri without user info and uses
`GetComponents(HttpRequestUrl, UriEscaped)`: scheme/host/default port canonicalization,
escaped request target and fragment exclusion follow supported .NET 8/10 Uri.
Compatibility vectors fix representative observable results. This is not DNS,
SSRF, credentials, redirect, HTTP transport or authority behavior.

## Compatibility and migration

1.x preserves existing constructors, methods, parameter defaults, generated
record members, value semantics, binary loading, fixed v1 bytes and supported
valid-input behavior. Patches fix defects without reinterpreting v1 or removing
accepted contract inputs. Additive minor APIs cannot require existing consumers
to implement new capabilities. Incompatible data shapes or semantics require
a major version; a new protocol schema has a distinct marker even within a
future additive release. AssemblyVersion stays 1.0.0.0 throughout 1.x.

Preview users update Protocols to stable, keep capability Abstractions preview
at least preview.2, and obtain Identity.Abstractions transitively. No source
namespace migration is required. Old compiled applications must deploy the new
Protocol DLL, forwarding Abstractions DLL and Identity DLL together; retaining
the old Abstractions DLL creates two definitions of moved types and is unsupported.
New assembly-qualified type names are not readable by the old preview release.
No blanket promise covers arbitrary external serializers; JSON shape preservation
and resolution of old assembly-qualified names are explicit probes.

Published Luban preview.1 pins both original IO package versions exactly. Its
unchanged DLL can use the forwarded deployment, as the binary probe demonstrates,
but its ordinary NuGet graph needs a metadata-only consumer release before it
can restore this boundary normally. That adoption is not claimed by the DLL
probe and does not graduate or reactivate Luban's parked capability work.

## Non-goals and release gates

No IO.Local/provider, writer/web/journal capability graduation, security-sensitive
consumer implementation, VFS, new format, generalized Unicode case equivalence,
physical namespace confinement, storage durability or performance rewrite.
The release passed shipped API and strict SDK RC-baseline validation, exact
nupkg/hash/commit recording, Windows/Linux/macOS net8/net10 vectors, fresh public
package-only behavior, unchanged preview/RC binaries/type forwarding, and the
bounded unchanged Windows Local/Luban deployment probes. Future 1.x packages
retain an unconditional stable 1.0.0 SDK baseline plus the shipped API gate.
Ordinary Luban NuGet graph adoption remains separate and is not claimed.
