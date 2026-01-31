# AppleDiskImageReader

A lightweight .NET library for reading Apple Disk Image files (2IMG format). Supports parsing 2IMG container metadata and extracting disk image data, comments, and creator-specific data. The 2IMG format is a universal container for Apple II disk images that can hold DOS, ProDOS, and nibble-encoded disk data.

## Features

- Read 2IMG (Apple Disk Image) container files
- Parse 2IMG prefix/header with full metadata
- Support for all 2IMG disk formats:
  - DOS 3.3 order
  - ProDOS order
  - Nibble encoding
- Extract raw disk image data to byte arrays or streams
- Read comment and creator-specific data sections
- Support for .NET 9.0
- Zero external dependencies (core library)

## Installation

Add the project reference to your .NET application:

```sh
dotnet add reference path/to/AppleDiskImageReader.csproj
```

Or, if published on NuGet:

```sh
dotnet add package AppleDiskImageReader
```

## Usage

### Opening a 2IMG Disk Image

```csharp
using AppleDiskImageReader;

// Open a 2IMG disk image file
using var stream = File.OpenRead("disk.2mg");

// Parse the disk image
var image = new AppleDiskImage(stream);

// Get image information
Console.WriteLine($"Signature: {image.Prefix.Signature}");
Console.WriteLine($"Creator: {image.Prefix.CreatorSignature}");
Console.WriteLine($"Format: {image.Prefix.Format}");
Console.WriteLine($"Version: {image.Prefix.Version}");
Console.WriteLine($"Data Length: {image.Prefix.DataLength} bytes");
Console.WriteLine($"Number of Blocks: {image.Prefix.NumberOfBlocks}");
```

### Extracting Disk Image Data

```csharp
// Extract the raw disk image data
byte[] diskData = image.GetImageData();
File.WriteAllBytes("disk.dsk", diskData);

// Or write directly to a stream
using var outputStream = File.Create("disk.dsk");
image.GetImageData(outputStream);
```

### Reading Comments and Creator Data

```csharp
// Read optional comment data
if (image.Prefix.CommentLength > 0)
{
    byte[] commentData = image.GetCommentData();
    string comment = System.Text.Encoding.UTF8.GetString(commentData);
    Console.WriteLine($"Comment: {comment}");
}

// Read optional creator-specific data
if (image.Prefix.CreatorDataLength > 0)
{
    byte[] creatorData = image.GetCreatorData();
    // Process creator-specific data as needed
}
```

## API Overview

### AppleDiskImage

The main class for reading 2IMG disk image containers.

- `AppleDiskImage(Stream stream)` - Opens a 2IMG disk image from a stream
- `Prefix` - Gets the 2IMG prefix containing metadata
- `GetImageData()` - Extracts disk image data as a byte array
- `GetImageData(Stream)` - Extracts disk image data to a stream
- `GetCommentData()` - Reads comment data as a byte array
- `GetCommentData(Stream)` - Reads comment data to a stream
- `GetCreatorData()` - Reads creator-specific data as a byte array
- `GetCreatorData(Stream)` - Reads creator-specific data to a stream

### AppleDiskImagePrefix

Contains the 2IMG header metadata (64 bytes):

- `Signature` - File signature ("2IMG")
- `CreatorSignature` - Creator application signature (4 characters)
- `HeaderLength` - Header length in bytes (64)
- `Version` - File format version (1)
- `Format` - Image format (DOS, ProDOS, or Nibble)
- `Flags` - Format-specific flags
- `NumberOfBlocks` - Number of 512-byte blocks (ProDOS format only)
- `DataOffset` - Offset to disk image data (64)
- `DataLength` - Length of disk image data in bytes
- `CommentOffset` - Offset to optional comment data
- `CommentLength` - Length of comment data in bytes
- `CreatorDataOffset` - Offset to optional creator-specific data
- `CreatorDataLength` - Length of creator-specific data in bytes

### AppleDiskImageFormat

