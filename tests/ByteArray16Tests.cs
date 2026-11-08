using AppleDiskImageReader.Utilities;

namespace AppleDiskImageReader.Tests;

public class ByteArray16Tests
{
    [Fact]
    public void Ctor_RequiresSixteenBytes()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ByteArray16(new byte[15]));

        Assert.Equal("data", exception.ParamName);
    }

    [Fact]
    public void SpansExposeAllBytes()
    {
        byte[] expected = Enumerable.Range(0, ByteArray16.Size).Select(value => (byte)value).ToArray();
        var value = new ByteArray16(expected);

        Assert.Equal(expected, value.AsReadOnlySpan().ToArray());
        Assert.False(value.IsZero);

        value.AsSpan().Clear();

        Assert.True(value.IsZero);
    }
}
