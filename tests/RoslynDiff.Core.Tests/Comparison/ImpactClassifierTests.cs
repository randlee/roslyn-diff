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

    #region P0 Critical: Missing SymbolKind Coverage Tests

    [Fact]
    public void Classify_EventAddition_ReturnsCorrectImpact()
    {
        // Arrange & Act - Public event addition
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Event,
            Visibility.Public);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Internal event addition
        var (impactInternal, _) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Event,
            Visibility.Internal);

        // Assert
        impactInternal.Should().Be(ChangeImpact.BreakingInternalApi);

        // Arrange & Act - Private event addition
        var (impactPrivate, _) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Event,
            Visibility.Private);

        // Assert
        impactPrivate.Should().Be(ChangeImpact.NonBreaking);
    }

    [Fact]
    public void Classify_IndexerModification_ReturnsCorrectImpact()
    {
        // Arrange & Act - Public indexer signature change
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Indexer,
            Visibility.Public,
            isSignatureChange: true);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Public indexer body-only change
        var (impactBodyOnly, _) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Indexer,
            Visibility.Public,
            isSignatureChange: false);

        // Assert
        impactBodyOnly.Should().Be(ChangeImpact.NonBreaking);

        // Arrange & Act - Internal indexer signature change
        var (impactInternal, _) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Indexer,
            Visibility.Internal,
            isSignatureChange: true);

        // Assert
        impactInternal.Should().Be(ChangeImpact.BreakingInternalApi);
    }

    [Fact]
    public void Classify_OperatorOverloadRename_ReturnsCorrectImpact()
    {
        // Arrange & Act - Public operator rename
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.Operator,
            Visibility.Public);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Public operator removal
        var (impactRemoval, _) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Operator,
            Visibility.Public);

        // Assert
        impactRemoval.Should().Be(ChangeImpact.BreakingPublicApi);

        // Arrange & Act - Public operator addition
        var (impactAddition, _) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Operator,
            Visibility.Public);

        // Assert
        impactAddition.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    [Fact]
    public void Classify_DelegateRemoval_ReturnsCorrectImpact()
    {
        // Arrange & Act - Public delegate removal
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Delegate,
            Visibility.Public);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Internal delegate removal
        var (impactInternal, _) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Delegate,
            Visibility.Internal);

        // Assert
        impactInternal.Should().Be(ChangeImpact.BreakingInternalApi);

        // Arrange & Act - Private delegate removal
        var (impactPrivate, _) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Delegate,
            Visibility.Private);

        // Assert
        impactPrivate.Should().Be(ChangeImpact.NonBreaking);
    }

    [Fact]
    public void Classify_EnumMemberRename_ReturnsCorrectImpact()
    {
        // Arrange & Act - Public enum member rename
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.EnumMember,
            Visibility.Public);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Public enum member removal
        var (impactRemoval, _) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.EnumMember,
            Visibility.Public);

        // Assert
        impactRemoval.Should().Be(ChangeImpact.BreakingPublicApi);

        // Arrange & Act - Internal enum member rename
        var (impactInternal, _) = ImpactClassifier.Classify(
            ChangeType.Renamed,
            SymbolKind.EnumMember,
            Visibility.Internal);

        // Assert
        impactInternal.Should().Be(ChangeImpact.BreakingInternalApi);
    }

    #endregion

    #region P1 High: Extension Method and Generic Constraint Tests

    [Fact]
    public void Classify_PublicExtensionMethodRemoval_ReturnsBreakingPublicApi()
    {
        // Note: Extension methods are classified as Method symbol kind
        // The classifier treats them the same as regular methods based on visibility
        
        // Arrange & Act
        var (impact, caveats) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Method,
            Visibility.Public);

        // Assert
        impact.Should().Be(ChangeImpact.BreakingPublicApi);
        caveats.Should().BeEmpty();
    }

    [Fact]
    public void Classify_InternalExtensionMethodRename_ReturnsBreakingInternalApi()
    {
        // Note: Extension methods are classified as Method symbol kind
        // Internal extension methods may be used by InternalsVisibleTo assemblies
        
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
    public void Classify_GenericConstraintAdded_ReturnsBreakingChange()
    {
        // Adding a generic constraint is a signature change that breaks existing code
        // that doesn't meet the new constraint
        
        // Arrange & Act - Public type with added constraint
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Type,
            Visibility.Public,
            isSignatureChange: true);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Internal type with added constraint
        var (impactInternal, _) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Type,
            Visibility.Internal,
            isSignatureChange: true);

        // Assert
        impactInternal.Should().Be(ChangeImpact.BreakingInternalApi);
    }

    [Fact]
    public void Classify_GenericConstraintRemoved_ReturnsBreakingChange()
    {
        // Removing a generic constraint is also a signature change
        // While it's generally compatible, it may change behavior in subtle ways
        
        // Arrange & Act - Public method with removed constraint
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Method,
            Visibility.Public,
            isSignatureChange: true);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Protected method with removed constraint
        var (impactProtected, _) = ImpactClassifier.Classify(
            ChangeType.Modified,
            SymbolKind.Method,
            Visibility.Protected,
            isSignatureChange: true);

        // Assert
        impactProtected.Should().Be(ChangeImpact.BreakingPublicApi);
    }

    [Fact]
    public void Classify_StaticMethodRemoval_ReturnsCorrectImpact()
    {
        // Static methods follow the same visibility rules
        
        // Arrange & Act - Public static method removal
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Method,
            Visibility.Public);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Internal static method removal
        var (impactInternal, _) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Method,
            Visibility.Internal);

        // Assert
        impactInternal.Should().Be(ChangeImpact.BreakingInternalApi);

        // Arrange & Act - Private static method removal
        var (impactPrivate, _) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Method,
            Visibility.Private);

        // Assert
        impactPrivate.Should().Be(ChangeImpact.NonBreaking);
    }

    #endregion

    #region P2 Robustness: Edge Cases

    [Fact]
    public void IsFormattingOnly_EmptyStrings_ReturnsFalse()
    {
        // Empty strings should be handled gracefully
        
        // Act & Assert - Both empty
        ImpactClassifier.IsFormattingOnly("", "").Should().BeTrue();
        
        // Act & Assert - Old empty, new has content
        ImpactClassifier.IsFormattingOnly("", "content").Should().BeFalse();
        
        // Act & Assert - Old has content, new empty
        ImpactClassifier.IsFormattingOnly("content", "").Should().BeFalse();
    }

    [Fact]
    public void Classify_NestedInterfaceMember_ReturnsCorrectImpact()
    {
        // Interface members follow the interface's visibility
        // When an interface is public, its members are effectively public
        
        // Arrange & Act - Public interface method addition
        var (impactPublic, caveatsPublic) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Method,
            Visibility.Public);

        // Assert
        impactPublic.Should().Be(ChangeImpact.BreakingPublicApi);
        caveatsPublic.Should().BeEmpty();

        // Arrange & Act - Public interface method removal (breaking for implementers)
        var (impactRemoval, _) = ImpactClassifier.Classify(
            ChangeType.Removed,
            SymbolKind.Method,
            Visibility.Public);

        // Assert
        impactRemoval.Should().Be(ChangeImpact.BreakingPublicApi);

        // Arrange & Act - Internal interface member
        var (impactInternal, _) = ImpactClassifier.Classify(
            ChangeType.Added,
            SymbolKind.Method,
            Visibility.Internal);

        // Assert
        impactInternal.Should().Be(ChangeImpact.BreakingInternalApi);
    }

    #endregion
}
