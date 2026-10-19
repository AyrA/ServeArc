namespace ServeArc.Utils;

/// <summary>
/// A wrapper for a stream that ignores calls to close or dispose it,
/// but will pretend the calls went through
/// </summary>
/// <param name="baseStream">Stream to wrap</param>
public class StreamWrapper(Stream baseStream) : Stream
{
    private bool disposed = false;

    public override bool CanRead
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.CanRead;
        }
    }

    public override bool CanSeek
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.CanSeek;
        }
    }

    public override bool CanWrite
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.CanWrite;
        }
    }

    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.Length;
        }
    }

    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.Position;
        }

        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            baseStream.Position = value;
        }
    }

    public override bool CanTimeout
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.CanTimeout;
        }
    }

    public override int ReadTimeout
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.ReadTimeout;
        }

        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            baseStream.ReadTimeout = value;
        }
    }

    public override int WriteTimeout
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.WriteTimeout;
        }

        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            baseStream.WriteTimeout = value;
        }
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.Write(buffer, offset, count);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void Close()
    {
        disposed = true;
    }

    protected override void Dispose(bool disposing)
    {
        disposed = true;
    }

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        disposed = true;
        return ValueTask.CompletedTask;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.EndWrite(asyncResult);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.FlushAsync(cancellationToken);
    }

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.ReadAsync(buffer, cancellationToken);
    }

    public override int ReadByte()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.ReadByte();
    }

    public override string ToString()
    {
        return $"Wrapper of {baseStream}";
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.WriteAsync(buffer, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.WriteByte(value);
    }
}
