using System.Buffers.Binary;
using System.Text;
using BenchmarkDotNet.Attributes;
using AppleDiskImageReader;

namespace AppleDiskImageReader.Benchmarks;

/// <summary>
/// Benchmarks for reading and enumerating Apple disk images.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class AppleDiskImageBenchmarks
{
    private byte[] _diskData = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Build a valid 2IMG disk image in memory to avoid file I/O dependencies.
        // DOS 3.3 format: 143,360 bytes (140 KB) of image data.
        const int dataLength = 143_360;
        _diskData = new byte[AppleDiskImagePrefix.Size + dataLength];

        var header = _diskData.AsSpan(0, AppleDiskImagePrefix.Size);
        int offset = 0;

        Encoding.ASCII.GetBytes("2IMG", header.Slice(offset, 4)); offset += 4;  // signature
        Encoding.ASCII.GetBytes("BNCH", header.Slice(offset, 4)); offset += 4;  // creator
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(offset), 64);  offset += 2; // header length
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(offset), 1);   offset += 2; // version
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(offset), 0);   offset += 4; // format = DOS
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(offset), 0);   offset += 4; // flags
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(offset), 0);   offset += 4; // blocks (unused for DOS)
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(offset), 64);  offset += 4; // data offset
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(offset), dataLength); offset += 4; // data length
        // comment offset, comment length, creator data offset, creator data length, reserved = 0 (already zeroed)
    }

    [Benchmark(Description = "Create AppleDiskImage from stream")]
    public AppleDiskImage CreateDisk()
    {
        using var stream = new MemoryStream(_diskData);
        return new AppleDiskImage(stream);
    }

    [Benchmark(Description = "GetImageData to byte[]")]
    public byte[] GetImageDataToArray()
    {
        using var stream = new MemoryStream(_diskData);
        var image = new AppleDiskImage(stream);
        return image.GetImageData();
    }

    [Benchmark(Description = "GetImageData to Stream")]
    public int GetImageDataToStream()
    {
        using var stream = new MemoryStream(_diskData);
        var image = new AppleDiskImage(stream);
        return image.GetImageData(Stream.Null);
    }
}
