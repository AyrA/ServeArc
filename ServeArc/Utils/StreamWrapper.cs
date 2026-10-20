namespace ServeArc.Utils;

/// <summary>
/// A wrapper for a stream that ignores calls to close or dispose it,
/// but will pretend the calls went through
/// </summary>
/// <param name="baseStream">Stream to wrap</param>
/// <param name="debugLogger">Debug logger to which every method and property call is logged</param>
public class StreamWrapper(Stream baseStream, ILogger? debugLogger = null) : Stream
{
    private bool disposed = false;

    public override bool CanRead
    {
        get
        {
            debugLogger?.LogDebug("{Name} get", nameof(CanRead));
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.CanRead;
        }
    }

    public override bool CanSeek
    {
        get
        {
            debugLogger?.LogDebug("{Name} get", nameof(CanSeek));
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.CanSeek;
        }
    }

    public override bool CanWrite
    {
        get
        {
            debugLogger?.LogDebug("{Name} get", nameof(CanWrite));
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.CanWrite;
        }
    }

    public override long Length
    {
        get
        {
            debugLogger?.LogDebug("{Name} get", nameof(Length));
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.Length;
        }
    }

    public override long Position
    {
        get
        {
            debugLogger?.LogDebug("{Name} get", nameof(Position));
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.Position;
        }

        set
        {
            debugLogger?.LogDebug("{Name} set {Value}", nameof(Position), value);
            ObjectDisposedException.ThrowIf(disposed, this);
            baseStream.Position = value;
        }
    }

    public override bool CanTimeout
    {
        get
        {
            debugLogger?.LogDebug("{Name} get", nameof(CanTimeout));
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.CanTimeout;
        }
    }

    public override int ReadTimeout
    {
        get
        {
            debugLogger?.LogDebug("{Name} get", nameof(ReadTimeout));
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.ReadTimeout;
        }

        set
        {
            debugLogger?.LogDebug("{Name} set {Value}", nameof(ReadTimeout), value);
            ObjectDisposedException.ThrowIf(disposed, this);
            baseStream.ReadTimeout = value;
        }
    }

    public override int WriteTimeout
    {
        get
        {
            debugLogger?.LogDebug("{Name} get", nameof(WriteTimeout));
            ObjectDisposedException.ThrowIf(disposed, this);
            return baseStream.WriteTimeout;
        }

        set
        {
            debugLogger?.LogDebug("{Name} set {Value}", nameof(WriteTimeout), value);
            ObjectDisposedException.ThrowIf(disposed, this);
            baseStream.WriteTimeout = value;
        }
    }

    public override void Flush()
    {
        debugLogger?.LogDebug("{Name}()", nameof(Flush));
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        debugLogger?.LogDebug("{Name}(buffer,{Offset},{Count})", nameof(Read), offset, count);
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        debugLogger?.LogDebug("{Name}(buffer,{Offset},{Origin})", nameof(Seek), offset, origin);
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        debugLogger?.LogDebug("{Name}(buffer,{Value})", nameof(SetLength), value);
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        debugLogger?.LogDebug("{Name}(buffer,{Offset},{Count})", nameof(Write), offset, count);
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.Write(buffer, offset, count);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        debugLogger?.LogDebug("{Name}(buffer,{Offset},{Count},callback,state)", nameof(BeginRead), offset, count);
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        debugLogger?.LogDebug("{Name}(buffer,{Offset},{Count},callback,state)", nameof(BeginWrite), offset, count);
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void Close()
    {
        debugLogger?.LogDebug("{Name}()", nameof(Close));
        disposed = true;
    }

    protected override void Dispose(bool disposing)
    {
        debugLogger?.LogDebug("{Name}({Disposing})", nameof(Dispose), disposing);
        disposed = true;
    }

    public override ValueTask DisposeAsync()
    {
        debugLogger?.LogDebug("{Name}()", nameof(DisposeAsync));
        GC.SuppressFinalize(this);
        disposed = true;
        return ValueTask.CompletedTask;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        debugLogger?.LogDebug("{Name}(destination,{BufferSize})", nameof(CopyTo), bufferSize);
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        debugLogger?.LogDebug("{Name}(destination,{BufferSize},cancellationToken)", nameof(CopyToAsync), bufferSize);
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        debugLogger?.LogDebug("{Name}(asyncResult)", nameof(EndRead));
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        debugLogger?.LogDebug("{Name}(asyncResult)", nameof(EndWrite));
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.EndWrite(asyncResult);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        debugLogger?.LogDebug("{Name}(cancellationToken)", nameof(FlushAsync));
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.FlushAsync(cancellationToken);
    }

    public override int Read(Span<byte> buffer)
    {
        debugLogger?.LogDebug("{Name}(buffer)", nameof(Read));
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        debugLogger?.LogDebug("{Name}(buffer,{Offset},{Count},cancellationToken)", nameof(ReadAsync), offset, count);
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        debugLogger?.LogDebug("{Name}(buffer,cancellationToken)", nameof(ReadAsync));
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.ReadAsync(buffer, cancellationToken);
    }

    public override int ReadByte()
    {
        debugLogger?.LogDebug("{Name}()", nameof(ReadByte));
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.ReadByte();
    }

    public override string ToString()
    {
        //Do not log this
        return $"Wrapper of {baseStream}";
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        debugLogger?.LogDebug("{Name}(buffer)", nameof(Write));
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        debugLogger?.LogDebug("{Name}(buffer,{Offset},{Count},cancellationToken)", nameof(WriteAsync), offset, count);
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        debugLogger?.LogDebug("{Name}(buffer,cancellationToken)", nameof(WriteAsync));
        ObjectDisposedException.ThrowIf(disposed, this);
        return baseStream.WriteAsync(buffer, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        debugLogger?.LogDebug("{Name}({Value})", nameof(WriteByte), value);
        ObjectDisposedException.ThrowIf(disposed, this);
        baseStream.WriteByte(value);
    }
}
