namespace AppleDiskImageReader.Tests;

public class AppleDiskImageTests
{
    public static TheoryData<string> DiskImages =>
    [
        "ftp.apple.asimov.net/images/gs/os/gsos/GS System 3.1.2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/IIGS Universal Access.2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/ProDOS 16v1_3.2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/ProDOS 16v1_6.2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/rDOS3.3.FST.2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/Swedish Language Apple IIGS Finder System 6.0.1.2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/System 1_1(Fr).2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/System 1.2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/Systeme 6.01 FR.2mg",
        "ftp.apple.asimov.net/images/gs/os/gsos/Tool_034-System 6.x TextEdit Patch.2mg",
        "retromelsarchive.com/apple2/appletekenprog3.2mg",
        "retromelsarchive.com/apple2/compspel7.2mg",
        "retromelsarchive.com/apple2/dbaseii.2mg",
        "retromelsarchive.com/apple2/dbasesenior6.2mg",
        "retromelsarchive.com/apple2/imagewriter20.2mg",
        "retromelsarchive.com/apple2/keycat15.2mg",
        "retromelsarchive.com/apple2/klok9.2mg",
        "retromelsarchive.com/apple2/oefenopgave12.2mg",
        "retromelsarchive.com/apple2/smashbuckels8.2mg",
        "retromelsarchive.com/apple2/unnamed13.2mg",
        "retromelsarchive.com/apple2/unnamed14.2mg",
        "retromelsarchive.com/apple2/unnamed16.2mg",
        "retromelsarchive.com/apple2/unnamed17.2mg",
        "retromelsarchive.com/apple2/unnamed18.2mg",
        "retromelsarchive.com/apple2/unnamed19.2mg",
        "retromelsarchive.com/apple2/unnamed21.2mg",
        "retromelsarchive.com/apple2/visicalc5.2mg",
        "retromelsarchive.com/apple2/visitrend2.2mg",
        "sembiance.com/fileFormatSamples/archive/apple2MG/SDGS 54.2mg",
        "sembiance.com/fileFormatSamples/archive/apple2MG/SDGS_ACTIONGAMEPACKI_ERR.2mg",
        "sembiance.com/fileFormatSamples/archive/apple2MG/SDGS%25201.2mg",
        "sembiance.com/fileFormatSamples/archive/apple2MG/SDGS%25202.2mg",
        "sembiance.com/fileFormatSamples/archive/apple2MG/SDGS%25203.2mg",
        "sembiance.com/fileFormatSamples/archive/apple2MG/SDGS%25204.2mg",
        "sembiance.com/fileFormatSamples/archive/apple2MG/SDGS%25209.2mg",
        "sembiance.com/fileFormatSamples/archive/apple2MG/SGDS 96.2mg",
    ];

    [Theory]
    [MemberData(nameof(DiskImages))]
    public void SampleImages_CanBeRead(string diskName)
    {
        var filePath = Path.Combine("Samples", diskName);
        using var stream = File.OpenRead(filePath);
        var image = new AppleDiskImage(stream);
        long positionAfterHeader = stream.Position;

        byte[] data = image.GetImageData();

        long expectedLength = image.Prefix.DataLength == 0 && image.Prefix.Format == AppleDiskImageFormat.ProDOS
            ? (long)image.Prefix.NumberOfBlocks * 512
            : image.Prefix.DataLength;
        Assert.Equal(expectedLength, image.ImageDataLength);
        Assert.Equal(expectedLength, data.LongLength);
        Assert.Equal(positionAfterHeader, stream.Position);
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new AppleDiskImage(null!));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Ctor_RequiresReadableSeekableStream(bool canRead, bool canSeek)
    {
        using var stream = new CapabilityStream(DiskImageBuilder.Create(), canRead, canSeek);

        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImage(stream));

