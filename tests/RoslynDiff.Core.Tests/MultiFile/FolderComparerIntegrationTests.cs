namespace RoslynDiff.Core.Tests.MultiFile;

using RoslynDiff.Core.Models;
using RoslynDiff.Core.MultiFile;
using Xunit;

/// <summary>
/// Integration tests for <see cref="FolderComparer"/> with realistic scenarios.
/// </summary>
public sealed class FolderComparerIntegrationTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _oldFolder;
    private readonly string _newFolder;

    public FolderComparerIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"FolderComparerIntegration_{Guid.NewGuid()}");
        _oldFolder = Path.Combine(_tempRoot, "old");
        _newFolder = Path.Combine(_tempRoot, "new");

        Directory.CreateDirectory(_oldFolder);
        Directory.CreateDirectory(_newFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Compare_RealisticCSharpProject_DetectsAllChanges()
    {
        // Arrange - Create a realistic C# project structure
        var oldSrc = Path.Combine(_oldFolder, "src");
        var newSrc = Path.Combine(_newFolder, "src");
        Directory.CreateDirectory(oldSrc);
        Directory.CreateDirectory(newSrc);

        // Old version
        File.WriteAllText(Path.Combine(oldSrc, "Calculator.cs"), @"
namespace MyApp;

public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}");

        File.WriteAllText(Path.Combine(oldSrc, "Helper.cs"), @"
namespace MyApp;

public static class Helper
{
    public static void PrintResult(int result)
    {
        Console.WriteLine(result);
    }
}");

        // New version - modified Calculator, removed Helper, added new class
        File.WriteAllText(Path.Combine(newSrc, "Calculator.cs"), @"
namespace MyApp;

public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Subtract(int a, int b)
    {
        return a - b;
    }
}");

        File.WriteAllText(Path.Combine(newSrc, "Logger.cs"), @"
namespace MyApp;

public class Logger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = true };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(3, result.Files.Count);
        Assert.Single(result.Files, f => f.Status == FileChangeStatus.Modified);
        Assert.Single(result.Files, f => f.Status == FileChangeStatus.Removed);
        Assert.Single(result.Files, f => f.Status == FileChangeStatus.Added);
    }

    [Fact]
    public void Compare_WithGeneratedFiles_ExcludesCorrectly()
    {
        // Arrange
        var oldSrc = Path.Combine(_oldFolder, "src");
        var newSrc = Path.Combine(_newFolder, "src");
        Directory.CreateDirectory(oldSrc);
        Directory.CreateDirectory(newSrc);

        // Create regular and generated files
        File.WriteAllText(Path.Combine(oldSrc, "Code.cs"), "class Code { }");
        File.WriteAllText(Path.Combine(oldSrc, "Code.g.cs"), "// Generated");
        File.WriteAllText(Path.Combine(oldSrc, "Form.Designer.cs"), "// Designer");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            Recursive = true,
            ExcludePatterns = new[] { "**/*.g.cs", "**/*.Designer.cs" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.Contains("Code.cs") == true && !f.OldPath.Contains(".g.") && !f.OldPath.Contains("Designer"));
    }

    [Fact]
    public void Compare_MultiLevelDirectories_HandlesCorrectly()
    {
        // Arrange
        var structure = new[]
        {
            "src/Core/Engine.cs",
            "src/Core/Models/User.cs",
            "src/Api/Controllers/UserController.cs",
            "tests/Core/EngineTests.cs"
        };

        foreach (var path in structure)
        {
            var oldPath = Path.Combine(_oldFolder, path);
            var newPath = Path.Combine(_newFolder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(oldPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
            File.WriteAllText(oldPath, $"// Old {path}");
            File.WriteAllText(newPath, $"// New {path}");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = true };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(4, result.Files.Count);
        Assert.All(result.Files, f => Assert.Equal(FileChangeStatus.Modified, f.Status));
    }

    [Fact]
    public void Compare_OnlyCSharpFiles_FiltersCorrectly()
    {
        // Arrange
        var files = new[]
        {
            ("Program.cs", "class Program { }"),
            ("Data.json", "{\"key\": \"value\"}"),
            ("README.md", "# Project"),
            ("Config.xml", "<config></config>"),
            ("Helper.cs", "class Helper { }")
        };

        foreach (var (name, content) in files)
        {
            File.WriteAllText(Path.Combine(_oldFolder, name), content);
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            IncludePatterns = new[] { "*.cs" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(2, result.Files.Count);
        Assert.All(result.Files, f => Assert.True(f.OldPath?.EndsWith(".cs")));
    }

    [Fact]
    public void CompareParallel_LargeNumberOfFiles_PerformanceTest()
    {
        // Arrange - Create 100 files
        for (int i = 0; i < 100; i++)
        {
            var content = $"class File{i} {{ public void Method{i}() {{ }} }}";
            File.WriteAllText(Path.Combine(_oldFolder, $"File{i}.cs"), content);
            File.WriteAllText(Path.Combine(_newFolder, $"File{i}.cs"), content + "\n// Comment");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = comparer.CompareParallel(_oldFolder, _newFolder, options, folderOptions);
        stopwatch.Stop();

        // Assert
        Assert.Equal(100, result.Files.Count);
        Assert.All(result.Files, f => Assert.Equal(FileChangeStatus.Modified, f.Status));
        // Parallel processing should be reasonably fast
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Compare_BinAndObjFolders_ExcludesCorrectly()
    {
        // Arrange
        var structure = new[]
        {
            "src/Program.cs",
            "bin/Release/output.dll",
            "bin/Debug/output.dll",
            "obj/Release/temp.cs",
            "obj/Debug/temp.cs"
        };

        foreach (var path in structure)
        {
            var fullPath = Path.Combine(_oldFolder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, "content");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            Recursive = true,
            ExcludePatterns = new[] { "bin/**", "obj/**" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.Contains("src") == true);
    }

    [Fact]
    public void Compare_CombineIncludeAndExclude_FiltersCorrectly()
    {
        // Arrange
        var files = new[]
        {
            "src/Code.cs",
            "src/Code.g.cs",
            "src/Code.Designer.cs",
            "tests/Test.cs",
            "docs/README.md"
        };

        foreach (var file in files)
        {
            var fullPath = Path.Combine(_oldFolder, file);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, "content");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            Recursive = true,
            IncludePatterns = new[] { "**/*.cs" },
            ExcludePatterns = new[] { "**/*.g.cs", "**/*.Designer.cs", "tests/**" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.Contains("src/Code.cs") == true);
    }
}
