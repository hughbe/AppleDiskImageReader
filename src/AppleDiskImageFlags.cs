using System.Buffers.Binary;
using System.Diagnostics;

namespace AppleDiskImageReader;

/// <summary>
/// Represents the flags of an Apple Disk Image.
/// </summary>
public readonly struct AppleDiskImageFlags
{
    /// <summary>
    /// The size of the Apple Disk Image flags in bytes.
    /// </summary>
    public const int Size = 4;

    /// <summary>
    /// Gets the raw flags value.
    /// </summary>
    public uint RawValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleDiskImageFlags"/> struct.
    /// </summary>
    /// <param name="rawValue">The raw flags value.</param>
    public AppleDiskImageFlags(uint rawValue)
    {
        RawValue = rawValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleDiskImageFlags"/> struct.
    /// </summary>
    /// <param name="data">The flags data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to <see cref="Size"/>.</exception>
    public AppleDiskImageFlags(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes.", nameof(data));
        }

        int offset = 0;

        RawValue = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        Debug.Assert(offset == data.Length, "Did not consume all data for AppleDiskImageFlags");
    }

    /// <inheritdoc/>
    public override string ToString() => $"0x{RawValue:X8}";
}
