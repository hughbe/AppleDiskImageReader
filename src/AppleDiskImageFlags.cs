using System.Buffers.Binary;
using System.Diagnostics;

namespace AppleDiskImageReader;

/// <summary>
/// Represents the flags of an Apple Disk Image.
/// </summary>
public readonly struct AppleDiskImageFlags
{
    private const uint VolumeNumberMask = 0x000000ff;
    private const uint VolumeNumberPresentMask = 0x00000100;
    private const uint WriteProtectedMask = 0x80000000;

    /// <summary>
    /// The size of the Apple Disk Image flags in bytes.
    /// </summary>
    public const int Size = 4;

    /// <summary>
    /// Gets the raw flags value.
    /// </summary>
    public uint RawValue { get; }

    /// <summary>
    /// Gets a value indicating whether a volume number is explicitly present.
    /// </summary>
    public bool HasVolumeNumber => (RawValue & VolumeNumberPresentMask) != 0;

    /// <summary>
    /// Gets the DOS 3.3 volume number, or the format-defined default of 254 when
    /// no volume number is explicitly present.
    /// </summary>
    public byte VolumeNumber => HasVolumeNumber ? (byte)(RawValue & VolumeNumberMask) : (byte)254;

    /// <summary>
    /// Gets a value indicating whether the disk image is write-protected.
    /// </summary>
    public bool IsWriteProtected => (RawValue & WriteProtectedMask) != 0;

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

        RawValue = BinaryPrimitives.ReadUInt32LittleEndian(data);
    }

    /// <inheritdoc/>
    public override string ToString() => $"0x{RawValue:X8}";
}
