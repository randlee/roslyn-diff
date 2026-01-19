namespace RoslynDiff.Core.Tests.Comparison;

using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for VisibilityExtractor.
/// </summary>
public class VisibilityExtractorTests
{
    #region Explicit Visibility Modifier Tests

    [Fact]
    public void Extract_PublicClass_ReturnsPublic()
    {
        // Arrange
        var code = "public class MyClass { }";
        var classNode = ParseAndGetFirstDescendant<ClassDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(classNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void Extract_PrivateMethod_ReturnsPrivate()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                private void MyMethod() { }
            }";
        var methodNode = ParseAndGetFirstDescendant<MethodDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(methodNode);

        // Assert
        visibility.Should().Be(Visibility.Private);
    }

    [Fact]
    public void Extract_InternalClass_ReturnsInternal()
    {
        // Arrange
        var code = "internal class MyClass { }";
        var classNode = ParseAndGetFirstDescendant<ClassDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(classNode);

        // Assert
        visibility.Should().Be(Visibility.Internal);
    }

    [Fact]
    public void Extract_ProtectedMethod_ReturnsProtected()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                protected void MyMethod() { }
            }";
        var methodNode = ParseAndGetFirstDescendant<MethodDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(methodNode);

        // Assert
        visibility.Should().Be(Visibility.Protected);
    }

    [Fact]
    public void Extract_ProtectedInternalMethod_ReturnsProtectedInternal()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                protected internal void MyMethod() { }
            }";
        var methodNode = ParseAndGetFirstDescendant<MethodDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(methodNode);

        // Assert
        visibility.Should().Be(Visibility.ProtectedInternal);
    }

    [Fact]
    public void Extract_PrivateProtectedMethod_ReturnsPrivateProtected()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                private protected void MyMethod() { }
            }";
        var methodNode = ParseAndGetFirstDescendant<MethodDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(methodNode);

        // Assert
        visibility.Should().Be(Visibility.PrivateProtected);
    }

    #endregion

    #region Default Visibility Tests

    [Fact]
    public void Extract_TopLevelClassWithNoModifier_ReturnsInternal()
    {
        // Arrange
        var code = "class MyClass { }";
        var classNode = ParseAndGetFirstDescendant<ClassDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(classNode);

        // Assert
        visibility.Should().Be(Visibility.Internal);
    }

    [Fact]
    public void Extract_NestedClassWithNoModifier_ReturnsPrivate()
    {
        // Arrange
        var code = @"
            public class OuterClass
            {
                class NestedClass { }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var nestedClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "NestedClass");

        // Act
        var visibility = VisibilityExtractor.Extract(nestedClass);

        // Assert
        visibility.Should().Be(Visibility.Private);
    }

    [Fact]
    public void Extract_InterfaceMemberWithNoModifier_ReturnsPublic()
    {
        // Arrange
        var code = @"
            public interface IMyInterface
            {
                void MyMethod();
            }";
        var methodNode = ParseAndGetFirstDescendant<MethodDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(methodNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void Extract_MethodWithNoModifierInClass_ReturnsPrivate()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                void MyMethod() { }
            }";
        var methodNode = ParseAndGetFirstDescendant<MethodDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(methodNode);

        // Assert
        visibility.Should().Be(Visibility.Private);
    }

    #endregion

    #region Local Scope Tests

    [Fact]
    public void Extract_Parameter_ReturnsLocal()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                public void MyMethod(int parameter) { }
            }";
        var parameterNode = ParseAndGetFirstDescendant<ParameterSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(parameterNode);

        // Assert
        visibility.Should().Be(Visibility.Local);
    }

    [Fact]
    public void Extract_LocalVariable_ReturnsLocal()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                public void MyMethod()
                {
                    int localVar = 42;
                }
            }";
        var localDeclaration = ParseAndGetFirstDescendant<LocalDeclarationStatementSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(localDeclaration);

        // Assert
        visibility.Should().Be(Visibility.Local);
    }

    [Fact]
    public void Extract_VariableDeclarator_ReturnsLocal()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                public void MyMethod()
                {
                    int localVar = 42;
                }
            }";
        var variableDeclarator = ParseAndGetFirstDescendant<VariableDeclaratorSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(variableDeclarator);

        // Assert
        visibility.Should().Be(Visibility.Local);
    }

    #endregion

    #region IsPublicApi and IsInternalApi Tests

    [Theory]
    [InlineData(Visibility.Public, true)]
    [InlineData(Visibility.Protected, true)]
    [InlineData(Visibility.ProtectedInternal, true)]
    [InlineData(Visibility.Internal, false)]
    [InlineData(Visibility.PrivateProtected, false)]
    [InlineData(Visibility.Private, false)]
    [InlineData(Visibility.Local, false)]
    public void IsPublicApi_ReturnsCorrectResult(Visibility visibility, bool expectedResult)
    {
        // Act
        var result = VisibilityExtractor.IsPublicApi(visibility);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(Visibility.Internal, true)]
    [InlineData(Visibility.PrivateProtected, true)]
    [InlineData(Visibility.Public, false)]
    [InlineData(Visibility.Protected, false)]
    [InlineData(Visibility.ProtectedInternal, false)]
    [InlineData(Visibility.Private, false)]
    [InlineData(Visibility.Local, false)]
    public void IsInternalApi_ReturnsCorrectResult(Visibility visibility, bool expectedResult)
    {
        // Act
        var result = VisibilityExtractor.IsInternalApi(visibility);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Property and Field Tests

    [Fact]
    public void Extract_PublicProperty_ReturnsPublic()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                public int MyProperty { get; set; }
            }";
        var propertyNode = ParseAndGetFirstDescendant<PropertyDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(propertyNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void Extract_PrivateField_ReturnsPrivate()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                private int _myField;
            }";
        var fieldNode = ParseAndGetFirstDescendant<FieldDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(fieldNode);

        // Assert
        visibility.Should().Be(Visibility.Private);
    }

    [Fact]
    public void Extract_FieldWithNoModifier_ReturnsPrivate()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                int _myField;
            }";
        var fieldNode = ParseAndGetFirstDescendant<FieldDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(fieldNode);

        // Assert
        visibility.Should().Be(Visibility.Private);
    }

    #endregion

    #region Struct and Record Tests

    [Fact]
    public void Extract_PublicStruct_ReturnsPublic()
    {
        // Arrange
        var code = "public struct MyStruct { }";
        var structNode = ParseAndGetFirstDescendant<StructDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(structNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void Extract_TopLevelStructWithNoModifier_ReturnsInternal()
    {
        // Arrange
        var code = "struct MyStruct { }";
        var structNode = ParseAndGetFirstDescendant<StructDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(structNode);

        // Assert
        visibility.Should().Be(Visibility.Internal);
    }

    [Fact]
    public void Extract_PublicRecord_ReturnsPublic()
    {
        // Arrange
        var code = "public record MyRecord;";
        var recordNode = ParseAndGetFirstDescendant<RecordDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(recordNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void Extract_PublicEnum_ReturnsPublic()
    {
        // Arrange
        var code = "public enum MyEnum { Value1, Value2 }";
        var enumNode = ParseAndGetFirstDescendant<EnumDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(enumNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void Extract_EnumMember_ReturnsPublic()
    {
        // Arrange
        var code = "public enum MyEnum { Value1, Value2 }";
        var enumMember = ParseAndGetFirstDescendant<EnumMemberDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(enumMember);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Extract_ExpressionBodiedMethod_ReturnsCorrectVisibility()
    {
        // Arrange
        var code = @"
            public class MyClass
            {
                public int GetValue() => 42;
            }";
        var methodNode = ParseAndGetFirstDescendant<MethodDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(methodNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void Extract_StaticClass_ReturnsCorrectVisibility()
    {
        // Arrange
        var code = "public static class MyStaticClass { }";
        var classNode = ParseAndGetFirstDescendant<ClassDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(classNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void Extract_NullNode_ReturnsDefaultVisibility()
    {
        // Arrange - Create a simple statement that doesn't have visibility modifiers
        var code = @"
            public class MyClass
            {
                public void Method()
                {
                    int x = 5;
                }
            }";
        // Get an expression statement (which doesn't have visibility modifiers)
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var expressionStatement = root.DescendantNodes()
            .OfType<ExpressionStatementSyntax>()
            .FirstOrDefault();

        // Act - Pass a node type that falls into the default case
        // If no expression statement found, test with a literal expression
        if (expressionStatement is not null)
        {
            var visibility = VisibilityExtractor.Extract(expressionStatement);
            // Assert - Default for unknown nodes should be Private
            visibility.Should().Be(Visibility.Private);
        }
        else
        {
            // Alternative test: pass a simple literal which hits default case
            var literalCode = "public class C { public int X = 1; }";
            var literalTree = CSharpSyntaxTree.ParseText(literalCode);
            var literalRoot = literalTree.GetCompilationUnitRoot();
            var equalsClause = literalRoot.DescendantNodes()
                .OfType<EqualsValueClauseSyntax>()
                .First();

            var visibility = VisibilityExtractor.Extract(equalsClause);
            visibility.Should().Be(Visibility.Private);
        }
    }

    [Fact]
    public void Extract_InitOnlyProperty_ReturnsCorrectVisibility()
    {
        // Arrange - init-only properties are a C# 9+ feature
        var code = @"
            public class MyClass
            {
                public int Value { get; init; }
            }";
        var propertyNode = ParseAndGetFirstDescendant<PropertyDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(propertyNode);

        // Assert
        visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void Extract_FileScopedType_ReturnsInternalVisibility()
    {
        // Arrange - file-scoped types use the 'file' modifier (C# 11+)
        // Note: Roslyn treats 'file' as internal for visibility purposes
        var code = "file class MyFileClass { }";
        var classNode = ParseAndGetFirstDescendant<ClassDeclarationSyntax>(code);

        // Act
        var visibility = VisibilityExtractor.Extract(classNode);

        // Assert - File-scoped types default to internal when no other modifier is present
        // The 'file' keyword doesn't map to a standard visibility, so it falls back to default
        visibility.Should().Be(Visibility.Internal);
    }

    [Fact]
    public void Extract_PropertyWithDifferentAccessorVisibility_ReturnsHigherVisibility()
    {
        // Arrange - property is public but has a private setter
        var code = @"
            public class MyClass
            {
                public int Value { get; private set; }
            }";
        var propertyNode = ParseAndGetFirstDescendant<PropertyDeclarationSyntax>(code);

        // Act - Extract visibility returns the property's visibility, not the accessor's
        var visibility = VisibilityExtractor.Extract(propertyNode);

        // Assert - The property itself is public
        visibility.Should().Be(Visibility.Public);
    }

    #endregion

    #region Helper Methods

    private static T ParseAndGetFirstDescendant<T>(string code) where T : Microsoft.CodeAnalysis.SyntaxNode
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        return root.DescendantNodes().OfType<T>().First();
    }

    #endregion
}
