# Canonical resource request identity, schema v1

`ResourceRequestIdentity.Compute` identifies one concrete backend request. It
does not authenticate a host invocation, subject, scope, effect, or permission.
The provider snapshots mutable request data before hashing and uses that same
snapshot for authorization and I/O. Invocation fields and the request identity
field itself are excluded to avoid recursion; the host and authorizer bind the
computed child request to its authenticated parent effect separately.

## Digest envelope

The digest input begins with these bytes, exactly:

1. UTF-8/ASCII bytes for `Penghou.IO.ResourceRequestIdentity`.
2. One zero byte.
3. The schema version as signed 32-bit little-endian integer (`1`).
4. The operation opcode as one byte.
5. Operation-specific fields in the order below.

The output is lowercase hexadecimal SHA-256 prefixed by
`penghou-io:request:v1:sha256:`. The prefix is a representation of the digest,
not part of the hashed envelope. The leading domain and version separate this
format from other hashes and future schema versions.

All integers are signed 32-bit little-endian. A string is a signed 32-bit
little-endian byte length followed by strict UTF-8 bytes without a BOM. A byte
sequence uses the same length prefix followed by the raw bytes. Nullable strings
use marker `00` for null or marker `01` followed by the ordinary string
encoding. A null value and an empty string therefore have different encodings.

Workspace IDs are exact opaque strings: they are validated as nonempty,
bounded, and control-free but are not case-folded. Workspace paths are first
validated by `WindowsWorkspacePath.Normalize`; reserved device-name stems are
checked before the first extension, after trimming spaces and periods from the
stem. The returned path preserves its original case. For identity only, its value is converted with
ASCII `a` through `z` are converted to `A` through `Z`; every other Unicode
code point is preserved exactly and the resulting value is encoded as a string.
This intentionally conservative rule avoids assuming that .NET Unicode
uppercasing matches every Windows filesystem's upcase table. Thus ASCII-only
case variants share an identity, while non-ASCII case variants may remain
distinct even on a case-insensitive filesystem. The Windows provider must
reject directories configured for case-sensitive name lookup before relying on
ASCII case-folded identity. This codec performs no filesystem access.

Web request URLs must be absolute HTTP or HTTPS URIs without user information.
Their encoded value is `Uri.GetComponents(UriComponents.HttpRequestUrl,
UriFormat.UriEscaped)`. This canonical request-target representation excludes a
fragment because fragments are not sent to an HTTP server.

## Operation field order and opcodes

| Opcode | Request | Fields after the envelope |
| --- | --- | --- |
| `01` | `FileReadRequest` | Workspace string; normalized path string; `MaxBytes` int32 |
| `02` | `FileMetadataRequest` | Workspace string; normalized file path string |
| `03` | `DirectoryListRequest` | Workspace string; normalized path string (root allowed); `MaxEntries`, `MaxCandidatesScanned`, `MaxOutputBytes` int32; nullable continuation value |
| `04` | `FileWriteRequest` | Workspace string; normalized file path string; `MaxBytes` int32; content bytes; write precondition |
| `05` | `FilePatchRequest` | Workspace string; normalized file path string; expected version string; `MaxPatchCount`, `MaxReplacementBytes`, `MaxOutputBytes` int32; patch count int32; each patch's `StartOffset` int32, `DeleteLength` int32, replacement bytes |
| `06` | `FileDeleteRequest` | Workspace string; normalized file path string; expected version string |
| `07` | `DirectoryCreateRequest` | Workspace string; normalized non-root path string |
| `08` | `FileMoveRequest` | Workspace string; normalized source path string; normalized destination path string; expected source version string; destination write precondition |
| `09` | `WebReadRequest` | canonical HTTP request URL string; `MaxBytes` int32 |

A write precondition is its `WritePreconditionKind` numeric value as int32,
then its nullable version string. Current values are `MustNotExist = 0` and
`MustMatchVersion = 1`. Providers separately validate that the combination is
supported and enforce it atomically; an identity does not validate or grant it.

Patch byte ranges and replacement payloads are encoded exactly as supplied,
after snapshotting. The codec does not sort patches, translate unified diff,
normalize UTF-8, or validate range semantics; providers validate patches
against the exact original version.

## Golden vectors

These vectors are generated from the envelope and primitive encodings above,
independently of the C# codec. They should remain fixed for schema v1.

| Request | Canonical fields | Identity |
| --- | --- | --- |
| `FileReadRequest` | opcode `01`; workspace `ws`; path `src/Foo.txt` (identity path `SRC/FOO.TXT`); `MaxBytes = 4096` | `penghou-io:request:v1:sha256:c3f2dec4a7a5f1df22441d28c427781d5612d0a5b079a0aac5bcf0c8cef7087b` |
| `DirectoryListRequest` | opcode `03`; workspace `ws`; root path; `MaxEntries = 10`; `MaxCandidatesScanned = 100`; `MaxOutputBytes = 4096`; no continuation | `penghou-io:request:v1:sha256:f9c6225c276e1675b44c6f340a8ea3f55182974d6aa0acdb86e39dd5335c4b4d` |

ASCII case-only path spelling differences produce the same identity. Non-ASCII
code points are preserved, so their spelling variants may have distinct
identities. Changing workspace, target, operation, bounds, preconditions,
continuation, version, or payload produces a different encoded request.
