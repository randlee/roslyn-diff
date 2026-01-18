namespace RoslynDiff.Core.Tests.Comparison;

using FluentAssertions;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for ImpactClassifier.
/// </summary>
public class ImpactClassifierTests
{
    #region Rename Tests

    [Fact]
    public void Classify_PublicMethodRename_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Method,
            Visibility.Public);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
        caveats.Should().BeEmpty();
    }

    [Fact]
    public void Classify_PrivateMethodRename_ReturnsNonBreakingWithCaveat()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Method,
            Visibility.Private);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
        caveats.Should().ContainSingle()
            .Which.Should().Match(s => s.Contains("reflection") || s.Contains("serialization"));
    }

    [Fact]
    public void Classify_InternalMethodRename_ReturnsBreakingInternalApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Method,
            Visibility.Internal);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingInternalApi);
        caveats.Should().BeEmpty();
    }

    [Fact]
    public void Classify_ParameterRename_ReturnsNonBreakingWithNamedArgumentCaveat()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Parameter,
            Visibility.Local);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
        caveats.Should().ContainSingle()
            .Which.Should().Contain("named argument");
    }

    [Fact]
    public void Classify_ProtectedMethodRename_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Method,
            Visibility.Protected);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    [Fact]
    public void Classify_PrivateProtectedMethodRename_ReturnsBreakingInternalApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Method,
            Visibility.PrivateProtected);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingInternalApi);
    }

    #endregion

    #region Addition Tests

    [Fact]
    public void Classify_PublicMethodAddition_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Method,
            Visibility.Public);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
        caveats.Should().BeEmpty();
    }

    [Fact]
    public void Classify_PrivateMethodAddition_ReturnsNonBreaking()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Method,
            Visibility.Private);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
    }

    [Fact]
    public void Classify_InternalMethodAddition_ReturnsBreakingInternalApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Method,
            Visibility.Internal);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingInternalApi);
    }

    #endregion

    #region Modification Tests

    [Theory]
    [InlineData(Visibility.Public)]
    [InlineData(Visibility.Private)]
    [InlineData(Visibility.Internal)]
    [InlineData(Visibility.Protected)]
    public void Classify_BodyOnlyModification_ReturnsNonBreaking(Visibility visibility)
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Method,
            visibility,
            isSignatureChange: false);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
    }

    [Fact]
    public void Classify_PublicSignatureModification_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Method,
            Visibility.Public,
            isSignatureChange: true);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    [Fact]
    public void Classify_InternalSignatureModification_ReturnsBreakingInternalApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Method,
            Visibility.Internal,
            isSignatureChange: true);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingInternalApi);
    }

    [Fact]
    public void Classify_PrivateSignatureModification_ReturnsNonBreaking()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Method,
            Visibility.Private,
            isSignatureChange: true);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
    }

    #endregion

    #region Move Tests

    [Fact]
    public void Classify_SameScopeMove_ReturnsNonBreakingWithCaveat()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Moved,
            SymbolKind.Method,
            Visibility.Public,
            isSameScopeMove: true);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
        caveats.Should().ContainSingle()
            .Which.Should().Match(s => s.Contains("reordering") || s.Contains("same scope"));
    }

    [Fact]
    public void Classify_CrossScopeMovePublic_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Moved,
            SymbolKind.Method,
            Visibility.Public,
            isSameScopeMove: false);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    [Fact]
    public void Classify_CrossScopeMoveInternal_ReturnsBreakingInternalApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Moved,
            SymbolKind.Method,
            Visibility.Internal,
            isSameScopeMove: false);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingInternalApi);
    }

    [Fact]
    public void Classify_CrossScopeMovePrivate_ReturnsNonBreaking()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Moved,
            SymbolKind.Method,
            Visibility.Private,
            isSameScopeMove: false);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
    }

    #endregion

    #region IsFormattingOnly Tests

    [Fact]
    public void IsFormattingOnly_WhitespaceOnlyDifference_ReturnsTrue()
    {
        // Arrange
        var oldContent = "public void Method() { }";
        var newContent = "public   void   Method()   {   }";

        // Act
        var result = ImpactClassifier.IsFormattingOnly(oldContent, newContent);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFormattingOnly_IndentationDifference_ReturnsTrue()
    {
        // Arrange
        var oldContent = @"public void Method()
{
    int x = 1;
}";
        var newContent = @"public void Method()
{
        int x = 1;
}";

        // Act
        var result = ImpactClassifier.IsFormattingOnly(oldContent, newContent);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFormattingOnly_NewlinesDifference_ReturnsTrue()
    {
        // Arrange
        var oldContent = "public void Method() { int x = 1; }";
        var newContent = @"public void Method()
{
    int x = 1;
}";

        // Act
        var result = ImpactClassifier.IsFormattingOnly(oldContent, newContent);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFormattingOnly_RealCodeDifference_ReturnsFalse()
    {
        // Arrange
        var oldContent = "public void Method() { int x = 1; }";
        var newContent = "public void Method() { int x = 2; }";

        // Act
        var result = ImpactClassifier.IsFormattingOnly(oldContent, newContent);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFormattingOnly_IdentifierChange_ReturnsFalse()
    {
        // Arrange
        var oldContent = "public void MethodA() { }";
        var newContent = "public void MethodB() { }";

        // Act
        var result = ImpactClassifier.IsFormattingOnly(oldContent, newContent);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFormattingOnly_NullOldContent_ReturnsFalse()
    {
        // Act
        var result = ImpactClassifier.IsFormattingOnly(null, "some content");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFormattingOnly_NullNewContent_ReturnsFalse()
    {
        // Act
        var result = ImpactClassifier.IsFormattingOnly("some content", null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFormattingOnly_BothNull_ReturnsFalse()
    {
        // Act
        var result = ImpactClassifier.IsFormattingOnly(null, null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFormattingOnly_IdenticalContent_ReturnsTrue()
    {
        // Arrange
        var content = "public void Method() { }";

        // Act
        var result = ImpactClassifier.IsFormattingOnly(content, content);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Removal Tests

    [Fact]
    public void Classify_PublicMethodRemoval_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Method,
            Visibility.Public);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    [Fact]
    public void Classify_PrivateMethodRemoval_ReturnsNonBreaking()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Method,
            Visibility.Private);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
    }

    #endregion

    #region Field and Property Rename Tests

    [Fact]
    public void Classify_PrivateFieldRename_ReturnsNonBreakingWithCaveat()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Field,
            Visibility.Private);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
        caveats.Should().ContainSingle()
            .Which.Should().Match(s => s.Contains("reflection") || s.Contains("serialization"));
    }

    [Fact]
    public void Classify_PrivatePropertyRename_ReturnsNonBreakingWithCaveat()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Property,
            Visibility.Private);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
        caveats.Should().ContainSingle()
            .Which.Should().Match(s => s.Contains("reflection") || s.Contains("serialization"));
    }

    #endregion

    #region Protected Internal Tests

    [Fact]
    public void Classify_ProtectedInternalMethodRename_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Method,
            Visibility.ProtectedInternal);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    [Fact]
    public void Classify_ProtectedInternalMethodAddition_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Method,
            Visibility.ProtectedInternal);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    #endregion

    #region Type and Constructor Tests

    [Fact]
    public void Classify_PublicTypeRename_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Type,
            Visibility.Public);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    [Fact]
    public void Classify_PublicConstructorAddition_ReturnsBreakingPublicApi()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Constructor,
            Visibility.Public);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    #endregion

    #region Local Variable Tests

    [Fact]
    public void Classify_LocalVariableRename_ReturnsNonBreaking()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Local,
            Visibility.Local);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
        // Local variables don't get the reflection/serialization caveat
    }

    [Fact]
    public void Classify_LocalVariableAddition_ReturnsNonBreaking()
    {
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Local,
            Visibility.Local);

        // Assert
        impact.Should().Be(ChangeImpact.NonBreaking);
    }

    #endregion
}
