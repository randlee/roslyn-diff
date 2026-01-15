namespace RoslynDiff.Core.Tests.EdgeCases;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Tests for handling generic types, constraints, and variance.
/// </summary>
public class GenericTests
{
    private readonly CSharpDiffer _differ = new();

    #region Generic Class Tests

    [Fact]
    public void Compare_GenericClass_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Container<T>
            {
                public T Value { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public class Container<T>
            {
                public T Value { get; set; }
                public T DefaultValue { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericClassWithMultipleTypeParameters_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Pair<TKey, TValue>
            {
                public TKey Key { get; set; }
                public TValue Value { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public class Pair<TKey, TValue, TMetadata>
            {
                public TKey Key { get; set; }
                public TValue Value { get; set; }
                public TMetadata Metadata { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericRecordClass_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record Container<T>(T Value);
            """;
        var newCode = """
            namespace Test;
            public record Container<T>(T Value, string Label);
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Generic Constraints Tests

    [Fact]
    public void Compare_ClassConstraint_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Container<T> where T : class
            {
                public T Value { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public class Container<T> where T : struct
            {
                public T Value { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NewConstraint_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Factory<T> where T : class
            {
                public T Create() => default!;
            }
            """;
        var newCode = """
            namespace Test;
            public class Factory<T> where T : class, new()
            {
                public T Create() => new T();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_BaseTypeConstraint_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public interface IEntity { int Id { get; } }
            public class Repository<T> where T : IEntity
            {
                public T GetById(int id) => default!;
            }
            """;
        var newCode = """
            namespace Test;
            public interface IEntity { int Id { get; } }
            public interface IAuditable { DateTime Created { get; } }
            public class Repository<T> where T : IEntity, IAuditable
            {
                public T GetById(int id) => default!;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NotNullConstraint_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Container<T>
            {
                public T Value { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public class Container<T> where T : notnull
            {
                public T Value { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_UnmanagedConstraint_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Buffer<T> where T : struct
            {
                public T[] Data { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public class Buffer<T> where T : unmanaged
            {
                public T[] Data { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MultipleConstraints_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public interface ICloneable<T> { T Clone(); }
            public class Repository<T> where T : class, ICloneable<T>, new()
            {
                public T Create() => new T();
            }
            """;
        var newCode = """
            namespace Test;
            public interface ICloneable<T> { T Clone(); }
            public interface IValidatable { bool IsValid(); }
            public class Repository<T> where T : class, ICloneable<T>, IValidatable, new()
            {
                public T Create() => new T();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ConstraintsOnMultipleTypeParameters_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Map<TKey, TValue>
                where TKey : notnull
            {
                public TValue Get(TKey key) => default!;
            }
            """;
        var newCode = """
            namespace Test;
            public class Map<TKey, TValue>
                where TKey : notnull
                where TValue : class
            {
                public TValue Get(TKey key) => default!;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Generic Method Tests

    [Fact]
    public void Compare_GenericMethod_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Utils
            {
                public T Default<T>() => default!;
            }
            """;
        var newCode = """
            namespace Test;
            public class Utils
            {
                public T Default<T>() where T : new() => new T();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericMethodWithMultipleTypeParameters_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Utils
            {
                public TResult Convert<TSource, TResult>(TSource source) => default!;
            }
            """;
        var newCode = """
            namespace Test;
            public class Utils
            {
                public TResult Convert<TSource, TResult>(TSource source)
                    where TResult : new() => new TResult();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericExtensionMethod_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public static class Extensions
            {
                public static T OrDefault<T>(this T? value, T defaultValue) where T : struct
                    => value ?? defaultValue;
            }
            """;
        var newCode = """
            namespace Test;
            public static class Extensions
            {
                public static T OrDefault<T>(this T? value, T defaultValue, bool logIfDefault = false) where T : struct
                    => value ?? defaultValue;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Nested Generics Tests

    [Fact]
    public void Compare_NestedGenericTypes_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System.Collections.Generic;
            public class Foo
            {
                public Dictionary<string, List<int>> Data { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            using System.Collections.Generic;
            public class Foo
            {
                public Dictionary<string, List<string>> Data { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_DeeplyNestedGenerics_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System.Collections.Generic;
            public class Foo
            {
                public Dictionary<string, Dictionary<int, List<HashSet<double>>>> DeepData { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            using System.Collections.Generic;
            public class Foo
            {
                public Dictionary<string, Dictionary<int, List<HashSet<float>>>> DeepData { get; set; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericClassWithGenericMember_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System.Collections.Generic;
            public class Container<T>
            {
                public List<T> Items { get; set; } = new();
            }
            """;
        var newCode = """
            namespace Test;
            using System.Collections.Generic;
            public class Container<T>
            {
                public List<T> Items { get; set; } = [];
                public Dictionary<string, T> IndexedItems { get; set; } = [];
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Variance Tests

    [Fact]
    public void Compare_CovariantInterface_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public interface IProducer<T>
            {
                T Produce();
            }
            """;
        var newCode = """
            namespace Test;
            public interface IProducer<out T>
            {
                T Produce();
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ContravariantInterface_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public interface IConsumer<T>
            {
                void Consume(T item);
            }
            """;
        var newCode = """
            namespace Test;
            public interface IConsumer<in T>
            {
                void Consume(T item);
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MixedVariance_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public interface ITransformer<TIn, TOut>
            {
                TOut Transform(TIn input);
            }
            """;
        var newCode = """
            namespace Test;
            public interface ITransformer<in TIn, out TOut>
            {
                TOut Transform(TIn input);
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_CovariantDelegate_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public delegate T Producer<T>();
            """;
        var newCode = """
            namespace Test;
            public delegate T Producer<out T>();
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ContravariantDelegate_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public delegate void Consumer<T>(T item);
            """;
        var newCode = """
            namespace Test;
            public delegate void Consumer<in T>(T item);
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Generic Interface Implementation Tests

    [Fact]
    public void Compare_GenericInterfaceImplementation_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            using System;
            public interface IComparable<T>
            {
                int CompareTo(T other);
            }
            public class Person : IComparable<Person>
            {
                public string Name { get; set; }
                public int CompareTo(Person other) => 0;
            }
            """;
        var newCode = """
            namespace Test;
            using System;
            public interface IComparable<T>
            {
                int CompareTo(T other);
            }
            public interface IEquatable<T>
            {
                bool Equals(T other);
            }
            public class Person : IComparable<Person>, IEquatable<Person>
            {
                public string Name { get; set; }
                public int CompareTo(Person other) => 0;
                public bool Equals(Person other) => Name == other?.Name;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_MultipleGenericInterfaceImplementations_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public interface IConverter<TIn, TOut>
            {
                TOut Convert(TIn input);
            }
            public class StringConverter : IConverter<int, string>
            {
                public string Convert(int input) => input.ToString();
            }
            """;
        var newCode = """
            namespace Test;
            public interface IConverter<TIn, TOut>
            {
                TOut Convert(TIn input);
            }
            public class StringConverter : IConverter<int, string>, IConverter<double, string>
            {
                public string Convert(int input) => input.ToString();
                public string Convert(double input) => input.ToString("F2");
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Generic Inheritance Tests

    [Fact]
    public void Compare_GenericBaseClass_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Base<T>
            {
                public T Value { get; set; }
            }
            public class Derived : Base<int>
            {
            }
            """;
        var newCode = """
            namespace Test;
            public class Base<T>
            {
                public T Value { get; set; }
            }
            public class Derived : Base<string>
            {
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericDerivedFromGeneric_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Base<T>
            {
                public T Value { get; set; }
            }
            public class Derived<T> : Base<T>
            {
            }
            """;
        var newCode = """
            namespace Test;
            public class Base<T>
            {
                public T Value { get; set; }
            }
            public class Derived<T> : Base<T> where T : class
            {
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Generic Struct Tests

    [Fact]
    public void Compare_GenericStruct_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public struct ValueWrapper<T>
            {
                public T Value { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public readonly struct ValueWrapper<T>
            {
                public T Value { get; init; }
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericRecordStruct_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record struct Point<T>(T X, T Y);
            """;
        var newCode = """
            namespace Test;
            public record struct Point<T>(T X, T Y, T Z);
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Complex Generic Scenarios Tests

    [Fact]
    public void Compare_SelfReferencingGeneric_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public interface INode<T> where T : INode<T>
            {
                T Parent { get; }
            }
            """;
        var newCode = """
            namespace Test;
            public interface INode<T> where T : INode<T>
            {
                T Parent { get; }
                IEnumerable<T> Children { get; }
            }
            using System.Collections.Generic;
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericWithDefaultValue_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Container<T>
            {
                public T GetOrDefault(T defaultValue = default) => defaultValue;
            }
            """;
        var newCode = """
            namespace Test;
            public class Container<T>
            {
                public T GetOrDefault(T defaultValue = default!) => defaultValue;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_GenericWithStaticMembers_DetectsChanges()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Singleton<T> where T : new()
            {
                private static T? _instance;
                public static T Instance => _instance ??= new T();
            }
            """;
        var newCode = """
            namespace Test;
            public class Singleton<T> where T : class, new()
            {
                private static T? _instance;
                public static T Instance => _instance ??= new T();
                public static void Reset() => _instance = null;
            }
            """;

        var options = new DiffOptions();

        // Act
        var result = _differ.Compare(oldCode, newCode, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion
}
