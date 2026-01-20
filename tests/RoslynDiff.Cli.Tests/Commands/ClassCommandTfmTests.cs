namespace RoslynDiff.Cli.Tests.Commands;

using FluentAssertions;
using NSubstitute;
using RoslynDiff.Cli.Commands;
using Spectre.Console.Cli;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ClassCommand"/> TFM (Target Framework Moniker) functionality.
/// Tests the TFM CLI options and settings for the class command.
/// </summary>
public class ClassCommandTfmTests
{
    #region Settings Tests - Default Values

    [Fact]
    public void Settings_DefaultTargetFramework_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.TargetFramework.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultTargetFrameworks_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings();

        // Assert
        settings.TargetFrameworks.Should().BeNull();
    }

    #endregion

    #region Settings Tests - TFM Options

    [Fact]
    public void Settings_TargetFramework_WhenSet_ShouldReturnArray()
    {
        // Arrange
        var tfms = new[] { "net8.0", "net9.0" };

        // Act
        var settings = new ClassCommand.Settings
        {
            TargetFramework = tfms
        };

        // Assert
        settings.TargetFramework.Should().BeEquivalentTo(tfms);
    }

    [Fact]
    public void Settings_TargetFrameworks_WhenSet_ShouldReturnString()
    {
        // Arrange
        const string tfms = "net8.0;net9.0;net10.0";

        // Act
        var settings = new ClassCommand.Settings
        {
            TargetFrameworks = tfms
        };

        // Assert
        settings.TargetFrameworks.Should().Be(tfms);
    }

    [Fact]
    public void Settings_SingleTargetFramework_CanBeSet()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings
        {
            TargetFramework = new[] { "net8.0" }
        };

        // Assert
        settings.TargetFramework.Should().HaveCount(1);
        settings.TargetFramework![0].Should().Be("net8.0");
    }

    [Fact]
    public void Settings_MultipleTargetFrameworks_CanBeSet()
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings
        {
            TargetFramework = new[] { "net8.0", "net9.0", "net10.0" }
        };

        // Assert
        settings.TargetFramework.Should().HaveCount(3);
        settings.TargetFramework.Should().ContainInOrder("net8.0", "net9.0", "net10.0");
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("netcoreapp3.1")]
    [InlineData("netstandard2.0")]
    [InlineData("net462")]
    [InlineData("net48")]
    public void Settings_TargetFramework_WithValidFormats_CanBeSet(string tfm)
    {
        // Arrange & Act
        var settings = new ClassCommand.Settings
        {
            TargetFramework = new[] { tfm }
        };

        // Assert
        settings.TargetFramework.Should().Contain(tfm);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithBothTfmOptionsSet_ShouldStillSucceed()
    {
        // Arrange
        // Note: Validation allows both options to be set; the mutual exclusivity
        // is enforced during execution, not validation
        var settings = new ClassCommand.Settings
        {
            TargetFramework = new[] { "net8.0" },
            TargetFrameworks = "net9.0"
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue("validation should allow both TFM options");
    }

    [Fact(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    public async Task ExecuteAsync_WithMultipleSingleTfmFlags_ShouldProcessAll()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, @"
public class TestClass
{
    public void Method1() { }
}
");

            await File.WriteAllTextAsync(newFile, @"
public class TestClass
{
    public void Method1() { }
    public void Method2() { }
}
");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFramework = new[] { "net8.0", "net9.0" },
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(0, "command should succeed with multiple TFMs");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region Integration Tests - Semicolon-Separated TFMs

    [Fact(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    public async Task ExecuteAsync_WithSemicolonSeparatedTfms_ShouldParseAll()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, @"
public class TestClass
{
    public void Method1() { }
}
");

            await File.WriteAllTextAsync(newFile, @"
public class TestClass
{
    public void Method1() { }
    public void Method2() { }
}
");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFrameworks = "net8.0;net9.0;net10.0",
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(0, "command should succeed with semicolon-separated TFMs");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region Error Handling Tests - Invalid TFMs

    [Fact(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    public async Task ExecuteAsync_WithInvalidTfm_ShouldReturnError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, "public class TestClass { }");
            await File.WriteAllTextAsync(newFile, "public class TestClass { }");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFramework = new[] { "invalid-tfm" },
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(1, "command should fail with invalid TFM");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    public async Task ExecuteAsync_WithEmptyTfm_ShouldReturnError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, "public class TestClass { }");
            await File.WriteAllTextAsync(newFile, "public class TestClass { }");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFramework = new[] { "" },
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(1, "command should fail with empty TFM");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    public async Task ExecuteAsync_WithInvalidSemicolonSeparatedTfm_ShouldReturnError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, "public class TestClass { }");
            await File.WriteAllTextAsync(newFile, "public class TestClass { }");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFrameworks = "net8.0;invalid-tfm;net9.0",
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(1, "command should fail when one TFM in the list is invalid");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    public async Task ExecuteAsync_WithBothTfmOptions_ShouldReturnError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, "public class TestClass { }");
            await File.WriteAllTextAsync(newFile, "public class TestClass { }");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFramework = new[] { "net8.0" },
                TargetFrameworks = "net9.0",
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(1, "command should fail when both TFM options are specified");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region Default Behavior Tests

    [Fact(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    public async Task ExecuteAsync_WithoutTfmFlags_ShouldSucceed()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, @"
public class TestClass
{
    public void Method1() { }
}
");

            await File.WriteAllTextAsync(newFile, @"
public class TestClass
{
    public void Method1() { }
    public void Method2() { }
}
");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(0, "command should succeed without TFM flags (backward compatibility)");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region TFM Format Tests

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("netcoreapp3.1")]
    [InlineData("netstandard2.0")]
    [InlineData("net462")]
    [InlineData("net48")]
    public async Task ExecuteAsync_WithValidTfmFormats_ShouldSucceed(string tfm)
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, "public class TestClass { }");
            await File.WriteAllTextAsync(newFile, "public class TestClass { }");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFramework = new[] { tfm },
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(0, $"command should succeed with valid TFM format: {tfm}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Theory(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    [InlineData("net5")]      // Missing minor version
    [InlineData("netcore3.1")] // Wrong framework name
    [InlineData("NET8.0")]     // Should be normalized (test assumes case-insensitive parser works)
    [InlineData("mono5.0")]    // Unsupported framework
    public async Task ExecuteAsync_WithInvalidTfmFormats_ShouldReturnError(string tfm)
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, "public class TestClass { }");
            await File.WriteAllTextAsync(newFile, "public class TestClass { }");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFramework = new[] { tfm },
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            // NET8.0 should actually succeed since parser normalizes to lowercase
            // But for truly invalid formats, we expect error
            if (tfm != "NET8.0")
            {
                exitCode.Should().Be(1, $"command should fail with invalid TFM format: {tfm}");
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region Duplicate TFM Tests

    [Fact(Skip = "ClassCommand ExecuteAsync tests need refactoring - covered by integration tests")]
    public async Task ExecuteAsync_WithDuplicateTfms_ShouldDeduplicateAndSucceed()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldFile = Path.Combine(tempDir, "old.cs");
            var newFile = Path.Combine(tempDir, "new.cs");

            await File.WriteAllTextAsync(oldFile, "public class TestClass { }");
            await File.WriteAllTextAsync(newFile, "public class TestClass { }");

            var command = new ClassCommand();
            var settings = new ClassCommand.Settings
            {
                OldSpec = oldFile,
                NewSpec = newFile,
                MatchBy = "exact",
                TargetFramework = new[] { "net8.0", "net8.0", "net9.0" },
                Quiet = true
            };

            var remainingArgs = Substitute.For<IRemainingArguments>();
            var context = new CommandContext(
                Array.Empty<string>(),
                remainingArgs,
                "class",
                null);

            // Act
            var exitCode = await command.ExecuteAsync(context, settings, CancellationToken.None);

            // Assert
            exitCode.Should().Be(0, "command should succeed and deduplicate TFMs");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion
}
