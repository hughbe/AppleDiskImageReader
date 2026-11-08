using System.Buffers;

namespace AppleDiskImageReader;

/// <summary>
/// Represents an Apple disk image.
/// </summary>
/// <remarks>
/// The image does not own the stream passed to the constructor: the caller must keep it open
/// for the lifetime of this object and dispose it. Reads through this class are serialized
/// internally, so its methods may be called concurrently, and the stream position is restored
/// after each call. Nothing else may use the stream while a read is in progress.
/// Malformed content is reported with <see cref="ArgumentException"/>.
/// </remarks>
public class AppleDiskImage
{
    private const int MaxBufferSize = 81920;

    private readonly Stream _stream;

    private readonly long _streamStartOffset;

    private readonly long _imageDataLength;

    private readonly SemaphoreSlim _streamGate = new(1, 1);

    /// <summary>
    /// Gets the prefix of the Apple Disk Image.
    /// </summary>
    public AppleDiskImagePrefix Prefix { get; }

    /// <summary>
    /// Gets the effective image data length. For ProDOS images whose header has
    /// a zero data length, this is calculated from the number of 512-byte blocks.
    /// </summary>
    public long ImageDataLength => _imageDataLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleDiskImage"/> class.
    /// </summary>
    /// <param name="stream">The stream representing the Apple disk image.</param>
    public AppleDiskImage(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        _stream = stream;
        _streamStartOffset = stream.Position;

        // Read the prefix
        Span<byte> prefixData = stackalloc byte[AppleDiskImagePrefix.Size];
        try
        {
            stream.ReadExactly(prefixData);
        }
        catch (EndOfStreamException)
        {
            throw new ArgumentException("Stream is too short to contain a valid Apple Disk Image prefix.", nameof(stream));
        }

        Prefix = new AppleDiskImagePrefix(prefixData);
        _imageDataLength = GetActualDataLength();

        ValidateSections();
    }

    /// <summary>
    /// Gets the data of the Apple Disk Image.
    /// </summary>
    /// <returns>The data as a byte array.</returns>
    public byte[] GetImageData()
        => GetData(Prefix.DataOffset, _imageDataLength);

    /// <summary>
    /// Gets the data of the Apple Disk Image and writes it to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetImageData(Stream outputStream)
        => GetData(Prefix.DataOffset, _imageDataLength, outputStream);

    /// <summary>
    /// Copies the image data to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public long CopyImageDataTo(Stream outputStream)
        => CopyDataTo(Prefix.DataOffset, _imageDataLength, outputStream);

    /// <summary>
    /// Asynchronously copies the image data to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the data to.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public ValueTask<long> CopyImageDataToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        => CopyDataToAsync(Prefix.DataOffset, _imageDataLength, outputStream, cancellationToken);

    /// <summary>
    /// Gets the comment data of the Apple Disk Image.
    /// </summary>
    /// <returns>The comment data as a byte array.</returns>
    public byte[] GetCommentData()
        => GetData(Prefix.CommentOffset, Prefix.CommentLength);

    /// <summary>
    /// Gets the comment data of the Apple Disk Image and writes it to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the comment data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetCommentData(Stream outputStream)
        => GetData(Prefix.CommentOffset, Prefix.CommentLength, outputStream);

    /// <summary>
    /// Copies the comment data to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the comment data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public long CopyCommentDataTo(Stream outputStream)
        => CopyDataTo(Prefix.CommentOffset, Prefix.CommentLength, outputStream);

    /// <summary>
    /// Asynchronously copies the comment data to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the comment data to.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public ValueTask<long> CopyCommentDataToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        => CopyDataToAsync(Prefix.CommentOffset, Prefix.CommentLength, outputStream, cancellationToken);

    /// <summary>
    /// Gets the creator data of the Apple Disk Image.
    /// </summary>
    /// <returns>The creator data as a byte array.</returns>
    public byte[] GetCreatorData()
        => GetData(Prefix.CreatorDataOffset, Prefix.CreatorDataLength);

    /// <summary>
    /// Gets the creator data of the Apple Disk Image and writes it to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the creator data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetCreatorData(Stream outputStream)
        => GetData(Prefix.CreatorDataOffset, Prefix.CreatorDataLength, outputStream);

    /// <summary>
    /// Copies the creator data to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the creator data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public long CopyCreatorDataTo(Stream outputStream)
        => CopyDataTo(Prefix.CreatorDataOffset, Prefix.CreatorDataLength, outputStream);

    /// <summary>
    /// Asynchronously copies the creator data to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the creator data to.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public ValueTask<long> CopyCreatorDataToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        => CopyDataToAsync(Prefix.CreatorDataOffset, Prefix.CreatorDataLength, outputStream, cancellationToken);

    /// <summary>
    /// Gets the actual data length of the disk image.
    /// For ProDOS format, if DataLength is 0, calculates from NumberOfBlocks * 512.
    /// Otherwise returns DataLength.
    /// </summary>
    /// <returns>The actual data length in bytes.</returns>
    private long GetActualDataLength()
    {
        if (Prefix.DataLength == 0 && Prefix.Format == AppleDiskImageFormat.ProDOS)
        {
            return (long)Prefix.NumberOfBlocks * 512;
        }

        return Prefix.DataLength;
    }

