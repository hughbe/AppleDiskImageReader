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
    private const string SampleDiskPath = "Samples/appleIIDos.dsk";

    [GlobalSetup]
    public void Setup()
    {
        // Load the disk image into memory once to avoid I/O overhead in benchmarks
        _diskData = File.ReadAllBytes(SampleDiskPath);
    }

    [Benchmark(Description = "Create AppleDiskImage from stream")]
    public AppleDiskImage CreateDisk()
    {
        using var stream = new MemoryStream(_diskData);
        return new AppleDiskImage(stream);
    }
}
