namespace AppleDiskImageReader;

/// <summary>
/// Represents the format of an Apple Disk Image.
/// </summary>
public enum AppleDiskImageFormat : uint
{
    /// <summary>
    /// DOS 3.3 format.
    /// </summary>
    DOS = 0x00,

    /// <summary>
    /// ProDOS format.
    /// </summary>
    ProDOS = 0x01,

    /// <summary>
    /// Nibble format.
    /// </summary>
    Nibble = 0x02,
}
