# Philiprehberger.FilesystemSnapshot

[![CI](https://github.com/philiprehberger/dotnet-filesystem-snapshot/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-filesystem-snapshot/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.FilesystemSnapshot.svg)](https://www.nuget.org/packages/Philiprehberger.FilesystemSnapshot)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-filesystem-snapshot)](LICENSE)

Snapshot a directory and detect which files were added, removed, or modified between runs. Uses SHA-256 hashes for reliable change detection.

## Install

```bash
dotnet add package Philiprehberger.FilesystemSnapshot
```

## Usage

```csharp
using Philiprehberger.FilesystemSnapshot;

// Take a snapshot
var before = DirectorySnapshot.Take("/path/to/dir");

// ... time passes, files change ...

var after = DirectorySnapshot.Take("/path/to/dir");

SnapshotDiff diff = before.CompareTo(after);

foreach (var f in diff.Added)    Console.WriteLine($"+ {f}");
foreach (var f in diff.Removed)  Console.WriteLine($"- {f}");
foreach (var f in diff.Modified) Console.WriteLine($"~ {f}");
```

### Persist between process runs

```csharp
// Save before exiting
var snapshot = DirectorySnapshot.Take("/path/to/dir");
snapshot.SaveTo("snapshot.json");

// Restore next time
var previous = DirectorySnapshot.LoadFrom("snapshot.json");
var diff = previous.CompareTo(DirectorySnapshot.Take("/path/to/dir"));
```

### Options

```csharp
var options = new SnapshotOptions
{
    SearchPattern = "*.cs",
    Recursive = true,
    ExcludePatterns = new[] { @"^bin/", @"^obj/" }
};

var snapshot = DirectorySnapshot.Take("/path/to/dir", options);
```

## API

### `DirectorySnapshot`

| Member | Description |
|--------|-------------|
| `Files` | `IReadOnlyDictionary<string, string>` — relative path to SHA-256 hash |
| `Timestamp` | UTC time the snapshot was taken |
| `Take(path, options?)` | Scan a directory and return a snapshot |
| `CompareTo(other)` | Diff this snapshot against a newer one |
| `SaveTo(filePath)` | Serialise to JSON |
| `LoadFrom(filePath)` | Deserialise from JSON |

### `SnapshotOptions`

| Property | Default | Description |
|----------|---------|-------------|
| `SearchPattern` | `"*"` | File name glob |
| `Recursive` | `true` | Include sub-directories |
| `ExcludePatterns` | `null` | Regex patterns for paths to skip |

### `SnapshotDiff`

| Property | Description |
|----------|-------------|
| `Added` | Files present in the newer snapshot only |
| `Removed` | Files present in the older snapshot only |
| `Modified` | Files present in both with different hashes |

## License

MIT
