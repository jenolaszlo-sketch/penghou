# Local existing-file patch profile

Status: Implemented narrow standalone profile, 2026-10-01. The profile is
provided by `Penghou.IO.Local.LocalWorkspacePatcher` and used by Luban's
separate `SinglePatchExecutor`. It is not the full `IWorkspaceWriter`, a batch
transaction, or a Hufu/Zhinu governed adapter. Luban's capture-only
`PreviewRuntime.WhatIfAsync` has no callback to this writer and
`ResolvedEffectPlan.CanCommit` remains false.

## Supported target and binding

The patcher accepts only an existing file below a trusted fixed-drive NTFS
workspace root when the caller explicitly selects
`LocalPatchNamespace.HostControlled`. The default `Unspecified` namespace is
rejected before target I/O. UNC/device roots, non-NTFS or non-fixed volumes,
reparse components, files with multiple hard links, and directories whose
case-sensitive mode is observed enabled are rejected. Enabling case-sensitive
mode is unsupported by this controlled-namespace profile; rejection of an
observed enabled flag is implementation behavior, not native qualification.
The root-to-drive mapping is trusted host configuration. Read, Patch and
Metadata rights must all be granted where required; missing authorization or
journal dependencies fail closed.

The patcher pins ancestor directory handles and opens the target existing-only
with no sharing. It verifies filesystem object identity and performs version
check, mutation, flush and verification through that same target handle. Parent
pins block ordinary rename/delete replacement and write opens; an exclusive leaf
handle blocks ordinary write opens. However, `CreateHardLink` succeeded during
an exclusive leaf handle in the native probe. A later link-count check detected
the alias and returned Unsupported/NoMutation, but a race after that check
cannot be closed. The host therefore must guarantee a stable, controlled
root/drive/mount/directory namespace against untrusted actors for the operation.
The profile is not general namespace confinement and does not defend against
privileged actors or raw-volume writes.

The explicit namespace mode is part of executor construction and host admission.
The default or unknown mode returns Unsupported before authorization or journal
calls. The versioned provider profile is
`local-windows-ntfs-controlled-patch-v1`.

The reader's `local-read-v1:sha256` version is a content precondition. It does
not prove that the later pathname still denotes the same native file object
that was read during preview. At execution, the patcher binds the current
qualified pathname and checks the expected content on the exact locked object
before writing.
## Patch semantics and bounds

Input and output are strict UTF-8. Patches use ordered, non-overlapping byte
ranges against the exact original bytes; offsets must lie on Unicode scalar
boundaries. No text-hunk conversion, fuzzy matching, or automatic retry is
performed. The provider bounds the original file at 16 MiB, the output at
16 MiB, patches at 128, each replacement at 1 MiB, and aggregate reads using
`MaxReadBytes` (provider default 32 MiB). Luban supplies the document's lower
limits; aggregate original plus post-write verification bytes must fit that
document read ceiling and is checked before host admission/provider start. The
operation deadline is at most 30 seconds; outcome completion uses an independent
5-second token.

The patch is applied in place through the same locked handle, flushed, and read
back for verification. A crash can leave partial/truncated content; there is no
rollback or atomic replacement. After mutation may have started, failures or
missing completion evidence are ambiguous and require reconciliation. Never
blindly retry an ambiguous operation. The start fence and outcome journal are
required host dependencies, not default implementations supplied by Local.

## Qualification boundary

The path pins and no-share target handle establish the documented ordinary-open
profile only. They do not establish resistance to privileged interference,
raw-volume changes, process termination, power loss, or multi-file partial
failure. Hufu grants/revocation, Zhinu durable journaling/recovery, batch
admission and production release policy require independent qualification.

For the Windows sharing and handle information primitives, see Microsoft's
[CreateFile](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea),
[FILE_ID_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info),
and [BY_HANDLE_FILE_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/ns-fileapi-by_handle_file_information)
references.