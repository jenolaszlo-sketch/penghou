using System.Collections;
using Penghou.Http.Abstractions;
using Xunit;

namespace Penghou.Http.Tests;

public sealed class HttpContractTests
{
    [Fact]
    public void BodyAndChunkOwnTheirBytes()
    {
        var source = new byte[] { 1, 2, 3 };
        var body = new HttpBinaryBody(source, "application/json");
        var chunk = new HttpResponseChunk(source, isCompleted: true);
        source[0] = 9;
        body.GetBytes()[1] = 9;
        chunk.GetBytes()[2] = 9;
        Assert.Equal(new byte[] { 1, 2, 3 }, body.GetBytes());
        Assert.Equal(new byte[] { 1, 2, 3 }, chunk.GetBytes());
        Assert.Equal(3, body.Length);
        Assert.True(chunk.IsCompleted);
    }

    [Fact]
    public void HeadersAndContextOwnNestedSnapshots()
    {
        var values = new List<string> { "a", "b" };
        var fields = new Dictionary<string, IReadOnlyList<string>> { ["X-Trace"] = values };
        var metadata = new Dictionary<string, string> { ["source"] = "first" };
        var headers = new HttpHeaders(fields);
        var context = new HttpRequestContext("trace", metadata);
        fields.Clear();
        values.Clear();
        metadata["source"] = "changed";
        Assert.Equal(new[] { "a", "b" }, headers.Fields["x-trace"]);
        Assert.Equal("first", context.Metadata["source"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)context.Metadata).Add("x", "y"));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)headers.Fields["X-Trace"])[0] = "changed");
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("file:///C:/secret")]
    [InlineData("ftp://example.test/path")]
    [InlineData("https://user:secret@example.test/")]
    [InlineData("https://example.test/#fragment")]
    public void InvalidOrAmbiguousDestinationRejected(string destination)
    {
        Assert.Throws<ArgumentException>(() => new HttpTransportRequest("r", "GET", new Uri(destination, UriKind.RelativeOrAbsolute)));
    }

    [Theory]
    [InlineData("bad\r\nInjected: yes")]
    [InlineData("bad name")]
    [InlineData("é")]
    public void HeaderNameCannotInjectOrViolateTokenGrammar(string name)
    {
        Assert.Throws<ArgumentException>(() => new HttpHeaders(new Dictionary<string, IReadOnlyList<string>> { [name] = new[] { "ok" } }));
    }

    [Theory]
    [InlineData("secret\r\nInjected: yes")]
    public void HeaderValueRejectsMalformedOrControlText(string value)
    {
        Assert.Throws<ArgumentException>(() => new HttpHeaders(new Dictionary<string, IReadOnlyList<string>> { ["X-Test"] = new[] { value } }));
    }

    [Fact]
    public void InvalidUtf16RejectedBeforeHeaderAdmission()
    {
        var value = new string((char)0xd800, 1);
        Assert.Throws<ArgumentException>(() => new HttpHeaders(new Dictionary<string, IReadOnlyList<string>> { ["X-Test"] = new[] { value } }));
    }

    [Theory]
    [InlineData("Content-Type")]
    [InlineData("content-length")]
    [InlineData("TRANSFER-ENCODING")]
    [InlineData("Host")]
    public void CallerCannotOverrideBodyFramingOrHost(string name)
    {
        var headers = new HttpHeaders(new Dictionary<string, IReadOnlyList<string>> { [name] = new[] { "1" } });
        Assert.Throws<ArgumentException>(() => new HttpTransportRequest("r", "POST", new Uri("https://example.test/"), headers: headers));
    }

    [Fact]
    public void MultipartCannotExpandPastItsDeclaredSnapshotOrReadAFile()
    {
        var parts = new List<HttpMultipartPart> { new("file", new HttpBinaryBody(new byte[] { 1, 2 }), "file.jsonl") };
        var body = new HttpMultipartBody(parts);
        parts.Clear();
        Assert.Single(body.Parts);
        Assert.Equal(2, body.Length);
        Assert.Equal("file.jsonl", body.Parts[0].FileName);
        Assert.Throws<NotSupportedException>(() => ((IList<HttpMultipartPart>)body.Parts).Clear());
        Assert.Throws<ArgumentException>(() => new HttpMultipartBody(new LyingList<HttpMultipartPart>(body.Parts[0])));
    }

    [Theory]
    [InlineData("C:\\private\\input.jsonl")]
    [InlineData("/private/input.jsonl")]
    [InlineData("../input.jsonl")]
    [InlineData("..")]
    public void MultipartFilenameCannotExposeLocalPath(string path)
    {
        Assert.Throws<ArgumentException>(() => new HttpMultipartPart("file", new HttpBinaryBody(new byte[] { 1 }), path));
    }

    [Fact]
    public void PublicFailuresContainOnlyNeutralCodes()
    {
        var failure = new HttpTransportException("r", "provider-failed");
        Assert.Null(failure.InnerException);
        Assert.Equal("provider-failed", failure.ReasonCode);
        Assert.DoesNotContain("https", failure.Message);
    }

    [Fact]
    public void DeclaredBoundsDoNotPermitUnboundedRequestsOrEmptyNonterminalChunks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HttpTransportLimits(maximumResponseBodyBytes: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HttpTransportLimits(maximumRequestBodyBytes: HttpTransportBody.MaximumBodyBytes + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HttpTransportLimits(timeout: Timeout.InfiniteTimeSpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HttpTransportLimits(maximumChunkBytes: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HttpTransportLimits(maximumRedirects: 11));
        Assert.Throws<ArgumentException>(() => new HttpResponseChunk(ReadOnlyMemory<byte>.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HttpResponseChunk(new byte[HttpTransportLimits.MaximumChunkBytes + 1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HttpTransportRequest("r", "POST", new Uri("https://example.test/"),
            body: new HttpBinaryBody(new byte[] { 1 }), limits: new HttpTransportLimits(maximumRequestBodyBytes: 0)));
        Assert.True(new HttpResponseChunk(ReadOnlyMemory<byte>.Empty, true).IsCompleted);
        Assert.Equal(0, new HttpTransportLimits().MaximumRedirects);
    }

    [Fact]
    public async Task DecoratorDenialAndRevocationPreventInnerDispatch()
    {
        var inner = new RecordingTransport();
        var decorator = new DenyingTransport(inner);
        var request = new HttpTransportRequest("request-1", "GET", new Uri("https://example.test/"));
        await using (var response = await decorator.SendAsync(request))
        {
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Equal(200, response.StatusCode);
            var first = await response.Body.ReadAsync(2);
            Assert.Equal(new byte[] { 1, 2 }, first.GetBytes());
            Assert.False(first.IsCompleted);
            var last = await response.Body.ReadAsync(2);
            Assert.Equal(new byte[] { 3 }, last.GetBytes());
            Assert.True(last.IsCompleted);
        }
        Assert.True(inner.LastBody!.Disposed);
        decorator.Allowed = false;
        var denial = await Assert.ThrowsAsync<HttpAccessDeniedException>(async () => await decorator.SendAsync(request));
        Assert.Equal(request.RequestId, denial.RequestId);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task CancellationRemainsCancellationAndOwnedResponseCanBeDisposedWithoutReading()
    {
        var transport = new RecordingTransport();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var request = new HttpTransportRequest("r", "GET", new Uri("https://example.test/"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await transport.SendAsync(request, cancelled.Token));
        Assert.Equal(0, transport.Calls);
        var response = await transport.SendAsync(request);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await response.Body.ReadAsync(2, cancelled.Token));
        await response.DisposeAsync();
        await response.DisposeAsync();
        Assert.True(transport.LastBody!.Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await response.Body.ReadAsync(2));
    }

    [Fact]
    public void ContractAssemblyHasNoProductOrOtherCapabilityDependencies()
    {
        var references = typeof(IHttpTransport).Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();
        Assert.DoesNotContain(references, name => name.StartsWith("Penghou.", StringComparison.Ordinal));
    }

    private sealed class LyingList<T>(T item) : IReadOnlyList<T>
    {
        public int Count => 1;
        public T this[int index] => item;
        public IEnumerator<T> GetEnumerator() { yield return item; yield return item; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class DenyingTransport(RecordingTransport inner) : IHttpTransport
    {
        public bool Allowed { get; set; } = true;
        public ValueTask<HttpTransportResponse> SendAsync(HttpTransportRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Allowed) throw new HttpAccessDeniedException(request.RequestId);
            return inner.SendAsync(request, cancellationToken);
        }
    }

    private sealed class RecordingTransport : IHttpTransport
    {
        public int Calls { get; private set; }
        public FixtureBody? LastBody { get; private set; }
        public ValueTask<HttpTransportResponse> SendAsync(HttpTransportRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            LastBody = new FixtureBody(request.Limits);
            return ValueTask.FromResult(new HttpTransportResponse(request.RequestId, 200, HttpHeaders.Empty, LastBody));
        }
    }

    private sealed class FixtureBody(HttpTransportLimits limits) : IHttpResponseBody
    {
        private readonly byte[] _bytes = [1, 2, 3];
        private int _position;
        public bool Disposed { get; private set; }
        public ValueTask<HttpResponseChunk> ReadAsync(int maximumBytes, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            if (maximumBytes < 1 || maximumBytes > limits.MaximumReadBytes) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            var count = Math.Min(maximumBytes, _bytes.Length - _position);
            if (_position + count > limits.MaximumResponseBodyBytes) throw new HttpTransportException("fixture", "body-limit");
            var result = new HttpResponseChunk(_bytes.AsMemory(_position, count), _position + count == _bytes.Length);
            _position += count;
            return ValueTask.FromResult(result);
        }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }
}
