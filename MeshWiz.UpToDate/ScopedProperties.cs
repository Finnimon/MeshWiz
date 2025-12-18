using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.UpToDate;

[Pure]
public static class ScopedProperties
{
    public static EquatableScopedProperty<T> Property<T>(this IUpToDate scope, T initialValue)
        where T : IEquatable<T>
        => new(scope, initialValue);

    public static EquatableScopedProperty<T> Property<T>(this IUpToDate scope)
        where T : IEquatable<T>, new()
        => new(scope, new T());

    public static EqualityComparerScopedProperty<T> PropertyWithComparer<T>(this IUpToDate scope, T initialValue,
        IEqualityComparer<T>? comparer = null)
        => new(scope, initialValue, comparer ?? EqualityComparer<T>.Default);

    public static EqualityComparerScopedProperty<T> PropertyWithComparer<T>(this IUpToDate scope, T initialValue,
        Func<T?, T?, bool> comparer)
        => new(scope, initialValue, EqualityComparer<T>.Create(comparer));

    public static FloatingPointScopedProperty<T> FloatingPointProperty<T>(this IUpToDate scope, T initialValue, T delta)
        where T : IFloatingPoint<T>
        => new(scope, initialValue, delta);
    
    public static FloatingPointScopedProperty<T> FloatingPointProperty<T>(this IUpToDate scope, T initialValue)
        where T : IFloatingPoint<T>
        => new(scope, initialValue, Numbers<T>.ZeroEpsilon);
    
}