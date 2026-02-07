using System.Buffers;
using System.Diagnostics.Contracts;

namespace AppleDiskImageReader;

/// <summary>
/// Represents an Apple disk image.
/// </summary>
public class AppleDiskImage
{
    private readonly Stream _stream;

    private readonly long _streamStartOffset;

    /// <summary>
    /// Gets the prefix of the Apple Disk Image.
    /// </summary>
    public AppleDiskImagePrefix Prefix { get; }

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
    }

    /// <summary>
    /// Gets the data of the Apple Disk Image.
    /// </summary>
    /// <returns>>The data as a byte array.</returns>
    public byte[] GetImageData()
    {
        int capacity = (int)GetActualDataLength();
        using var outputStream = new MemoryStream(capacity);
        GetImageData(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Gets the data of the Apple Disk Image and writes it to the provided output stream.
    /// </summary>
    /// <param name="outputStream">>The output stream to write the data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetImageData(Stream outputStream)
        => GetData(Prefix.DataOffset, GetActualDataLength(), outputStream);

    /// <summary>
    /// Gets the comment data of the Apple Disk Image.
    /// </summary>
    /// <returns>>The comment data as a byte array.</returns>
    public byte[] GetCommentData()
    {
        using var outputStream = new MemoryStream((int)Prefix.CommentLength);
        GetCommentData(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Gets the comment data of the Apple Disk Image and writes it to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the comment data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetCommentData(Stream outputStream)
        => GetData(Prefix.CommentOffset, Prefix.CommentLength, outputStream);

    /// <summary>
    /// Gets the creator data of the Apple Disk Image and writes it to the provided output stream.
    /// </summary>
    /// <returns>The creator data as a byte array.</returns>
    public byte[] GetCreatorData()
    {
        using var outputStream = new MemoryStream((int)Prefix.CreatorDataLength);
        GetCreatorData(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Gets the creator data of the Apple Disk Image and writes it to the provided output stream.
    /// </summary>
    /// <param name="outputStream">The output stream to write the creator data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetCreatorData(Stream outputStream)
        => GetData(Prefix.CreatorDataOffset, Prefix.CreatorDataLength, outputStream);

    /// <summary>
    /// Gets the actual data length of the disk image.
    /// For ProDOS format, if DataLength is 0, calculates from NumberOfBlocks * 512.
    /// Otherwise returns DataLength.
    /// </summary>
    /// <returns>The actual data length in bytes.</returns>
    private uint GetActualDataLength()
    {
        if (Prefix.DataLength == 0 && Prefix.Format == AppleDiskImageFormat.ProDOS)
        {
            return Prefix.NumberOfBlocks * 512;
        }

        return Prefix.DataLength;
    }

    /// <summary>
    /// Gets the data at the specified offset and length, writing it to the provided output stream.
    /// </summary>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The length of data to read.</param>
    /// <param name="outputStream">The output stream to write the data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    /// <exception cref="ArgumentException">Thrown when the output stream is not writable.</exception>
    private int GetData(uint offset, uint length, Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!outputStream.CanWrite)
        {
            throw new ArgumentException("Output stream must be writable.", nameof(outputStream));
        }

        if (length == 0)
        {
            return 0; // No data
        }

        _stream.Position = _streamStartOffset + offset;

        const int MaxBufferSize = 81920; // Common .NET buffer size
        int bufferSize = (int)Math.Min(length, MaxBufferSize);

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int totalBytesRead = 0;
            int remaining = (int)length;

            while (remaining > 0)
            {
                int toRead = Math.Min(remaining, bufferSize);
                Span<byte> buffer = rentedBuffer.AsSpan(0, toRead);
                int bytesRead = _stream.Read(buffer);

                if (bytesRead == 0)
                {
                    break; // End of source stream
                }

                outputStream.Write(rentedBuffer.AsSpan(0, bytesRead));
                totalBytesRead += bytesRead;
                remaining -= bytesRead;
            }

            return totalBytesRead;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }
}
