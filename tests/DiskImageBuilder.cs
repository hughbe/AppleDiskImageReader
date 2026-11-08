using System.Buffers.Binary;

namespace AppleDiskImageReader.Tests;

internal static class DiskImageBuilder
{
    public static byte[] Create(
        AppleDiskImageFormat format = AppleDiskImageFormat.DOS,
        byte[]? imageData = null,
        byte[]? commentData = null,
        byte[]? creatorData = null,
        ushort headerLength = AppleDiskImagePrefix.Size,
        uint flags = 0,
        uint? numberOfBlocks = null,
        int dataPadding = 0)
    {
        imageData ??= [1, 2, 3, 4];
        commentData ??= [];
        creatorData ??= [];

        int dataOffset = AppleDiskImagePrefix.Size + dataPadding;
        int commentOffset = commentData.Length == 0 ? 0 : dataOffset + imageData.Length;
        int creatorOffset = creatorData.Length == 0
            ? 0
            : (commentData.Length == 0 ? dataOffset + imageData.Length : commentOffset + commentData.Length);
        int totalLength = creatorData.Length != 0
            ? creatorOffset + creatorData.Length
            : commentData.Length != 0
                ? commentOffset + commentData.Length
                : dataOffset + imageData.Length;

        byte[] result = new byte[totalLength];
        Span<byte> header = result.AsSpan(0, AppleDiskImagePrefix.Size);
        "2IMG"u8.CopyTo(header);
        "TEST"u8.CopyTo(header[4..]);
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], headerLength);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..], 1);
        BinaryPrimitives.WriteUInt32LittleEndian(header[12..], (uint)format);
        BinaryPrimitives.WriteUInt32LittleEndian(header[16..], flags);
        BinaryPrimitives.WriteUInt32LittleEndian(header[20..], numberOfBlocks ?? (uint)(imageData.Length / 512));
        BinaryPrimitives.WriteUInt32LittleEndian(header[24..], (uint)dataOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(header[28..], (uint)imageData.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(header[32..], (uint)commentOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(header[36..], (uint)commentData.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(header[40..], (uint)creatorOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(header[44..], (uint)creatorData.Length);

        imageData.CopyTo(result, dataOffset);
        if (commentData.Length != 0)
        {
            commentData.CopyTo(result, commentOffset);
        }

        if (creatorData.Length != 0)
        {
            creatorData.CopyTo(result, creatorOffset);
        }

        return result;
    }

    public static byte[] CreateProDosWithImplicitLength(byte[] imageData)
    {
        if (imageData.Length % 512 != 0)
        {
            throw new ArgumentException("ProDOS test data must contain complete blocks.", nameof(imageData));
        }

        byte[] result = Create(
            AppleDiskImageFormat.ProDOS,
            imageData,
            numberOfBlocks: (uint)(imageData.Length / 512));
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(28), 0);
        return result;
    }

    public static void WriteUInt16(byte[] image, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(offset), value);

    public static void WriteUInt32(byte[] image, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(offset), value);
}
