using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppleDiskImageReader.Utilities;

/// <summary>
/// An inline array of 16 bytes.
/// </summary>
[InlineArray(Size)]
public struct ByteArray16
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 16;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray16"/> struct.
    /// </summary>
    public ByteArray16(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);

    /// <summary>
    /// Gets a read-only span over the elements of the array.
    /// </summary>
    public readonly ReadOnlySpan<byte> AsReadOnlySpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _element0), Size);

    /// <summary>
    /// Gets a value indicating whether every byte is zero.
    /// </summary>
    public readonly bool IsZero => AsReadOnlySpan().IndexOfAnyExcept((byte)0) < 0;
}
