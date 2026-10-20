# ServeArc

A simple HTTP server that serves files directly from common archive formats.
You can load multiple archives at once in order to browse them simultaneously.

## Supported archives

For types not supported by .NET internally, ServeArc utilizes 7-zip, allowing almost all 7-zip supported formats to work.

Supported archive types:

	7z, XZ, BZIP2, GZIP, TAR, ZIP, WIM, APFS, AR,
	ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT,
	GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI,
	NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF,
	UEFI, VDI, VHD, VHDX, VMDK, XAR, Z

*This list was obtained from the 7-zip homepage,*
*support for types that can only contain one file (for example bzip2) is not provided.*

### GZip compressed tar

Due to the abundance of gzip compressed tar files (`.tar.gz` or `.tgz`),
these archives are directly supported. 
Note however that this support is provided by extracting the gzip stream to a temporary file,
which might take a long time for large archives.

*The detection is purely based on the file name extension.*

## Usage

Command: `SaveArc [/NB] [/AS | /SR] <file> [file ...]`

All arguments are optional apart from the first file.

### Argument `/NB`

Disable directory browsing

### Argument `/AS`

Automatically strip root directory.
This switch removes the first directory level under the following condition:

- The archive root folder contains a single item, a directory
- The directory name matches the archive name in a case insensitive manner (without file extension)

This argument is mutually exclusive with `/SR`

### Argument `/SR`

Strip root directory.
This argument is similar to `/AS` but will not check if the directory name matches the archive file name.

### Argument `file`

Archive to serve files from. This argument is repeatable,
and archive types can be mixed.

#### File behavior for multiple archives

File download requests are matched to the archives in reverse order.
The first match will download that file.

This means files from later archives whose paths collide with earlier ones will hide those earlier files.
This allows you to patch archives (additive only)

#### Directory behavior for multiple archives

Directory listings from all archives are merged.
File and directory duplicates are removed, with entries from later archives taking precedence.

#### Example

If three archives are loaded in this order with these contents inside of them:

1. a.zip: `test/1.txt`; `test/3.txt`
2. b.zip: `test/2.txt`
3. c.zip: `test/1.txt`

When browsing the directory "test", the user will see all 3 files, `1.txt`, `2.txt`, and `3.txt` in the listing.

When downloading a file, the source is as follows:

- `1.txt`: Downloaded from `c.zip` because it overrides the file from `a.zip` due to being specified later on the command line
- `2.txt`: Downloaded from `b.zip` because it's not specified anywhere else
- `3.txt`: Downloaded from `a.zip` because it's not specified anywhere else (the last archive only overrides `1.txt`)