Enum of supported disk image formats:

- `DOS` (0x00) - DOS 3.3 sector order
- `ProDOS` (0x01) - ProDOS block order
- `Nibble` (0x02) - Nibble encoding

### AppleDiskImageFlags

Flags field containing format-specific metadata:

- `RawValue` - Raw 32-bit flags value

## Building

Build the project using the .NET SDK:

```sh
dotnet build
```

Run tests:

```sh
dotnet test
```

## AppleDiskImageDumper CLI

Extract disk image data from a 2IMG container file. The dumper displays metadata about the 2IMG file and extracts its contents to separate files.

### Install/Build

```sh
dotnet build dumper/AppleDiskImageDumper.csproj -c Release
```

### Usage

```sh
apple-disk-image-dumper <input> [-o|--output <path>]
```

#### Arguments

- `<input>`: Path to the 2IMG disk image file (.2mg, .2img)
- `-o|--output`: Output directory for extracted files (defaults to input filename without extension)

#### Output Files

The dumper extracts the following files to the output directory:

- **Disk image**: Named based on format with appropriate extension:
  - `.dsk` - DOS 3.3 order format
  - `.po` - ProDOS order format
  - `.nib` - Nibble encoding format
- **comment.txt**: Optional comment data (if present in the 2IMG file)
- **creator-data.bin**: Optional creator-specific data (if present)

#### Example

```sh
# Extract to default directory
apple-disk-image-dumper disk.2mg

# Extract to specific directory
apple-disk-image-dumper disk.2mg -o extracted

# Output:
# Reading 2IMG file: disk.2mg
#
# ┌──────────────────────┬────────────────────┐
# │ Property             │ Value              │
# ├──────────────────────┼────────────────────┤
# │ Signature            │ 2IMG               │
# │ Creator              │ CPII               │
# │ Version              │ 1                  │
# │ Format               │ DOS 3.3 order      │
# │ Flags                │ 0x00000000         │
# │ Data Length          │ 143,360 bytes      │
# │ Comment Length       │ 0 bytes            │
# │ Creator Data Length  │ 0 bytes            │
# └──────────────────────┴────────────────────┘
#
# Extraction complete
# Output directory: /path/to/disk
#
# Extracted files:
#   - disk.dsk (143,360 bytes)
```

## Requirements

- .NET 9.0 or later

## License

MIT License. See [LICENSE](LICENSE) for details.

Copyright (c) 2025 Hugh Bellamy

## About the 2IMG Format

The 2IMG (Universal Disk Image Format) is a container format designed to preserve Apple II disk images with metadata. Created to standardize disk image preservation, it supports:

- Multiple disk ordering formats (DOS, ProDOS, nibble)
- Optional comment and creator-specific data sections
- Metadata about the disk image format and size
- 64-byte header followed by disk image data

The format specification includes:
- Magic signature: "2IMG" (0x32494D47)
- Version 1 specification
- Little-endian byte ordering
- Support for 140KB 5.25" disks and larger ProDOS volumes

## Related Projects

- [DiskCopyReader](https://github.com/hughbe/DiskCopyReader) - Reader for Disk Copy 4.2 (.dc42) images
- [AppleIIDiskReader](https://github.com/hughbe/AppleIIDiskReader) - Reader for Apple II DOS 3.3 volumes
- [ProDosVolumeReader](https://github.com/hughbe/ProDosVolumeReader) - Reader for ProDOS volumes
- [MfsReader](https://github.com/hughbe/MfsReader) - Reader for MFS (Macintosh File System) volumes
- [HfsReader](https://github.com/hughbe/HfsReader) - Reader for HFS (Hierarchical File System) volumes

## Documentation

- [2IMG Format Specification - CiderPress2](https://ciderpress2.com/formatdoc/TwoIMG-notes.html)
- [2MG (or 2IMG) Disk Image Files](https://gswv.apple2.org.za/a2zine/Docs/DiskImage_2MG_Info.txt)
