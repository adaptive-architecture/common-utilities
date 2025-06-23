using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;

internal sealed class ResponseStreamWrapper : Stream
{
    private IHttpResponseBodyFeature? _originalBodyFeature;
    private HttpContext? _context;

    private IResponseRewriterFactory? _rewriterFactory;

    public ResponseStreamWrapper(IHttpResponseBodyFeature originalStream, HttpContext context, IResponseRewriterFactory rewriterFactory)
    {
        _originalBodyFeature = originalStream;
        _context = context;
        _rewriterFactory = rewriterFactory;
    }

    public override bool CanRead => _originalBodyFeature!.Stream.CanRead;
    public override bool CanSeek => _originalBodyFeature!.Stream.CanSeek;
    public override bool CanWrite => true;
    public override long Length => _originalBodyFeature!.Stream.Length;
    public override long Position
    {
        get
        {
            return _originalBodyFeature!.Stream.Position;
        }
        set
        {
            _originalBodyFeature!.Stream.Position = value;
        }
    }

    public override void Flush() => _originalBodyFeature!.Stream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        UpdateResponseContentLength();
        return _originalBodyFeature!.Stream.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        _originalBodyFeature!.Stream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) =>
        _originalBodyFeature!.Stream.Seek(offset, origin);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _originalBodyFeature!.Stream.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _originalBodyFeature!.Stream.ReadAsync(buffer, cancellationToken);

    public override void SetLength(long value)
    {
        _originalBodyFeature!.Stream.SetLength(value);
        UpdateResponseContentLength();
    }

    public override void WriteByte(byte value) => Write([value]);

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var bufferArray = buffer.ToArray();
        Write(bufferArray, 0, bufferArray.Length);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var rewrote = TryRewriteAsync(buffer.AsMemory(offset, count), CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        if (!rewrote)
        {
            _originalBodyFeature!.Stream.Write(buffer, offset, count);
        }
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var rewrote = await TryRewriteAsync(buffer, cancellationToken);
        if (!rewrote && _originalBodyFeature != null)
        {
            await _originalBodyFeature!.Stream.WriteAsync(buffer, cancellationToken);
        }
    }

    private async Task<bool> TryRewriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        var rewriter = _rewriterFactory!.GetRewriter(_context!);
        if (rewriter?.CanRewrite(_context!) != true)
        {
            return false;
        }

        try
        {
            await rewriter.RewriteAsync(buffer, _context!, _originalBodyFeature!.Stream, cancellationToken);
        }
        finally
        {
            rewriter.Dispose();
        }
        UpdateResponseContentLength();

        return true;
    }

    private void UpdateResponseContentLength()
    {
        // If the content length is set and we've rewritten the response
        // we need to remove the content length header as it will be incorrect
        // and can cause the client to hang/fail.

        if (_context!.Response.ContentLength != null)
        {
            _context!.Response.Headers.ContentLength = null;
        }
    }

    private void CoreDisposeLogic()
    {
        // Should we also call the `Dispose()`  method on the _originalBodyFeature ?
        _originalBodyFeature = null;
        _context = null;
        _rewriterFactory = null;
    }

    protected override void Dispose(bool disposing)
    {
        CoreDisposeLogic();
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        CoreDisposeLogic();
        return base.DisposeAsync();
    }
}