        Assert.Equal("stream", exception.ParamName);
    }

    [Fact]
    public void Ctor_RejectsTruncatedHeader()
    {
        using var stream = new MemoryStream(new byte[AppleDiskImagePrefix.Size - 1]);

        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImage(stream));

        Assert.Equal("stream", exception.ParamName);
    }

    [Fact]
    public void Ctor_RejectsStreamWhoseReportedLengthIsShorterThanHeader()
    {
        using var stream = new ReportedLengthStream(DiskImageBuilder.Create(), 63);

        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImage(stream));

        Assert.Equal("stream", exception.ParamName);
    }

    [Fact]
    public void Ctor_ReadsEmbeddedImageFromCurrentPosition()
    {
        byte[] imageBytes = DiskImageBuilder.Create(imageData: [10, 20, 30], dataPadding: 7);
        byte[] envelope = new byte[11 + imageBytes.Length + 5];
        imageBytes.CopyTo(envelope, 11);
        using var stream = new MemoryStream(envelope) { Position = 11 };

        var image = new AppleDiskImage(stream);

        Assert.Equal([10, 20, 30], image.GetImageData());
    }

    [Fact]
    public void Ctor_RejectsSectionPastEndOfStream()
    {
        byte[] imageBytes = DiskImageBuilder.Create();
        DiskImageBuilder.WriteUInt32(imageBytes, 28, 100);
        using var stream = new MemoryStream(imageBytes);

        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImage(stream));

        Assert.Equal("stream", exception.ParamName);
        Assert.Contains("image data", exception.Message);
    }

    [Fact]
    public void Ctor_RejectsCommentBeforeImageDataEnds()
    {
        byte[] imageBytes = DiskImageBuilder.Create(imageData: [1, 2, 3, 4], commentData: [5]);
        DiskImageBuilder.WriteUInt32(imageBytes, 32, AppleDiskImagePrefix.Size + 2);
        using var stream = new MemoryStream(imageBytes);

        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImage(stream));

        Assert.Equal("stream", exception.ParamName);
        Assert.Contains("comment", exception.Message);
    }

    [Fact]
    public void Ctor_RejectsCreatorDataBeforeCommentEnds()
    {
        byte[] imageBytes = DiskImageBuilder.Create(imageData: [1], commentData: [2, 3], creatorData: [4]);
        DiskImageBuilder.WriteUInt32(imageBytes, 40, AppleDiskImagePrefix.Size + 2);
        using var stream = new MemoryStream(imageBytes);

        var exception = Assert.Throws<ArgumentException>(() => new AppleDiskImage(stream));

        Assert.Equal("stream", exception.ParamName);
        Assert.Contains("creator data", exception.Message);
    }

    [Fact]
    public void DataAccessors_ReturnAllSections_AndPreserveInputPosition()
    {
        byte[] imageData = new byte[100_000];
        Random.Shared.NextBytes(imageData);
        byte[] commentData = "A comment"u8.ToArray();
        byte[] creatorData = [9, 8, 7, 6];
        using var stream = new ChunkedReadStream(
            DiskImageBuilder.Create(imageData: imageData, commentData: commentData, creatorData: creatorData),
            maxReadSize: 13);
        var image = new AppleDiskImage(stream);
        stream.Position = 7;

        Assert.Equal(imageData, image.GetImageData());
        Assert.Equal(commentData, image.GetCommentData());
        Assert.Equal(creatorData, image.GetCreatorData());

        using var output = new MemoryStream();
        Assert.Equal(imageData.Length, image.GetImageData(output));
        Assert.Equal(imageData, output.ToArray());

        output.SetLength(0);
        output.Position = 0;
        Assert.Equal(imageData.LongLength, image.CopyImageDataTo(output));
        Assert.Equal(imageData, output.ToArray());

        output.SetLength(0);
        output.Position = 0;
        Assert.Equal(commentData.LongLength, image.CopyCommentDataTo(output));
        Assert.Equal(commentData, output.ToArray());

        output.SetLength(0);
        output.Position = 0;
        Assert.Equal(creatorData.LongLength, image.CopyCreatorDataTo(output));
        Assert.Equal(creatorData, output.ToArray());
        Assert.Equal(7, stream.Position);
    }

    [Fact]
    public async Task AsyncCopyAccessors_ReturnAllSections_AndPreserveInputPosition()
    {
        byte[] imageData = new byte[100_000];
        Random.Shared.NextBytes(imageData);
        byte[] commentData = "Async comment"u8.ToArray();
        byte[] creatorData = [5, 4, 3, 2, 1];
        using var stream = new ChunkedReadStream(
            DiskImageBuilder.Create(imageData: imageData, commentData: commentData, creatorData: creatorData),
            maxReadSize: 17);
        var image = new AppleDiskImage(stream);
        stream.Position = 9;
        using var output = new MemoryStream();

        Assert.Equal(imageData.LongLength, await image.CopyImageDataToAsync(output));
        Assert.Equal(imageData, output.ToArray());

        output.SetLength(0);
        output.Position = 0;
        Assert.Equal(commentData.LongLength, await image.CopyCommentDataToAsync(output));
        Assert.Equal(commentData, output.ToArray());

        output.SetLength(0);
        output.Position = 0;
        Assert.Equal(creatorData.LongLength, await image.CopyCreatorDataToAsync(output));
        Assert.Equal(creatorData, output.ToArray());
        Assert.Equal(9, stream.Position);
    }

    [Fact]
    public async Task AsyncCopyAccessor_HonorsCancellation_AndPreservesInputPosition()
    {
        using var stream = new MemoryStream(DiskImageBuilder.Create());
        var image = new AppleDiskImage(stream);
        stream.Position = 2;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => image.CopyImageDataToAsync(Stream.Null, cancellation.Token).AsTask());
        Assert.Equal(2, stream.Position);
    }

    [Fact]
    public void ProDosImage_UsesBlockCountWhenDataLengthIsZero()
    {
        byte[] expected = new byte[1024];
        Random.Shared.NextBytes(expected);
        using var stream = new MemoryStream(DiskImageBuilder.CreateProDosWithImplicitLength(expected));
        var image = new AppleDiskImage(stream);

        Assert.Equal(1024, image.ImageDataLength);
        Assert.Equal(expected, image.GetImageData());
    }

    [Fact]
    public void EmptyImageData_ReturnsEmptyData()
    {
        using var stream = new MemoryStream(DiskImageBuilder.Create(imageData: []));
        var image = new AppleDiskImage(stream);

        Assert.Equal(0, image.ImageDataLength);
        Assert.Empty(image.GetImageData());
        Assert.Equal(0, image.GetImageData(Stream.Null));
    }

    [Fact]
    public async Task EmptyOptionalSections_ReturnEmptyData()
    {
        using var stream = new MemoryStream(DiskImageBuilder.Create());
        var image = new AppleDiskImage(stream);

        Assert.Empty(image.GetCommentData());
        Assert.Empty(image.GetCreatorData());
        Assert.Equal(0, image.GetCommentData(Stream.Null));
        Assert.Equal(0, image.GetCreatorData(Stream.Null));
        Assert.Equal(0, image.CopyCommentDataTo(Stream.Null));
        Assert.Equal(0, image.CopyCreatorDataTo(Stream.Null));
        Assert.Equal(0, await image.CopyCommentDataToAsync(Stream.Null));
        Assert.Equal(0, await image.CopyCreatorDataToAsync(Stream.Null));
    }

    [Fact]
    public void StreamAccessor_ValidatesOutputStream()
    {
        using var input = new MemoryStream(DiskImageBuilder.Create());
        var image = new AppleDiskImage(input);
        using var readOnlyOutput = new MemoryStream([], writable: false);

        Assert.Throws<ArgumentNullException>("outputStream", () => image.GetImageData(null!));
        Assert.Throws<ArgumentException>("outputStream", () => image.GetImageData(readOnlyOutput));
        Assert.Throws<ArgumentException>("outputStream", () => image.GetImageData(input));
    }

    [Fact]
    public void DataAccessor_RestoresPositionWhenInputBecomesTruncated()
    {
        using var stream = new MemoryStream(DiskImageBuilder.Create());
        var image = new AppleDiskImage(stream);
        stream.SetLength(AppleDiskImagePrefix.Size);
        stream.Position = 3;

        Assert.Throws<EndOfStreamException>(() => image.GetImageData());
        Assert.Equal(3, stream.Position);
    }

    [Fact]
    public void DataAccessors_RejectSectionsTooLargeForTheirReturnTypes()
    {
        const uint numberOfBlocks = 4_194_304;
        byte[] imageBytes = DiskImageBuilder.CreateProDosWithImplicitLength([]);
        DiskImageBuilder.WriteUInt32(imageBytes, 20, numberOfBlocks);
        long reportedLength = AppleDiskImagePrefix.Size + (long)numberOfBlocks * 512;
        using var stream = new ReportedLengthStream(imageBytes, reportedLength);
        var image = new AppleDiskImage(stream);

        Assert.Throws<NotSupportedException>(() => image.GetImageData());
        Assert.Throws<NotSupportedException>(() => image.GetImageData(Stream.Null));
    }

    private sealed class CapabilityStream(byte[] data, bool canRead, bool canSeek) : MemoryStream(data)
    {
        public override bool CanRead => canRead;

        public override bool CanSeek => canSeek;
    }

    private sealed class ChunkedReadStream(byte[] data, int maxReadSize) : MemoryStream(data)
    {
        public override int Read(Span<byte> buffer) =>
            base.Read(buffer[..Math.Min(buffer.Length, maxReadSize)]);

        public override int Read(byte[] buffer, int offset, int count) =>
            base.Read(buffer, offset, Math.Min(count, maxReadSize));
    }

    private sealed class ReportedLengthStream(byte[] data, long reportedLength) : MemoryStream(data)
    {
        public override long Length => reportedLength;
    }
}
