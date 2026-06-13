# SSD Trim Tool for Windows 7

A lightweight console utility to manually trigger TRIM operations on SSD drives in Windows 7.

## Features

- Manual TRIM trigger via zero-file technique (similar to Windows 8+ Optimize-Volume)
- TRIM status verification
- Verbose logging with timestamps
- File logging
- No external dependencies (pure .NET, .NET Framework 4.0)

## Requirements

- Windows 7 with .NET Framework 4.0 (pre-installed)
- Administrator privileges
- SSD drive with TRIM enabled
- AHCI mode (not IDE/RAID in BIOS)

## Usage

### Check TRIM Status
```
SsdTrim.exe -status [-verbose] [-log path]
```

### Perform TRIM Operation
```
SsdTrim.exe -drive C: [-verbose] [-log path]
```

### Options
- `-drive D` - Target drive letter (required for trim)
- `-status` - Query TRIM status only
- `-verbose` - Show detailed progress
- `-log path` - Write log to file

## Examples

Check TRIM status:
```
SsdTrim.exe -status
```

Trim drive C with progress:
```
SsdTrim.exe -drive C: -verbose
```

Trim drive D with log file:
```
SsdTrim.exe -drive D: -log D:\trim.log
```

## Building

```
build.bat
```

Requires .NET Framework 4.0+ (included with Windows).

## How It Works

Windows 7 NTFS sends TRIM hints automatically when files are deleted (if TRIM is enabled).

This tool creates a large zero-filled file that fills almost all free space, then deletes it. When the file is deleted, NTFS sends TRIM hints for every freed cluster — effectively trimming the entire free space area.

This replicates the "Optimize-Volume" behavior from Windows 8+ without requiring APIs that only exist in newer Windows versions.

## Troubleshooting

### "Requires Administrator privileges"
Right-click Command Prompt → "Run as Administrator"

### "TRIM is DISABLED"
Run:
```
fsutil behavior set DisableDeleteNotify 0
```

### SSD not performing well after trim?
Ensure the SSD is in AHCI mode (not IDE) in BIOS settings.

## License

MIT
