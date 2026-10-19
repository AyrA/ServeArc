# ServeArc

A simple HTTP server that serves files directly from common archive formats.
You can load multiple archives at once in order to browse them simultaneously.

## Supported archives

ServeArc currently supports zip and tar files.

### Zip

Full support (all files and directories)

### Tar

Partial support (no support for symbolic links)

Tar files are also supported inside a gzip compressed file (`.tar.gz` or `.tgz`).
To facilitate random access, the tar file is decompressed into your temp directory.

## Usage

Command: `SaveArc [/NB] [/AS | /SR ] <file> [file ...]`

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

Archive to serve files from. This argument is repeatable.

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
