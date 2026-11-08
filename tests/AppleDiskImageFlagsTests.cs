namespace AppleDiskImageReader.Tests;

public class AppleDiskImageFlagsTests
{
    [Fact]
    public void RawValueConstructor_DecodesFlags()
    {
        var flags = new AppleDiskImageFlags(0x8000017b);

        Assert.Equal(0x8000017bu, flags.RawValue);
        Assert.True(flags.HasVolumeNumber);
        Assert.Equal(123, flags.VolumeNumber);
        Assert.True(flags.IsWriteProtected);
        Assert.Equal("0x8000017B", flags.ToString());
    }

    [Fact]
    public void MissingVolumeNumber_UsesFormatDefault()
    {
        var flags = new AppleDiskImageFlags(0);

        Assert.False(flags.HasVolumeNumber);
        Assert.Equal(254, flags.VolumeNumber);
        Assert.False(flags.IsWriteProtected);
    }

    [Fact]
    public void SpanConstructor_ReadsLittleEndianValue()
    {
        var flags = new AppleDiskImageFlags(new byte[] { 0x2a, 0x01, 0x00, 0x80 });

        Assert.Equal(0x8000012au, flags.RawValue);
        Assert.Equal(42, flags.VolumeNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void SpanConstructor_RequiresFourBytes(int length)
    {
        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImageFlags(new byte[length]));

        Assert.Equal("data", exception.ParamName);
    }
}
