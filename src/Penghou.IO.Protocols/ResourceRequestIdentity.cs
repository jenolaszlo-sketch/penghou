using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Penghou.IO.Abstractions;

/// <summary>
/// Computes versioned, domain-separated identities for concrete backend
/// requests. Invocation and supplied request identity fields are intentionally
/// excluded. Providers must still snapshot inputs before both computing the
/// identity and using those inputs for I/O.
/// </summary>
public static class ResourceRequestIdentity
{
    public const int SchemaVersion = 1;
    public const string IdentityPrefix = "penghou-io:request:v1:sha256:";
    public const int MaximumPayloadBytes = 16 * 1024 * 1024;

    private const string Domain = "Penghou.IO.ResourceRequestIdentity";
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static RequestIdentity Compute(FileReadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var writer = Start(Operation.ReadFile);
        WriteWorkspacePath(writer, request.Workspace, request.Path, allowRoot: false);
        WriteLimits(writer, request.Limits);
        return Finish(writer);
    }

    public static RequestIdentity Compute(FileMetadataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var writer = Start(Operation.FileMetadata);
        WriteWorkspacePath(writer, request.Workspace, request.Path, allowRoot: false);
        return Finish(writer);
    }

    public static RequestIdentity Compute(DirectoryListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var writer = Start(Operation.ListDirectory);
        WriteWorkspacePath(writer, request.Workspace, request.Path, allowRoot: true);
        writer.WriteInt32(request.MaxEntries);
        writer.WriteInt32(request.MaxCandidatesScanned);
        writer.WriteInt32(request.MaxOutputBytes);
        writer.WriteNullableString(request.Continuation?.Value);
        return Finish(writer);
    }

