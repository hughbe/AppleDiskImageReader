using System.Buffers.Binary;
using System.Diagnostics;
using AppleDiskImageReader.Utilities;

namespace AppleDiskImageReader;

/// <summary>
/// Represents the prefix of an Apple Disk Image.
/// </summary>
public readonly struct AppleDiskImagePrefix
{
    /// <summary>
    /// The size of the Apple Disk Image prefix in bytes.
    /// </summary>
    public const int Size = 64;

    /// <summary>
    /// Gets the signature of the Apple Disk Image.
    /// </summary>
    public String4 Signature { get; }

    /// <summary>
    /// Gets the creator signature of the Apple Disk Image.
    /// </summary>
    public String4 CreatorSignature { get; }

    /// <summary>
    /// Gets the header length of the Apple Disk Image.
    /// </summary>
    public ushort HeaderLength { get; }

    /// <summary>
    /// Gets the version of the Apple Disk Image.
    /// </summary>
    public ushort Version { get; }

    /// <summary>
    /// Gets the format of the Apple Disk Image.
    /// </summary>
    public AppleDiskImageFormat Format { get; }

    /// <summary>
    /// Gets the flags of the Apple Disk Image.
    /// </summary>
    public AppleDiskImageFlags Flags { get; }

    /// <summary>
    /// Gets the number of blocks in the Apple Disk Image.
    /// </summary>
    public uint NumberOfBlocks { get; }

    /// <summary>
    /// Gets the data offset of the Apple Disk Image.
    /// </summary>
    public uint DataOffset { get; }

    /// <summary>
    /// Gets the data length of the Apple Disk Image.
    /// </summary>
    public uint DataLength { get; }

    /// <summary>
    /// Gets the comment offset of the Apple Disk Image.
    /// </summary>
    public uint CommentOffset { get; }

    /// <summary>
    /// Gets the comment length of the Apple Disk Image.
    /// </summary>
    public uint CommentLength { get; }

    /// <summary>
    /// Gets the creator data offset of the Apple Disk Image.
    /// </summary>
    public uint CreatorDataOffset { get; }

    /// <summary>
    /// Gets the creator data length of the Apple Disk Image.
    /// </summary>
    public uint CreatorDataLength { get; }

    /// <summary>
    /// Gets the reserved bytes of the Apple Disk Image.
    /// </summary>
    public ByteArray16 Reserved { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleDiskImagePrefix"/> struct.
    /// </summary>
    /// <param name="data">The prefix data.</param>
    /// <exception cref="ArgumentException">>Thrown when the data length is not equal to <see cref="Size"/>.</exception>
    public AppleDiskImagePrefix(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes.", nameof(data));
        }

        // Structure documented in https://ciderpress2.com/formatdoc/TwoIMG-notes.html
        int offset = 0;

        // magic file signature, a 4-char string "2IMG" ($32 $49 $4d $47)
        Signature = new String4(data.Slice(offset, String4.Size));
        offset += String4.Size;

        if (!Signature.Equals("2IMG"u8))
        {
            throw new ArgumentException("Invalid Apple Disk Image signature.", nameof(data));
        }

        // creator signature code, a 4-char string
        CreatorSignature = new String4(data.Slice(offset, String4.Size));
        offset += String4.Size;

        // header length, in bytes; should be 64
        HeaderLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset));
        offset += 2;

        if (HeaderLength != Size)
        {
            throw new ArgumentException("Invalid Apple Disk Image header length.", nameof(data));
        }

        // file format version (always 1)
        Version = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset));
        offset += 2;

        if (Version != 1)
        {
            throw new ArgumentException("Unsupported Apple Disk Image version.", nameof(data));
        }

        // image data format (0=DOS order, 1=ProDOS order, 2=nibbles)
        Format = (AppleDiskImageFormat)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
        offset += 4;

        if (Format > AppleDiskImageFormat.Nibble)
        {
            throw new ArgumentException("Unknown Apple Disk Image format.", nameof(data));
        }

        // flags and DOS 3.3 volume number
        Flags = new AppleDiskImageFlags(data.Slice(offset, AppleDiskImageFlags.Size));
        offset += AppleDiskImageFlags.Size;

        // number of 512-byte blocks; only meaningful when format==1
        NumberOfBlocks = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
        offset += 4;

        // offset from start of file to data (should be 64, same as header length)
        DataOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
        offset += 4;

        if (DataOffset != Size)
        {
            throw new ArgumentException("Invalid Apple Disk Image data offset.", nameof(data));
        }

        // length of data, in bytes
        DataLength = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
        offset += 4;

        // offset from start of file to comment
        CommentOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
        offset += 4;

        if (CommentOffset != 0 && CommentOffset < Size)
        {
            throw new ArgumentException("Invalid Apple Disk Image comment offset.", nameof(data));
        }

        // length of comment, in bytes
        CommentLength = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
        offset += 4;

        // offset from start of file to creator data
        CreatorDataOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
        offset += 4;

        if (CreatorDataOffset != 0 && CreatorDataOffset < Size)
        {
            throw new ArgumentException("Invalid Apple Disk Image creator data offset.", nameof(data));
        }

        // length of creator data, in bytes
        CreatorDataLength = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
        offset += 4;

        // reserved, must be zero (pads header to 64 bytes)
        Reserved = new ByteArray16(data.Slice(offset, ByteArray16.Size));
        offset += ByteArray16.Size;

        Debug.Assert(offset == data.Length, "Did not consume all data for AppleDiskImagePrefix");
    }
}
