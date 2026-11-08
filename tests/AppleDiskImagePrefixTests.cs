namespace AppleDiskImageReader.Tests;

public class AppleDiskImagePrefixTests
{
    [Fact]
    public void Ctor_ParsesEveryField()
    {
        byte[] image = DiskImageBuilder.Create(
            AppleDiskImageFormat.Nibble,
            imageData: [1, 2],
            commentData: [3],
            creatorData: [4, 5],
            flags: 0x800001fe,
            numberOfBlocks: 123,
            dataPadding: 4);

        var prefix = new AppleDiskImagePrefix(image.AsSpan(0, AppleDiskImagePrefix.Size));

        Assert.Equal("2IMG", prefix.Signature.ToString());
        Assert.Equal("TEST", prefix.CreatorSignature.ToString());
        Assert.Equal(64, prefix.HeaderLength);
        Assert.Equal(1, prefix.Version);
        Assert.Equal(AppleDiskImageFormat.Nibble, prefix.Format);
        Assert.Equal(0x800001feu, prefix.Flags.RawValue);
        Assert.Equal(123u, prefix.NumberOfBlocks);
        Assert.Equal(68u, prefix.DataOffset);
        Assert.Equal(2u, prefix.DataLength);
        Assert.Equal(70u, prefix.CommentOffset);
        Assert.Equal(1u, prefix.CommentLength);
        Assert.Equal(71u, prefix.CreatorDataOffset);
        Assert.Equal(2u, prefix.CreatorDataLength);
        Assert.True(prefix.Reserved.IsZero);
    }

    [Fact]
    public void Ctor_AcceptsLegacy52ByteHeaderValue()
    {
        byte[] image = DiskImageBuilder.Create(headerLength: 52);

        var prefix = new AppleDiskImagePrefix(image.AsSpan(0, AppleDiskImagePrefix.Size));

        Assert.Equal(52, prefix.HeaderLength);
    }

    [Fact]
    public void Ctor_AcceptsDataOffsetAfterHeader()
    {
        byte[] image = DiskImageBuilder.Create(dataPadding: 16);

        var prefix = new AppleDiskImagePrefix(image.AsSpan(0, AppleDiskImagePrefix.Size));

        Assert.Equal(80u, prefix.DataOffset);
    }

    [Theory]
    [InlineData(AppleDiskImagePrefix.Size - 1)]
    [InlineData(AppleDiskImagePrefix.Size + 1)]
    public void Ctor_RequiresExactPrefixLength(int length)
    {
        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImagePrefix(new byte[length]));

        Assert.Equal("data", exception.ParamName);
    }

    [Fact]
    public void Ctor_RejectsInvalidSignature()
    {
        byte[] header = Header();
        header[0] = (byte)'X';

        AssertInvalid(header, "signature");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(48)]
    [InlineData(63)]
    [InlineData(65)]
    public void Ctor_RejectsUnknownHeaderLength(ushort headerLength)
    {
        byte[] header = Header();
        DiskImageBuilder.WriteUInt16(header, 8, headerLength);

        AssertInvalid(header, "header length");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Ctor_RejectsUnsupportedVersion(ushort version)
    {
        byte[] header = Header();
        DiskImageBuilder.WriteUInt16(header, 10, version);

        AssertInvalid(header, "version");
    }

    [Theory]
    [InlineData(3u)]
    [InlineData(uint.MaxValue)]
    public void Ctor_RejectsUnknownFormat(uint format)
    {
        byte[] header = Header();
        DiskImageBuilder.WriteUInt32(header, 12, format);

        AssertInvalid(header, "format");
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(63u)]
    public void Ctor_RejectsDataOffsetInsideHeader(uint dataOffset)
    {
        byte[] header = Header();
        DiskImageBuilder.WriteUInt32(header, 24, dataOffset);

        AssertInvalid(header, "data offset");
    }

    [Theory]
    [InlineData(32, 36)]
    [InlineData(40, 44)]
    public void Ctor_RejectsOptionalOffsetInsideHeader(int offsetField, int lengthField)
    {
        byte[] header = Header();
        DiskImageBuilder.WriteUInt32(header, offsetField, 10);
        DiskImageBuilder.WriteUInt32(header, lengthField, 1);

        AssertInvalid(header, "offset");
    }

    [Theory]
    [InlineData(32, 36)]
    [InlineData(40, 44)]
    public void Ctor_RejectsLengthWithoutOffset(int offsetField, int lengthField)
    {
        byte[] header = Header();
        DiskImageBuilder.WriteUInt32(header, offsetField, 0);
        DiskImageBuilder.WriteUInt32(header, lengthField, 1);

        AssertInvalid(header, "both be zero");
    }

    [Theory]
    [InlineData(32, 36)]
    [InlineData(40, 44)]
    public void Ctor_RejectsOffsetWithoutLength(int offsetField, int lengthField)
    {
        byte[] header = Header();
        DiskImageBuilder.WriteUInt32(header, offsetField, 64);
        DiskImageBuilder.WriteUInt32(header, lengthField, 0);

        AssertInvalid(header, "both be zero");
    }

    [Fact]
    public void Ctor_RejectsNonZeroReservedBytes()
    {
        byte[] header = Header();
        header[63] = 1;

        AssertInvalid(header, "reserved");
    }

    private static byte[] Header() =>
        DiskImageBuilder.Create().AsSpan(0, AppleDiskImagePrefix.Size).ToArray();

    private static void AssertInvalid(byte[] header, string messageFragment)
    {
        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImagePrefix(header));
        Assert.Equal("data", exception.ParamName);
        Assert.Contains(messageFragment, exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
