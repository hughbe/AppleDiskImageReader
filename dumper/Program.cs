using Spectre.Console;
using Spectre.Console.Cli;
using AppleDiskImageReader;

public sealed class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<ExtractCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("apple-disk-image-dumper");
            config.ValidateExamples();
        });

        return app.Run(args);
    }
}

sealed class ExtractSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    public required string Input { get; init; }

    [CommandOption("-o|--output")]
    public string? Output { get; init; }
}

sealed class ExtractCommand : AsyncCommand<ExtractSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ExtractSettings settings, CancellationToken cancellationToken)
    {
        var input = new FileInfo(settings.Input);
        if (!input.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Input file not found[/]: {Markup.Escape(input.FullName)}");
            return -1;
        }

        AnsiConsole.MarkupLine($"[cyan]Reading 2IMG file[/]: {Markup.Escape(input.Name)}");
        AnsiConsole.WriteLine();

        await using var stream = input.OpenRead();
        AppleDiskImage image;

        try
        {
            image = new AppleDiskImage(stream);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error reading 2IMG file[/]: {Markup.Escape(ex.Message)}");
            return -1;
        }

        // Display image information
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Signature", Markup.Escape(image.Prefix.Signature));
        table.AddRow("Creator", Markup.Escape(image.Prefix.CreatorSignature));
        table.AddRow("Version", image.Prefix.Version.ToString());
        table.AddRow("Format", FormatDiskFormat(image.Prefix.Format));
        table.AddRow("Flags", $"0x{image.Prefix.Flags.RawValue:X8}");

        if (image.Prefix.Format == AppleDiskImageFormat.ProDOS)
        {
            table.AddRow("Number of Blocks", image.Prefix.NumberOfBlocks.ToString());
        }

        table.AddRow("Data Length", $"{image.ImageDataLength:N0} bytes");
        table.AddRow("Comment Length", $"{image.Prefix.CommentLength:N0} bytes");
        table.AddRow("Creator Data Length", $"{image.Prefix.CreatorDataLength:N0} bytes");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Determine output path
        var outputPath = settings.Output ?? Path.GetFileNameWithoutExtension(input.Name);
        var outputDir = new DirectoryInfo(outputPath);
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        var extractedFiles = new List<string>();

        // Extract disk image data
        await AnsiConsole.Status()
            .StartAsync("Extracting disk image data...", async _ =>
            {
                var diskImagePath = Path.Combine(outputDir.FullName, GetDiskImageFileName(input.Name, image.Prefix.Format));
                await using var outputStream = File.Create(diskImagePath);
                await image.CopyImageDataToAsync(outputStream, cancellationToken);
                extractedFiles.Add(diskImagePath);
            });

        // Extract comment if present
        if (image.Prefix.CommentLength > 0)
        {
            await AnsiConsole.Status()
                .StartAsync("Extracting comment...", async _ =>
                {
                    var commentPath = Path.Combine(outputDir.FullName, "comment.txt");
                    await using var outputStream = File.Create(commentPath);
                    await image.CopyCommentDataToAsync(outputStream, cancellationToken);
                    extractedFiles.Add(commentPath);
                });
        }

        // Extract creator data if present
        if (image.Prefix.CreatorDataLength > 0)
        {
            await AnsiConsole.Status()
                .StartAsync("Extracting creator data...", async _ =>
                {
                    var creatorDataPath = Path.Combine(outputDir.FullName, "creator-data.bin");
                    await using var outputStream = File.Create(creatorDataPath);
                    await image.CopyCreatorDataToAsync(outputStream, cancellationToken);
                    extractedFiles.Add(creatorDataPath);
                });
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Extraction complete[/]");
        AnsiConsole.MarkupLine($"Output directory: [yellow]{Markup.Escape(outputDir.FullName)}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[cyan]Extracted files:[/]");
        foreach (var file in extractedFiles)
        {
            var fileInfo = new FileInfo(file);
            AnsiConsole.MarkupLine($"  - [yellow]{Markup.Escape(Path.GetFileName(file))}[/] ({fileInfo.Length:N0} bytes)");
        }

        return 0;
    }

    private static string GetDiskImageFileName(string inputFileName, AppleDiskImageFormat format)
    {
        var baseName = Path.GetFileNameWithoutExtension(inputFileName);
        return format switch
        {
            AppleDiskImageFormat.DOS => $"{baseName}.dsk",
            AppleDiskImageFormat.ProDOS => $"{baseName}.po",
            AppleDiskImageFormat.Nibble => $"{baseName}.nib",
            _ => $"{baseName}.img"
        };
    }

    private static string FormatDiskFormat(AppleDiskImageFormat format)
    {
        return format switch
        {
            AppleDiskImageFormat.DOS => "DOS 3.3 order",
            AppleDiskImageFormat.ProDOS => "ProDOS order",
            AppleDiskImageFormat.Nibble => "Nibble encoding",
            _ => $"Unknown ({(uint)format})"
        };
    }
}
