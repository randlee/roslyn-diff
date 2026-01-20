namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for TFM (Target Framework Moniker) support in <see cref="PlainTextFormatter"/>.
/// </summary>
public class PlainTextFormatterTfmTests
{
    private readonly PlainTextFormatter _formatter = new();

    [Fact]
    public void Format_WithAnalyzedTfms_ShouldShowTargetFrameworksInHeader()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0" }
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("Target Frameworks: net8.0, net10.0");
    }

    [Fact]
    public void Format_WithSingleAnalyzedTfm_ShouldShowSingleTargetFramework()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0" }
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("Target Frameworks: net8.0");
    }

    [Fact]
    public void Format_WithoutAnalyzedTfms_ShouldNotShowTargetFrameworks()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = null
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().NotContain("Target Frameworks:");
    }

    [Fact]
    public void Format_WithEmptyAnalyzedTfms_ShouldNotShowTargetFrameworks()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = Array.Empty<string>()
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().NotContain("Target Frameworks:");
    }

    [Fact]
    public void Format_ChangeWithSingleTfm_ShouldShowTfmAnnotation()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Method: NewMethod [.NET 10.0]");
    }

    [Fact]
    public void Format_ChangeWithMultipleTfms_ShouldShowCommaSeparatedAnnotation()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0", "net9.0" },
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
                            Kind = ChangeKind.Class,
                            Name = "NewClass",
                            ApplicableToTfms = new[] { "net8.0", "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Class: NewClass [.NET 8.0, .NET 10.0]");
    }

    [Fact]
    public void Format_ChangeWithEmptyApplicableToTfms_ShouldNotShowAnnotation()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            Kind = ChangeKind.Property,
                            Name = "Value",
                            ApplicableToTfms = Array.Empty<string>()
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Property: Value");
        output.Should().NotContain("[+] Property: Value [");
    }

    [Fact]
    public void Format_ChangeWithNullApplicableToTfms_ShouldNotShowAnnotation()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            Kind = ChangeKind.Field,
                            Name = "_field",
                            ApplicableToTfms = null
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Field: _field");
        output.Should().NotContain("[+] Field: _field [");
    }

    [Fact]
    public void Format_NestedChangeWithTfm_ShouldShowTfmAnnotation()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            ApplicableToTfms = Array.Empty<string>(),
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "NewMethod",
                                    ApplicableToTfms = new[] { "net10.0" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[~] Class: MyClass");
        output.Should().NotContain("[~] Class: MyClass [");
        output.Should().Contain("[+] Method: NewMethod [.NET 10.0]");
    }

    [Fact]
    public void Format_ChangeWithTfmAndLocation_ShouldShowBothAnnotations()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            NewLocation = new Location { StartLine = 10, EndLine = 20 },
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Method: NewMethod (line 10-20) [.NET 10.0]");
    }

    [Fact]
    public void Format_DifferentChangeTypesWithTfm_ShouldShowCorrectMarkers()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            Name = "AddedMethod",
                            ApplicableToTfms = new[] { "net10.0" }
                        },
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "RemovedMethod",
                            ApplicableToTfms = new[] { "net8.0" }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "ModifiedMethod",
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Method: AddedMethod [.NET 10.0]");
        output.Should().Contain("[-] Method: RemovedMethod [.NET 8.0]");
        output.Should().Contain("[~] Method: ModifiedMethod [.NET 10.0]");
    }

    [Fact]
    public void Format_TfmRangeNotation_ShouldShowPlusSign()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0", "net11.0" },
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
                            ApplicableToTfms = new[] { "net10.0+" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Method: NewMethod [.NET 10.0+]");
    }

    [Fact]
    public void Format_NetStandardTfm_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "netstandard2.0", "netstandard2.1" },
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
                            Kind = ChangeKind.Class,
                            Name = "NewClass",
                            ApplicableToTfms = new[] { "netstandard2.1" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Class: NewClass [.NET Standard 2.1]");
    }

    [Fact]
    public void Format_NetFrameworkTfm_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net48", "net472" },
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
                            Name = "LegacyMethod",
                            ApplicableToTfms = new[] { "net48" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Method: LegacyMethod [.NET Framework 4.8.0]");
    }

    [Fact]
    public void Format_UnrecognizedTfm_ShouldShowAsIs()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "custom-tfm-1.0" },
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
                            Kind = ChangeKind.Class,
                            Name = "CustomClass",
                            ApplicableToTfms = new[] { "custom-tfm-1.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Class: CustomClass [custom-tfm-1.0]");
    }

    [Fact]
    public void Format_MixedTfmTypes_ShouldFormatAllCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "netstandard2.1", "net48" },
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
                            Name = "MultiTfmMethod",
                            ApplicableToTfms = new[] { "net8.0", "netstandard2.1" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Method: MultiTfmMethod [.NET 8.0, .NET Standard 2.1]");
    }

    [Fact]
    public void Format_BackwardCompatibility_NoTfmAnalysis_ShouldWorkAsExpected()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            Kind = ChangeKind.Class,
                            Name = "TestClass",
                            ApplicableToTfms = null
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().NotContain("Target Frameworks:");
        output.Should().Contain("[+] Class: TestClass");
        output.Should().NotContain("[+] Class: TestClass [");
    }

    [Fact]
    public void Format_ComplexHierarchyWithMixedTfms_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            ApplicableToTfms = Array.Empty<string>(),
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "Method1",
                                    ApplicableToTfms = new[] { "net10.0" }
                                },
                                new Change
                                {
                                    Type = ChangeType.Removed,
                                    Kind = ChangeKind.Method,
                                    Name = "Method2",
                                    ApplicableToTfms = new[] { "net8.0" }
                                },
                                new Change
                                {
                                    Type = ChangeType.Modified,
                                    Kind = ChangeKind.Property,
                                    Name = "Property1",
                                    ApplicableToTfms = Array.Empty<string>()
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[~] Class: ParentClass");
        output.Should().NotContain("[~] Class: ParentClass [");
        output.Should().Contain("[+] Method: Method1 [.NET 10.0]");
        output.Should().Contain("[-] Method: Method2 [.NET 8.0]");
        output.Should().Contain("[~] Property: Property1");
        output.Should().NotContain("[~] Property: Property1 [");
    }

    [Fact]
    public void Format_TfmWithSpecialCharacters_ShouldNotBreakFormatting()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0-windows" },
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
                            Kind = ChangeKind.Class,
                            Name = "WindowsClass",
                            ApplicableToTfms = new[] { "net8.0-windows" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+] Class: WindowsClass [net8.0-windows]");
        output.Should().NotContain("[[");
        output.Should().NotContain("]]");
    }

    [Fact]
    public void Format_MultipleFilesWithDifferentTfms_ShouldFormatEachCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "file1.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            Name = "Class1",
                            ApplicableToTfms = new[] { "net8.0" }
                        }
                    ]
                },
                new FileChange
                {
                    Path = "file2.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            Name = "Class2",
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("File: file1.cs");
        output.Should().Contain("[+] Class: Class1 [.NET 8.0]");
        output.Should().Contain("File: file2.cs");
        output.Should().Contain("[+] Class: Class2 [.NET 10.0]");
    }

    [Fact]
    public void Format_TfmAnnotation_ShouldNotAffectWhitespaceWarnings()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            Name = "TestMethod",
                            WhitespaceIssues = WhitespaceIssue.IndentationChanged | WhitespaceIssue.TrailingWhitespace,
                            ApplicableToTfms = new[] { "net10.0" }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[~] Method: TestMethod [.NET 10.0]");
        output.Should().Contain("WARNING: Whitespace issues: IndentationChanged, TrailingWhitespace");
    }

    [Fact]
    public void Format_AllChangesApplyToAllTfms_ShouldNotShowAnyAnnotations()
    {
        // Arrange
        var result = new DiffResult
        {
            AnalyzedTfms = new[] { "net8.0", "net10.0", "net9.0" },
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
                            Kind = ChangeKind.Class,
                            Name = "UniversalClass",
                            ApplicableToTfms = Array.Empty<string>()
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "UniversalMethod",
                            ApplicableToTfms = Array.Empty<string>()
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("Target Frameworks: net8.0, net10.0, net9.0");
        output.Should().Contain("[+] Class: UniversalClass");
        output.Should().NotContain("[+] Class: UniversalClass [");
        output.Should().Contain("[~] Method: UniversalMethod");
        output.Should().NotContain("[~] Method: UniversalMethod [");
    }
}
