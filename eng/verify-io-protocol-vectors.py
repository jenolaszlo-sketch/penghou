"""Independent v1 encoder: verify fixed JSON bytes/hashes without loading .NET."""
import hashlib
import json
import pathlib
import struct

path = pathlib.Path(__file__).resolve().parents[1] / "tests/Penghou.IO.Protocol.Tests/protocol-v1-vectors.json"
document = json.loads(path.read_text(encoding="utf-8"))
assert document["schema"] == 1

def integer(value):
    return struct.pack("<i", value)

def string(value):
    data = value.encode("utf-8", errors="strict")
    return integer(len(data)) + data

def nullable(value):
    return b"\x00" if value is None else b"\x01" + string(value)

for vector in document["vectors"]:
    opcode = vector["opcode"]
    data = b"Penghou.IO.ResourceRequestIdentity\x00" + integer(1) + bytes([opcode])
    if opcode != 9:
        data += string(vector["workspace"])
    if opcode in (1, 2, 3, 4, 6, 7):
        data += string(vector["canonicalPath"])
    if opcode == 9:
        data += string(vector["canonicalUrl"])
    if opcode in (1, 4, 9):
        data += integer(vector["maxBytes"])
    if opcode == 3:
        data += integer(vector["maxEntries"]) + integer(vector["maxCandidatesScanned"]) + integer(vector["maxOutputBytes"]) + nullable(vector["continuation"])
    if opcode == 4:
        content = bytes.fromhex(vector["contentHex"])
        data += integer(len(content)) + content
    if opcode == 6:
        data += string(vector["expectedVersion"])
    if opcode == 8:
        data += string(vector["canonicalSource"]) + string(vector["canonicalDestination"]) + string(vector["expectedVersion"])
    if opcode in (4, 8):
        data += integer(vector["preconditionKind"]) + nullable(vector["preconditionVersion"])
    assert data.hex() == vector["canonicalHex"], vector["name"]
    identity = "penghou-io:request:v1:sha256:" + hashlib.sha256(data).hexdigest()
    assert identity == vector["identity"], vector["name"]
print(f"Verified {len(document['vectors'])} independent schema-v1 byte/hash vectors.")
