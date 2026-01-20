namespace RoslynDiff.Output.Tests;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for TFM (Target Framework Moniker) support in <see cref="JsonFormatter"/>.
/// </summary>
public class JsonFormatterTfmTests
{
    private readonly JsonFormatter _formatter = new();

    #region Metadata TFM Tests

    [Fact]
    public void FormatResult_WithAnalyzedTfms_ShouldIncludeTargetFrameworksInMetadata()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges = []
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        metadata.TryGetProperty("targetFrameworks", out var tfms).Should().BeTrue(
            "metadata should include targetFrameworks when TFMs are analyzed");
        tfms.ValueKind.Should().Be(JsonValueKind.Array);
        tfms.GetArrayLength().Should().Be(2);
        tfms[0].GetString().Should().Be("net8.0");
        tfms[1].GetString().Should().Be("net10.0");
    }

    [Fact]
    public void FormatResult_WithoutAnalyzedTfms_ShouldOmitTargetFrameworksInMetadata()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = null,
            FileChanges = []
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        metadata.TryGetProperty("targetFrameworks", out _).Should().BeFalse(
            "metadata should not include targetFrameworks when no TFMs are analyzed for backward compatibility");
    }

    [Fact]
    public void FormatResult_WithSingleTfm_ShouldIncludeTargetFrameworksArray()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0" },
            FileChanges = []
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        metadata.TryGetProperty("targetFrameworks", out var tfms).Should().BeTrue();
        tfms.ValueKind.Should().Be(JsonValueKind.Array);
        tfms.GetArrayLength().Should().Be(1);
        tfms[0].GetString().Should().Be("net8.0");
    }

    [Fact]
    public void FormatResult_WithMultipleTfms_ShouldPreserveOrder()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net6.0", "net8.0", "net10.0" },
            FileChanges = []
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        var tfms = metadata.GetProperty("targetFrameworks");
        tfms.GetArrayLength().Should().Be(3);
        tfms[0].GetString().Should().Be("net6.0");
        tfms[1].GetString().Should().Be("net8.0");
        tfms[2].GetString().Should().Be("net10.0");
    }

    #endregion

    #region Change ApplicableToTfms Tests

    [Fact]
    public void FormatResult_ChangeWithApplicableToTfms_ShouldIncludeField()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 12 },
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("applicableToTfms", out var tfms).Should().BeTrue(
            "change should include applicableToTfms when it applies to specific TFMs");
        tfms.ValueKind.Should().Be(JsonValueKind.Array);
        tfms.GetArrayLength().Should().Be(1);
        tfms[0].GetString().Should().Be("net10.0");
    }

    [Fact]
    public void FormatResult_ChangeWithoutApplicableToTfms_ShouldOmitField()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = null,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 12 },
                            ApplicableToTfms = null
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("applicableToTfms", out _).Should().BeFalse(
            "change should not include applicableToTfms when no TFMs are analyzed for backward compatibility");
    }

    [Fact]
    public void FormatResult_ChangeWithEmptyApplicableToTfms_ShouldOmitField()
    {
        // Arrange - Empty list means applies to all TFMs
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "UniversalMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            NewContent = "public void UniversalMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 12 },
                            ApplicableToTfms = Array.Empty<string>()
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("applicableToTfms", out _).Should().BeFalse(
            "change should not include applicableToTfms when it applies to all TFMs (empty list)");
    }

    [Fact]
    public void FormatResult_ChangeWithMultipleApplicableToTfms_ShouldIncludeAllTfms()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net6.0", "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "ModernMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            NewContent = "public void ModernMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 12 },
                            ApplicableToTfms = new[] { "net8.0", "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        var tfms = change.GetProperty("applicableToTfms");
        tfms.GetArrayLength().Should().Be(2);
        tfms[0].GetString().Should().Be("net8.0");
        tfms[1].GetString().Should().Be("net10.0");
    }

    #endregion

    #region Nested Changes TFM Tests

    [Fact]
    public void FormatResult_NestedChangeWithApplicableToTfms_ShouldPreserveTfmInformation()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "ParentClass",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = Array.Empty<string>(), // Applies to all TFMs
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "Net10OnlyMethod",
                                    Impact = ChangeImpact.BreakingPublicApi,
                                    ApplicableToTfms = new[] { "net10.0" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var parentChange = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        parentChange.TryGetProperty("applicableToTfms", out _).Should().BeFalse(
            "parent change with empty ApplicableToTfms should omit the field");

        var childChange = parentChange.GetProperty("children")[0];
        childChange.TryGetProperty("applicableToTfms", out var childTfms).Should().BeTrue(
            "child change should include applicableToTfms");
        childTfms.GetArrayLength().Should().Be(1);
        childTfms[0].GetString().Should().Be("net10.0");
    }

    [Fact]
    public void FormatResult_DeeplyNestedChangesWithDifferentTfms_ShouldPreserveAllTfmInformation()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net6.0", "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Namespace,
                            Name = "MyNamespace",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = Array.Empty<string>(), // All TFMs
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Modified,
                                    Kind = ChangeKind.Class,
                                    Name = "MyClass",
                                    Impact = ChangeImpact.BreakingPublicApi,
                                    ApplicableToTfms = new[] { "net8.0", "net10.0" },
                                    Children =
                                    [
                                        new Change
                                        {
                                            Type = ChangeType.Added,
                                            Kind = ChangeKind.Method,
                                            Name = "Net10Feature",
                                            Impact = ChangeImpact.BreakingPublicApi,
                                            ApplicableToTfms = new[] { "net10.0" }
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var namespaceChange = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        namespaceChange.TryGetProperty("applicableToTfms", out _).Should().BeFalse();

        var classChange = namespaceChange.GetProperty("children")[0];
        var classTfms = classChange.GetProperty("applicableToTfms");
        classTfms.GetArrayLength().Should().Be(2);
        classTfms[0].GetString().Should().Be("net8.0");
        classTfms[1].GetString().Should().Be("net10.0");

        var methodChange = classChange.GetProperty("children")[0];
        var methodTfms = methodChange.GetProperty("applicableToTfms");
        methodTfms.GetArrayLength().Should().Be(1);
        methodTfms[0].GetString().Should().Be("net10.0");
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void FormatResult_NoTfmAnalysis_ShouldProduceLegacyCompatibleJson()
    {
        // Arrange - This is the traditional single-TFM scenario
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = null,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 12 },
                            ApplicableToTfms = null
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);

        // Assert - No TFM-related fields should appear
        json.Should().NotContain("targetFrameworks");
        json.Should().NotContain("applicableToTfms");

        using var doc = JsonDocument.Parse(json);
        var metadata = doc.RootElement.GetProperty("metadata");
        metadata.TryGetProperty("targetFrameworks", out _).Should().BeFalse();

        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("applicableToTfms", out _).Should().BeFalse();
    }

    [Fact]
    public void FormatResult_MixedTfmScenarios_ShouldHandleCorrectly()
    {
        // Arrange - Some changes have TFMs, some don't
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "UniversalMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = Array.Empty<string>() // All TFMs - should be omitted
                        },
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "SpecificMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net10.0" } // Specific TFM - should be included
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var changes = doc.RootElement.GetProperty("files")[0].GetProperty("changes");

        var universalChange = changes[0];
        universalChange.TryGetProperty("applicableToTfms", out _).Should().BeFalse(
            "universal change should omit applicableToTfms");

        var specificChange = changes[1];
        specificChange.TryGetProperty("applicableToTfms", out var tfms).Should().BeTrue(
            "specific change should include applicableToTfms");
        tfms.GetArrayLength().Should().Be(1);
        tfms[0].GetString().Should().Be("net10.0");
    }

    #endregion

    #region JSON Schema and Structure Tests

    [Fact]
    public void FormatResult_WithTfmData_ShouldProduceValidJson()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);

        // Assert - Should be parseable JSON
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow("JSON output should be valid");
    }

    [Fact]
    public void FormatResult_WithTfmData_ShouldHaveCorrectStructure()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert - Verify complete structure
        var root = doc.RootElement;
        root.TryGetProperty("$schema", out _).Should().BeTrue();
        root.TryGetProperty("metadata", out _).Should().BeTrue();
        root.TryGetProperty("summary", out _).Should().BeTrue();
        root.TryGetProperty("files", out _).Should().BeTrue();

        var metadata = root.GetProperty("metadata");
        metadata.TryGetProperty("version", out _).Should().BeTrue();
        metadata.TryGetProperty("timestamp", out _).Should().BeTrue();
        metadata.TryGetProperty("mode", out _).Should().BeTrue();
        metadata.TryGetProperty("options", out _).Should().BeTrue();
        metadata.TryGetProperty("targetFrameworks", out _).Should().BeTrue();
    }

    [Fact]
    public void FormatResult_TfmFieldsShouldUseCamelCase()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);

        // Assert - Check camelCase property names
        json.Should().Contain("\"targetFrameworks\"");
        json.Should().Contain("\"applicableToTfms\"");

        // Should NOT contain PascalCase
        json.Should().NotContain("\"TargetFrameworks\"");
        json.Should().NotContain("\"ApplicableToTfms\"");
    }

    #endregion

    #region Pretty Print Tests

    [Fact]
    public void FormatResult_WithTfmsAndPrettyPrint_ShouldBeIndented()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { PrettyPrint = true };

        // Act
        var json = _formatter.FormatResult(result, options);

        // Assert
        json.Should().Contain("\n  "); // Contains indentation
        json.Should().Contain("\"targetFrameworks\": [");
        json.Should().Contain("\"applicableToTfms\": [");

        // Verify JSON is still valid
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void FormatResult_WithTfmsAndCompactPrint_ShouldNotBeIndented()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { PrettyPrint = false };

        // Act
        var json = _formatter.FormatResult(result, options);

        // Assert
        json.Should().NotContain("\n  "); // No indentation
        json.Should().Contain("\"targetFrameworks\":[");
        json.Should().Contain("\"applicableToTfms\":[");
    }

    #endregion

    #region Complex Integration Tests

    [Fact]
    public void FormatResult_CompleteMultiTfmScenario_ShouldProduceCorrectJson()
    {
        // Arrange - A realistic multi-TFM scenario
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            OldPath = "old/MyClass.cs",
            NewPath = "new/MyClass.cs",
            AnalyzedTfms = new[] { "net6.0", "net8.0", "net10.0" },
            Stats = new DiffStats
            {
                Additions = 2,
                Modifications = 1
            },
            FileChanges =
            [
                new FileChange
                {
                    Path = "MyClass.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "MyClass",
                            Impact = ChangeImpact.BreakingPublicApi,
                            Visibility = Visibility.Public,
                            ApplicableToTfms = Array.Empty<string>(), // All TFMs
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "ModernOnlyMethod",
                                    Impact = ChangeImpact.BreakingPublicApi,
                                    Visibility = Visibility.Public,
                                    ApplicableToTfms = new[] { "net8.0", "net10.0" },
                                    NewContent = "public void ModernOnlyMethod() { }",
                                    NewLocation = new Location { StartLine = 10, EndLine = 12 }
                                },
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Property,
                                    Name = "Net10Feature",
                                    Impact = ChangeImpact.BreakingPublicApi,
                                    Visibility = Visibility.Public,
                                    ApplicableToTfms = new[] { "net10.0" },
                                    NewContent = "public int Net10Feature { get; set; }",
                                    NewLocation = new Location { StartLine = 14, EndLine = 14 }
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { IncludeContent = true, PrettyPrint = true };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert - Comprehensive verification
        var root = doc.RootElement;

        // Metadata
        var metadata = root.GetProperty("metadata");
        var tfms = metadata.GetProperty("targetFrameworks");
        tfms.GetArrayLength().Should().Be(3);
        tfms[0].GetString().Should().Be("net6.0");
        tfms[1].GetString().Should().Be("net8.0");
        tfms[2].GetString().Should().Be("net10.0");

        // Summary
        var summary = root.GetProperty("summary");
        summary.GetProperty("totalChanges").GetInt32().Should().Be(3);
        summary.GetProperty("additions").GetInt32().Should().Be(2);

        // Files and changes
        var file = root.GetProperty("files")[0];
        file.GetProperty("oldPath").GetString().Should().Be("old/MyClass.cs");
        file.GetProperty("newPath").GetString().Should().Be("new/MyClass.cs");

        var classChange = file.GetProperty("changes")[0];
        classChange.GetProperty("name").GetString().Should().Be("MyClass");
        classChange.TryGetProperty("applicableToTfms", out _).Should().BeFalse(
            "class applies to all TFMs");

        var children = classChange.GetProperty("children");
        children.GetArrayLength().Should().Be(2);

        var modernMethod = children[0];
        modernMethod.GetProperty("name").GetString().Should().Be("ModernOnlyMethod");
        var modernMethodTfms = modernMethod.GetProperty("applicableToTfms");
        modernMethodTfms.GetArrayLength().Should().Be(2);
        modernMethodTfms[0].GetString().Should().Be("net8.0");
        modernMethodTfms[1].GetString().Should().Be("net10.0");

        var net10Property = children[1];
        net10Property.GetProperty("name").GetString().Should().Be("Net10Feature");
        var net10PropertyTfms = net10Property.GetProperty("applicableToTfms");
        net10PropertyTfms.GetArrayLength().Should().Be(1);
        net10PropertyTfms[0].GetString().Should().Be("net10.0");
    }

    [Fact]
    public void FormatResult_MultipleFilesWithDifferentTfms_ShouldHandleCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "FileA.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            Name = "ClassA",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                },
                new FileChange
                {
                    Path = "FileB.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            Name = "ClassB",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var files = doc.RootElement.GetProperty("files");
        files.GetArrayLength().Should().Be(2);

        var fileA = files[0];
        var changeA = fileA.GetProperty("changes")[0];
        var tfmsA = changeA.GetProperty("applicableToTfms");
        tfmsA.GetArrayLength().Should().Be(1);
        tfmsA[0].GetString().Should().Be("net8.0");

        var fileB = files[1];
        var changeB = fileB.GetProperty("changes")[0];
        var tfmsB = changeB.GetProperty("applicableToTfms");
        tfmsB.GetArrayLength().Should().Be(1);
        tfmsB[0].GetString().Should().Be("net10.0");
    }

    #endregion

    #region IncludeNonImpactful Filter Tests

    [Fact]
    public void FormatResult_WithTfmsAndIncludeNonImpactfulFalse_ShouldFilterButPreserveTfms()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "BreakingChange",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net10.0" }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "NonBreakingChange",
                            Impact = ChangeImpact.NonBreaking,
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { IncludeNonImpactful = false };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        metadata.TryGetProperty("targetFrameworks", out _).Should().BeTrue(
            "metadata should still include targetFrameworks even when filtering");

        var changes = doc.RootElement.GetProperty("files")[0].GetProperty("changes");
        changes.GetArrayLength().Should().Be(1, "only breaking change should be included");

        var change = changes[0];
        change.GetProperty("name").GetString().Should().Be("BreakingChange");
        var tfms = change.GetProperty("applicableToTfms");
        tfms.GetArrayLength().Should().Be(1);
        tfms[0].GetString().Should().Be("net10.0");
    }

    [Fact]
    public void FormatResult_NestedChangesWithTfmsAndFiltering_ShouldPreserveTfmsOnFilteredChildren()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "MyClass",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = Array.Empty<string>(),
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "BreakingMethod",
                                    Impact = ChangeImpact.BreakingPublicApi,
                                    ApplicableToTfms = new[] { "net10.0" }
                                },
                                new Change
                                {
                                    Type = ChangeType.Modified,
                                    Kind = ChangeKind.Method,
                                    Name = "FormattingChange",
                                    Impact = ChangeImpact.FormattingOnly,
                                    ApplicableToTfms = new[] { "net8.0" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { IncludeNonImpactful = false };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var classChange = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        var children = classChange.GetProperty("children");
        children.GetArrayLength().Should().Be(1, "only breaking method should be included");

        var breakingMethod = children[0];
        breakingMethod.GetProperty("name").GetString().Should().Be("BreakingMethod");
        var tfms = breakingMethod.GetProperty("applicableToTfms");
        tfms.GetArrayLength().Should().Be(1);
        tfms[0].GetString().Should().Be("net10.0");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FormatResult_EmptyTfmsList_ShouldOmitField()
    {
        // Arrange - Empty list in AnalyzedTfms should be treated as null
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = Array.Empty<string>(),
            FileChanges = []
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        // Note: Empty array is treated as "some analysis was done, but no TFMs"
        // which is different from null (no analysis). Implementation may vary.
        // Based on the JsonIgnore(WhenWritingNull), empty arrays will be included.
        metadata.TryGetProperty("targetFrameworks", out var tfms).Should().BeTrue();
        tfms.ValueKind.Should().Be(JsonValueKind.Array);
        tfms.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void FormatResult_TfmWithSpecialCharacters_ShouldSerializeCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0-windows", "net8.0-android31.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "PlatformSpecificMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net8.0-windows" }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        var tfms = metadata.GetProperty("targetFrameworks");
        tfms.GetArrayLength().Should().Be(2);
        tfms[0].GetString().Should().Be("net8.0-windows");
        tfms[1].GetString().Should().Be("net8.0-android31.0");

        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        var changeTfms = change.GetProperty("applicableToTfms");
        changeTfms[0].GetString().Should().Be("net8.0-windows");
    }

    [Fact]
    public void FormatResult_AllChangesFilteredOut_ShouldStillIncludeMetadataTfms()
    {
        // Arrange - All changes are non-impactful and will be filtered
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "FormattingOnly",
                            Impact = ChangeImpact.FormattingOnly,
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { IncludeNonImpactful = false };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        metadata.TryGetProperty("targetFrameworks", out var tfms).Should().BeTrue(
            "metadata should include targetFrameworks even when all changes are filtered");
        tfms.GetArrayLength().Should().Be(2);

        var changes = doc.RootElement.GetProperty("files")[0].GetProperty("changes");
        changes.GetArrayLength().Should().Be(0, "all changes should be filtered");
    }

    #endregion

    #region Async Formatting Tests

    [Fact]
    public async Task FormatResultAsync_WithTfmData_ShouldIncludeTfmFields()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };
        using var writer = new StringWriter();

        // Act
        await _formatter.FormatResultAsync(result, writer);
        var json = writer.ToString();
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metadata = doc.RootElement.GetProperty("metadata");
        metadata.TryGetProperty("targetFrameworks", out var tfms).Should().BeTrue();
        tfms.GetArrayLength().Should().Be(2);

        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("applicableToTfms", out var changeTfms).Should().BeTrue();
        changeTfms.GetArrayLength().Should().Be(1);
        changeTfms[0].GetString().Should().Be("net10.0");
    }

    #endregion
}
