# Penghou.IO.Protocols

Stateless versioned request identities and Windows logical-path profile,
deterministic on supported .NET 8/10 platforms. The package depends only on
the data-only Penghou.IO.Identity.Abstractions package. Namespaces remain
Penghou.IO.Abstractions for preview consumer source compatibility.

V1 encoding and SHA-256 identities do not authorize or implement resource I/O.
IO.Local and broader IO.Abstractions capabilities remain preview. Preserve frozen
inputs, fixed vectors and protocol version markers; no decoder is exposed.

See [stable contract and migration](https://github.com/jenolaszlo-sketch/penghou/blob/main/docs/graduation/io-stable-contract.md)
and [canonical encoding](https://github.com/jenolaszlo-sketch/penghou/blob/main/docs/canonical-request-identity.md).