    public static RequestIdentity Compute(FileWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Limits);
        if (request.Content.Length > request.Limits.MaxBytes || request.Content.Length > MaximumPayloadBytes)
            throw new ArgumentException("Write payload exceeds its declared or encoding bound.", nameof(request));
        var content = request.Content.ToArray();
        using var writer = Start(Operation.WriteFile);
        WriteWorkspacePath(writer, request.Workspace, request.Path, allowRoot: false);
        WriteLimits(writer, request.Limits);
        writer.WriteBytes(content);
        WritePrecondition(writer, request.Precondition);
        return Finish(writer);
    }

    public static RequestIdentity Compute(FileDeleteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var writer = Start(Operation.DeleteFile);
        WriteWorkspacePath(writer, request.Workspace, request.Path, allowRoot: false);
        writer.WriteString(RequiredToken(request.ExpectedVersion.Value, nameof(request.ExpectedVersion)));
        return Finish(writer);
    }

    public static RequestIdentity Compute(DirectoryCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var writer = Start(Operation.CreateDirectory);
        WriteWorkspacePath(writer, request.Workspace, request.Path, allowRoot: false);
        return Finish(writer);
    }

    public static RequestIdentity Compute(FileMoveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.DestinationPrecondition);
        using var writer = Start(Operation.MoveFile);
        WriteWorkspace(writer, request.Workspace);
        writer.WriteString(WindowsWorkspacePath.ToIdentityPath(request.Source, allowRoot: false));
        writer.WriteString(WindowsWorkspacePath.ToIdentityPath(request.Destination, allowRoot: false));
        writer.WriteString(RequiredToken(request.ExpectedSourceVersion.Value, nameof(request.ExpectedSourceVersion)));
        WritePrecondition(writer, request.DestinationPrecondition);
        return Finish(writer);
    }

    public static RequestIdentity Compute(WebReadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var writer = Start(Operation.WebRead);
        writer.WriteString(CanonicalHttpRequestUrl(request.Url));
        WriteLimits(writer, request.Limits);
        return Finish(writer);
    }

    private static CanonicalWriter Start(Operation operation)
    {
        var writer = new CanonicalWriter();
        writer.WriteRaw(StrictUtf8.GetBytes(Domain));
        writer.WriteByte(0);
        writer.WriteInt32(SchemaVersion);
        writer.WriteByte((byte)operation);
        return writer;
    }

    private static RequestIdentity Finish(CanonicalWriter writer)
    {
        var digest = SHA256.HashData(writer.WrittenSpan);
        return new RequestIdentity(IdentityPrefix + Convert.ToHexString(digest).ToLowerInvariant());
    }

    private static void WriteWorkspacePath(CanonicalWriter writer, WorkspaceId workspace, WorkspacePath path, bool allowRoot)
    {
        WriteWorkspace(writer, workspace);
        writer.WriteString(WindowsWorkspacePath.ToIdentityPath(path, allowRoot));
    }

    private static void WriteWorkspace(CanonicalWriter writer, WorkspaceId workspace)
    {
        var value = workspace.Value;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value.Any(char.IsControl))
            throw new ArgumentException("Workspace identity must be nonempty, bounded, and free of control characters.", nameof(workspace));
        writer.WriteString(value);
    }

    private static void WriteLimits(CanonicalWriter writer, IoLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (limits.MaxBytes is < 0 or > MaximumPayloadBytes)
            throw new ArgumentException("Byte limit exceeds the finite encoding profile.", nameof(limits));
        writer.WriteInt32(limits.MaxBytes);
    }

    private static void WritePrecondition(CanonicalWriter writer, WritePrecondition precondition)
    {
        ArgumentNullException.ThrowIfNull(precondition);
        writer.WriteInt32((int)precondition.Kind);
        writer.WriteNullableString(precondition.Version?.Value);
    }

    private static string RequiredToken(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value.Any(char.IsControl) || StrictUtf8.GetByteCount(value) > 1024)
            throw new ArgumentException("Version tokens must be nonempty bounded UTF-8 without controls.", parameterName);
        return value;
    }

    private static string CanonicalHttpRequestUrl(WebUrl webUrl)
    {
        var uri = webUrl.Value ?? throw new ArgumentException("Web URL cannot be null.", nameof(webUrl));
        if (!uri.IsAbsoluteUri ||
            !(uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
              uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
            string.IsNullOrEmpty(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Web request identity requires an absolute HTTP or HTTPS URL without user information.", nameof(webUrl));
        }

        // HttpRequestUrl deliberately excludes a URI fragment: fragments are not
        // sent in an HTTP request. Uri canonicalizes scheme/host and escaping.
        return uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
    }

    private enum Operation : byte
    {
        ReadFile = 1,
        FileMetadata = 2,
        ListDirectory = 3,
        WriteFile = 4,
        // Reserved: the former text-patch request belongs to Luban semantics.
        ReservedPatchFile = 5,
        DeleteFile = 6,
        CreateDirectory = 7,
        MoveFile = 8,
        WebRead = 9
    }

    private sealed class CanonicalWriter : IDisposable
    {
        private readonly ArrayBufferWriter<byte> _buffer = new();
        private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        internal ReadOnlySpan<byte> WrittenSpan => _buffer.WrittenSpan;

        internal void WriteByte(byte value)
        {
            var span = _buffer.GetSpan(sizeof(byte));
            span[0] = value;
            _buffer.Advance(sizeof(byte));
        }

        internal void WriteInt32(int value)
        {
            var span = _buffer.GetSpan(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
            _buffer.Advance(sizeof(int));
        }

        internal void WriteString(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var byteCount = Utf8.GetByteCount(value);
            if (byteCount > 16 * 1024) throw new ArgumentException("String exceeds the finite encoding bound.");
            WriteInt32(byteCount);
            var destination = _buffer.GetSpan(byteCount);
            var written = Utf8.GetBytes(value.AsSpan(), destination);
            _buffer.Advance(written);
        }

        internal void WriteNullableString(string? value)
        {
            WriteByte(value is null ? (byte)0 : (byte)1);
            if (value is not null)
                WriteString(value);
        }

        internal void WriteBytes(ReadOnlySpan<byte> value)
        {
            WriteInt32(value.Length);
            var destination = _buffer.GetSpan(value.Length);
            value.CopyTo(destination);
            _buffer.Advance(value.Length);
        }

        internal void WriteRaw(ReadOnlySpan<byte> value)
        {
            var destination = _buffer.GetSpan(value.Length);
            value.CopyTo(destination);
            _buffer.Advance(value.Length);
        }

        public void Dispose() => _buffer.Clear();
    }
}
