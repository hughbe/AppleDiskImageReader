using AppleDiskImageReader.Utilities;

namespace AppleDiskImageReader.Tests;

public class String4Tests
{
    [Fact]
    public void Ctor_RequiresFourBytes()
    {
        var exception = Assert.Throws<ArgumentException>(() => new String4(new byte[3]));

        Assert.Equal("data", exception.ParamName);
    }

    [Fact]
    public void LengthAndToString_StopAtNullTerminator()
    {
        var value = new String4(new byte[] { (byte)'A', (byte)'B', 0, (byte)'Z' });

        Assert.Equal(2, value.Length);
        Assert.Equal("AB", value.ToString());
        Assert.Equal("AB", ((IFormattable)value).ToString(null, null));
    }

    [Fact]
    public void AsSpan_AllowsUpdatingValue()
    {
        var value = new String4("TEST"u8);

        value.AsSpan()[0] = (byte)'B';

        Assert.Equal("BEST", value.ToString());
        Assert.Equal("BEST"u8.ToArray(), value.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void TryFormat_WritesValueWithoutAllocating()
    {
        var value = new String4("ABCD"u8);
        Span<char> destination = stackalloc char[4];

        Assert.True(value.TryFormat(destination, out int charsWritten));
        Assert.Equal(4, charsWritten);
        Assert.Equal("ABCD", destination.ToString());

        destination.Clear();
        Assert.True(((ISpanFormattable)value).TryFormat(destination, out charsWritten, default, null));
        Assert.Equal(4, charsWritten);
        Assert.Equal("ABCD", destination.ToString());
    }

    [Fact]
    public void TryFormat_ReturnsFalseWhenDestinationIsTooSmall()
    {
        var value = new String4("ABCD"u8);
        Span<char> destination = stackalloc char[3];

        Assert.False(value.TryFormat(destination, out int charsWritten));
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void Equality_HandlesBytesCharactersObjectsAndOperators()
    {
        var value = new String4("ABCD"u8);
        var same = new String4("ABCD"u8);
        var different = new String4("ABCE"u8);

        Assert.True(value.Equals("ABCD".AsSpan()));
        Assert.False(value.Equals("ABC".AsSpan()));
        Assert.False(value.Equals("ABCE".AsSpan()));
        Assert.True(value.Equals("ABCD"u8));
        Assert.False(value.Equals("ABC"u8));
        Assert.True(value.Equals(same));
        Assert.False(value.Equals(different));
        Assert.True(value.Equals((object)same));
        Assert.False(value.Equals((object)"ABCD"));
        Assert.True(value == same);
        Assert.True(value != different);
        Assert.Equal(value.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Equality_IgnoresBytesAfterNullTerminator()
    {
        var first = new String4(new byte[] { (byte)'A', 0, 1, 0 });
        var second = new String4(new byte[] { (byte)'A', 0, 2, 0 });
        var different = new String4(new byte[] { (byte)'B', 0, 1, 0 });

        Assert.True(first.Equals(second));
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals("A"u8));
        Assert.True(first != different);
    }

    [Fact]
    public void NonAsciiBytes_AreLatin1InToStringTryFormatAndEquals()
    {
        var value = new String4(new byte[] { 0xE9, (byte)'A', 0, 0 });
        Span<char> destination = stackalloc char[4];

        Assert.True(value.TryFormat(destination, out int charsWritten));

        Assert.Equal("éA", value.ToString());
        Assert.Equal("éA", destination[..charsWritten].ToString());
        Assert.True(value.Equals("éA".AsSpan()));
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsText()
    {
        var value = new String4("ABCD"u8);

        string result = value;

        Assert.Equal("ABCD", result);
    }
}
