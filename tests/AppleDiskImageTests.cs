using System.Diagnostics;

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
    public void Ctor_Stream(string diskName)
    {
        var filePath = Path.Combine("Samples", diskName);
        using var stream = File.OpenRead(filePath);
        var image = new AppleDiskImage(stream);

        // Create output directory based on disk name (without extension)
        string diskNameWithoutExtension = Path.GetFileNameWithoutExtension(diskName);
        string outputDir = Path.Combine("Output", diskNameWithoutExtension);

        // Delete the output directory if it exists
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        Directory.CreateDirectory(outputDir);

        Debug.WriteLine($"Extracting disk image '{diskName}' to '{outputDir} (Format: {image.Prefix.Format})'");

        // Extract data
        var data = image.GetImageData();
        File.WriteAllBytes(Path.Combine(outputDir, "data.bin"), data);

        // Extract comment data if present
        if (image.Prefix.CommentLength > 0)
        {
            var commentData = image.GetCommentData();
            File.WriteAllBytes(Path.Combine(outputDir, "comment.bin"), commentData);
        }

        // Extract creator data if present
        if (image.Prefix.CreatorDataLength > 0)
        {
            var creatorData = image.GetCreatorData();
            File.WriteAllBytes(Path.Combine(outputDir, "creator.bin"), creatorData);
        }
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new AppleDiskImage(null!));
    }
}
