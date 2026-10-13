# Penghou.IO.Identity.Abstractions

Data-only request and identity values for the stable Penghou I/O protocol v1.
Exactly 18 types; no provider, authority, mutation journal or transport capability.
Namespaces and record shapes are preserved from the preview IO.Abstractions package.
FileWriteRequest borrows its byte memory; callers freeze it during computation.
Record equality is not canonical request identity equality. Constructors do not
authenticate or validate operational acceptance.

See [stable contract](https://github.com/jenolaszlo-sketch/penghou/blob/main/docs/graduation/io-stable-contract.md)
and [migration decision](https://github.com/jenolaszlo-sketch/penghou/blob/main/docs/decisions/0005-stable-io-identity-boundary.md).
