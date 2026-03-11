# Changelog

## 0.1.1 (2026-03-10)

- Fix README path in csproj so README displays on nuget.org

## 0.1.0 (2026-03-10)

- Initial release
- `SnapshotOptions` with search pattern, recursive flag, and exclude patterns
- `SnapshotDiff` record with Added, Removed, and Modified lists
- `DirectorySnapshot.Take` — scan a directory and hash every file with SHA-256
- `DirectorySnapshot.CompareTo` — diff two snapshots
- `DirectorySnapshot.SaveTo` / `LoadFrom` — JSON persistence
