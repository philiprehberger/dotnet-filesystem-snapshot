using Xunit;
namespace Philiprehberger.FilesystemSnapshot.Tests;

public class DirectorySnapshotTests : IDisposable
{
    private readonly string _tempDir;

    public DirectorySnapshotTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"snapshot-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Take_EmptyDirectory_ReturnsEmptyFiles()
    {
        var snapshot = DirectorySnapshot.Take(_tempDir);

        Assert.Empty(snapshot.Files);
        Assert.True(snapshot.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Take_WithFiles_CapturesAllFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "a.txt"), "hello");
        File.WriteAllText(Path.Combine(_tempDir, "b.txt"), "world");

        var snapshot = DirectorySnapshot.Take(_tempDir);

        Assert.Equal(2, snapshot.Files.Count);
        Assert.Contains("a.txt", snapshot.Files.Keys);
        Assert.Contains("b.txt", snapshot.Files.Keys);
    }

    [Fact]
    public void Take_Recursive_CapturesSubdirectoryFiles()
    {
        var subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_tempDir, "root.txt"), "root");
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

        var snapshot = DirectorySnapshot.Take(_tempDir);

        Assert.Equal(2, snapshot.Files.Count);
        Assert.Contains("sub/nested.txt", snapshot.Files.Keys);
    }

    [Fact]
    public void Take_NonRecursive_IgnoresSubdirectories()
    {
        var subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_tempDir, "root.txt"), "root");
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

        var snapshot = DirectorySnapshot.Take(_tempDir, new SnapshotOptions { Recursive = false });

        Assert.Single(snapshot.Files);
        Assert.Contains("root.txt", snapshot.Files.Keys);
    }

    [Fact]
    public void Take_WithExcludePatterns_ExcludesMatchingFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(_tempDir, "skip.log"), "skip");

        var snapshot = DirectorySnapshot.Take(_tempDir, new SnapshotOptions
        {
            ExcludePatterns = new[] { @"\.log$" }
        });

        Assert.Single(snapshot.Files);
        Assert.Contains("keep.txt", snapshot.Files.Keys);
    }

    [Fact]
    public void CompareTo_IdenticalSnapshots_ReturnsEmptyDiff()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "content");

        var snapshot1 = DirectorySnapshot.Take(_tempDir);
        var snapshot2 = DirectorySnapshot.Take(_tempDir);

        var diff = snapshot1.CompareTo(snapshot2);

        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Empty(diff.Modified);
    }

    [Fact]
    public void CompareTo_FileAdded_DetectsAddition()
    {
        File.WriteAllText(Path.Combine(_tempDir, "original.txt"), "original");
        var snapshot1 = DirectorySnapshot.Take(_tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "new.txt"), "new");
        var snapshot2 = DirectorySnapshot.Take(_tempDir);

        var diff = snapshot1.CompareTo(snapshot2);

        Assert.Single(diff.Added);
        Assert.Equal("new.txt", diff.Added[0]);
        Assert.Empty(diff.Removed);
    }

    [Fact]
    public void CompareTo_FileRemoved_DetectsRemoval()
    {
        File.WriteAllText(Path.Combine(_tempDir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "b.txt"), "b");
        var snapshot1 = DirectorySnapshot.Take(_tempDir);

        File.Delete(Path.Combine(_tempDir, "b.txt"));
        var snapshot2 = DirectorySnapshot.Take(_tempDir);

        var diff = snapshot1.CompareTo(snapshot2);

        Assert.Single(diff.Removed);
        Assert.Equal("b.txt", diff.Removed[0]);
    }

    [Fact]
    public void CompareTo_FileModified_DetectsModification()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "original");
        var snapshot1 = DirectorySnapshot.Take(_tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "modified");
        var snapshot2 = DirectorySnapshot.Take(_tempDir);

        var diff = snapshot1.CompareTo(snapshot2);

        Assert.Single(diff.Modified);
        Assert.Equal("file.txt", diff.Modified[0]);
    }

    [Fact]
    public void SaveTo_And_LoadFrom_RoundTrips()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "content");
        var snapshot = DirectorySnapshot.Take(_tempDir);

        var savePath = Path.Combine(_tempDir, "snapshot.json");
        snapshot.SaveTo(savePath);

        var loaded = DirectorySnapshot.LoadFrom(savePath);

        Assert.Equal(snapshot.Files.Count, loaded.Files.Count);
        foreach (var kvp in snapshot.Files)
        {
            Assert.Equal(kvp.Value, loaded.Files[kvp.Key]);
        }
    }
}
