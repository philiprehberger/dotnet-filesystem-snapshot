using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Philiprehberger.FilesystemSnapshot;

/// <summary>
/// The result of comparing two <see cref="DirectorySnapshot"/> instances.
/// </summary>
public sealed record SnapshotDiff(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Modified
);

/// <summary>
/// Controls how <see cref="DirectorySnapshot.Take"/> scans the directory.
/// </summary>
public sealed record SnapshotOptions
{
    /// <summary>File name glob pattern. Defaults to <c>*</c>.</summary>
    public string SearchPattern { get; init; } = "*";

    /// <summary>Whether to scan sub-directories. Defaults to <c>true</c>.</summary>
    public bool Recursive { get; init; } = true;

    /// <summary>
    /// Optional list of regex patterns. Files whose relative paths match any
    /// pattern are excluded from the snapshot.
    /// </summary>
    public IReadOnlyList<string>? ExcludePatterns { get; init; }
}

/// <summary>
/// An immutable snapshot of a directory — a map from relative path to SHA-256 hash.
/// </summary>
public sealed class DirectorySnapshot
{
    /// <summary>Relative paths mapped to their SHA-256 hex hashes.</summary>
    public IReadOnlyDictionary<string, string> Files { get; }

    /// <summary>UTC time the snapshot was taken.</summary>
    public DateTimeOffset Timestamp { get; }

    [JsonConstructor]
    public DirectorySnapshot(IReadOnlyDictionary<string, string> files, DateTimeOffset timestamp)
    {
        Files = files;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Scans <paramref name="path"/> and returns a snapshot of its contents.
    /// </summary>
    public static DirectorySnapshot Take(string path, SnapshotOptions? options = null)
    {
        options ??= new SnapshotOptions();

        var searchOption = options.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var excludeRegexes = options.ExcludePatterns?
            .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .ToList();

        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var root = new DirectoryInfo(path);

        foreach (var file in root.EnumerateFiles(options.SearchPattern, searchOption))
        {
            var relativePath = Path.GetRelativePath(path, file.FullName)
                .Replace('\\', '/');

            if (excludeRegexes != null && excludeRegexes.Any(r => r.IsMatch(relativePath)))
                continue;

            files[relativePath] = ComputeHash(file.FullName);
        }

        return new DirectorySnapshot(files, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Returns the diff between this snapshot (baseline) and <paramref name="other"/> (newer).
    /// </summary>
    public SnapshotDiff CompareTo(DirectorySnapshot other)
    {
        var added = new List<string>();
        var removed = new List<string>();
        var modified = new List<string>();

        foreach (var key in other.Files.Keys)
        {
            if (!Files.TryGetValue(key, out var oldHash))
                added.Add(key);
            else if (oldHash != other.Files[key])
                modified.Add(key);
        }

        foreach (var key in Files.Keys)
        {
            if (!other.Files.ContainsKey(key))
                removed.Add(key);
        }

        added.Sort(StringComparer.OrdinalIgnoreCase);
        removed.Sort(StringComparer.OrdinalIgnoreCase);
        modified.Sort(StringComparer.OrdinalIgnoreCase);

        return new SnapshotDiff(added, removed, modified);
    }

    /// <summary>Serialises the snapshot to a JSON file.</summary>
    public void SaveTo(string filePath)
    {
        var json = JsonSerializer.Serialize(this, SnapshotJsonContext.Default.DirectorySnapshot);
        File.WriteAllText(filePath, json);
    }

    /// <summary>Deserialises a snapshot from a JSON file written by <see cref="SaveTo"/>.</summary>
    public static DirectorySnapshot LoadFrom(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize(json, SnapshotJsonContext.Default.DirectorySnapshot)
               ?? throw new InvalidOperationException("Failed to deserialise snapshot.");
    }

    private static string ComputeHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

[JsonSerializable(typeof(DirectorySnapshot))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class SnapshotJsonContext : JsonSerializerContext { }
