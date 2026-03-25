# Changelog

## 0.1.7 (2026-03-24)

- Add unit tests
- Add test step to CI workflow

## 0.1.6 (2026-03-23)

- Shorten package description to meet 120-character limit

## 0.1.5 (2026-03-22)

- Add dates to changelog entries

## 0.1.4 (2026-03-16)

- Add Development section to README
- Add GenerateDocumentationFile and RepositoryType to .csproj

## 0.1.1 (2026-03-10)

- Fix README path in csproj so README displays on nuget.org

## 0.1.0 (2026-03-10)

- Initial release
- `SnapshotOptions` with search pattern, recursive flag, and exclude patterns
- `SnapshotDiff` record with Added, Removed, and Modified lists
- `DirectorySnapshot.Take` — scan a directory and hash every file with SHA-256
- `DirectorySnapshot.CompareTo` — diff two snapshots
- `DirectorySnapshot.SaveTo` / `LoadFrom` — JSON persistence
