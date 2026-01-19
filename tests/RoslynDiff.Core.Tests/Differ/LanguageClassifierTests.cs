namespace RoslynDiff.Core.Tests.Differ;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

public class LanguageClassifierTests
{
    #region GetSensitivity - Significant Languages

    [Theory]
    [InlineData("test.py", WhitespaceSensitivity.Significant)]
    [InlineData("test.pyw", WhitespaceSensitivity.Significant)]
    [InlineData("test.yaml", WhitespaceSensitivity.Significant)]
    [InlineData("test.yml", WhitespaceSensitivity.Significant)]
    [InlineData("test.fs", WhitespaceSensitivity.Significant)]
    [InlineData("test.fsx", WhitespaceSensitivity.Significant)]
    [InlineData("test.fsi", WhitespaceSensitivity.Significant)]
    [InlineData("test.nim", WhitespaceSensitivity.Significant)]
    [InlineData("test.haml", WhitespaceSensitivity.Significant)]
    [InlineData("test.pug", WhitespaceSensitivity.Significant)]
    [InlineData("test.jade", WhitespaceSensitivity.Significant)]
    [InlineData("test.coffee", WhitespaceSensitivity.Significant)]
    [InlineData("test.slim", WhitespaceSensitivity.Significant)]
    [InlineData("test.sass", WhitespaceSensitivity.Significant)]
    [InlineData("test.styl", WhitespaceSensitivity.Significant)]
    public void GetSensitivity_SignificantExtensions_ReturnsSignificant(string path, WhitespaceSensitivity expected)
    {
        LanguageClassifier.GetSensitivity(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("Makefile")]
    [InlineData("GNUmakefile")]
    [InlineData("makefile")]
    [InlineData("/path/to/Makefile")]
    [InlineData("src/GNUmakefile")]
    public void GetSensitivity_MakefileVariants_ReturnsSignificant(string path)
    {
        LanguageClassifier.GetSensitivity(path).Should().Be(WhitespaceSensitivity.Significant);
    }

    #endregion

    #region GetSensitivity - Insignificant Languages

    [Theory]
    [InlineData("test.cs", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.vb", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.java", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.js", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.jsx", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.ts", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.tsx", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.go", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.rs", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.c", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.cpp", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.h", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.hpp", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.json", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.xml", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.html", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.htm", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.css", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.scss", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.less", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.php", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.rb", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.swift", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.kt", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.scala", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.sql", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.sh", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.bash", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.zsh", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.ps1", WhitespaceSensitivity.Insignificant)]
    [InlineData("test.psm1", WhitespaceSensitivity.Insignificant)]
    public void GetSensitivity_InsignificantExtensions_ReturnsInsignificant(string path, WhitespaceSensitivity expected)
    {
        LanguageClassifier.GetSensitivity(path).Should().Be(expected);
    }

    #endregion

    #region GetSensitivity - Unknown/Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSensitivity_NullOrEmpty_ReturnsUnknown(string? path)
    {
        LanguageClassifier.GetSensitivity(path).Should().Be(WhitespaceSensitivity.Unknown);
    }

    [Theory]
    [InlineData("test.unknown")]
    [InlineData("test.xyz")]
    [InlineData("test.abc123")]
    [InlineData("README")]
    [InlineData("LICENSE")]
    [InlineData(".gitignore")]
    public void GetSensitivity_UnknownExtensions_ReturnsUnknown(string path)
    {
        LanguageClassifier.GetSensitivity(path).Should().Be(WhitespaceSensitivity.Unknown);
    }

    [Theory]
    [InlineData("TEST.PY", WhitespaceSensitivity.Significant)]
    [InlineData("Test.Yaml", WhitespaceSensitivity.Significant)]
    [InlineData("TEST.CS", WhitespaceSensitivity.Insignificant)]
    [InlineData("Test.Java", WhitespaceSensitivity.Insignificant)]
    public void GetSensitivity_CaseInsensitive_ReturnsCorrectValue(string path, WhitespaceSensitivity expected)
    {
        LanguageClassifier.GetSensitivity(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("/Users/dev/project/src/main.py", WhitespaceSensitivity.Significant)]
    [InlineData("C:\\Projects\\App\\Program.cs", WhitespaceSensitivity.Insignificant)]
    [InlineData("./relative/path/config.yaml", WhitespaceSensitivity.Significant)]
    [InlineData("../parent/script.js", WhitespaceSensitivity.Insignificant)]
    public void GetSensitivity_WithFullPaths_ReturnsCorrectValue(string path, WhitespaceSensitivity expected)
    {
        LanguageClassifier.GetSensitivity(path).Should().Be(expected);
    }

    #endregion

    #region IsWhitespaceSignificant

    [Theory]
    [InlineData("test.py", true)]
    [InlineData("test.yaml", true)]
    [InlineData("Makefile", true)]
    [InlineData("test.cs", false)]
    [InlineData("test.js", false)]
    [InlineData("test.unknown", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsWhitespaceSignificant_ReturnsExpectedValue(string? path, bool expected)
    {
        LanguageClassifier.IsWhitespaceSignificant(path).Should().Be(expected);
    }

    #endregion
}