    private void ValidateSections()
    {
        long streamLength = _stream.Length - _streamStartOffset;
        if (streamLength < AppleDiskImagePrefix.Size)
        {
            throw new ArgumentException("Stream is too short to contain a valid Apple Disk Image.", "stream");
        }

        long dataEnd = ValidateSection(Prefix.DataOffset, _imageDataLength, streamLength, "image data");
        long previousEnd = dataEnd;

        if (Prefix.CommentLength != 0)
        {
            if (Prefix.CommentOffset < previousEnd)
            {
                throw new ArgumentException("The Apple Disk Image comment must follow the image data.", "stream");
            }

            previousEnd = ValidateSection(Prefix.CommentOffset, Prefix.CommentLength, streamLength, "comment");
        }

        if (Prefix.CreatorDataLength != 0)
        {
            if (Prefix.CreatorDataOffset < previousEnd)
            {
                throw new ArgumentException("The Apple Disk Image creator data must follow the preceding chunks.", "stream");
            }

            ValidateSection(Prefix.CreatorDataOffset, Prefix.CreatorDataLength, streamLength, "creator data");
        }
    }

    private static long ValidateSection(uint offset, long length, long streamLength, string sectionName)
    {
        long end = offset + length;
        if (end > streamLength)
        {
            throw new ArgumentException($"The Apple Disk Image {sectionName} extends beyond the end of the stream.", "stream");
        }

        return end;
    }

    private byte[] GetData(uint offset, long length)
    {
        if (length > Array.MaxLength)
        {
            throw new NotSupportedException($"Sections larger than {Array.MaxLength:N0} bytes cannot be returned as a byte array.");
        }

        byte[] data = GC.AllocateUninitializedArray<byte>((int)length);
        if (data.Length == 0)
        {
            return data;
        }

        _streamGate.Wait();
        try
        {
            long originalPosition = _stream.Position;
            try
            {
                _stream.Position = _streamStartOffset + offset;
                _stream.ReadExactly(data);
            }
            finally
            {
                _stream.Position = originalPosition;
            }
        }
        finally
        {
            _streamGate.Release();
        }

        return data;
    }

    /// <summary>
    /// Gets the data at the specified offset and length, writing it to the provided output stream.
    /// </summary>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The length of data to read.</param>
    /// <param name="outputStream">The output stream to write the data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    /// <exception cref="ArgumentException">Thrown when the output stream is not writable.</exception>
    private int GetData(uint offset, long length, Stream outputStream)
    {
        if (length > int.MaxValue)
        {
            throw new NotSupportedException($"Sections larger than {int.MaxValue:N0} bytes cannot be copied by this API.");
        }

        return (int)CopyDataTo(offset, length, outputStream);
    }

    private long CopyDataTo(uint offset, long length, Stream outputStream)
    {
        ValidateOutputStream(outputStream);

        if (length == 0)
        {
            return 0; // No data
        }

        int bufferSize = (int)Math.Min(length, MaxBufferSize);

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            _streamGate.Wait();
            try
            {
                long originalPosition = _stream.Position;
                try
                {
                    long totalBytesRead = 0;
                    long remaining = length;

                    _stream.Position = _streamStartOffset + offset;
                    while (remaining > 0)
                    {
                        int toRead = (int)Math.Min(remaining, bufferSize);
                        _stream.ReadExactly(rentedBuffer.AsSpan(0, toRead));

                        outputStream.Write(rentedBuffer.AsSpan(0, toRead));
                        totalBytesRead += toRead;
                        remaining -= toRead;
                    }

                    return totalBytesRead;
                }
                finally
                {
                    _stream.Position = originalPosition;
                }
            }
            finally
            {
                _streamGate.Release();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    private async ValueTask<long> CopyDataToAsync(
        uint offset,
        long length,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        ValidateOutputStream(outputStream);

        if (length == 0)
        {
            return 0;
        }

        int bufferSize = (int)Math.Min(length, MaxBufferSize);
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            await _streamGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                long originalPosition = _stream.Position;
                try
                {
                    long totalBytesRead = 0;
                    long remaining = length;

                    _stream.Position = _streamStartOffset + offset;
                    while (remaining > 0)
                    {
                        int toRead = (int)Math.Min(remaining, bufferSize);
                        await _stream.ReadExactlyAsync(
                            rentedBuffer.AsMemory(0, toRead),
                            cancellationToken).ConfigureAwait(false);

                        await outputStream.WriteAsync(
                            rentedBuffer.AsMemory(0, toRead),
                            cancellationToken).ConfigureAwait(false);
                        totalBytesRead += toRead;
                        remaining -= toRead;
                    }

                    return totalBytesRead;
                }
                finally
                {
                    _stream.Position = originalPosition;
                }
            }
            finally
            {
                _streamGate.Release();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    private void ValidateOutputStream(Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!outputStream.CanWrite)
        {
            throw new ArgumentException("Output stream must be writable.", nameof(outputStream));
        }

        if (ReferenceEquals(outputStream, _stream))
        {
            throw new ArgumentException("The input stream cannot also be used as the output stream.", nameof(outputStream));
        }
    }
}
