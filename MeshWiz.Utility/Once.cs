using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Utility;

/// <summary>
/// Initial Impulse Provider
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct Once : IEquatable<Once>, 
    IComparable, 
    IComparable<Once>, 
    IEqualityComparer<Once>,
    IEqualityOperators<Once, Once, bool>
{
    private bool _value;

    public Once() => _value = true;
    public Once(bool value) => _value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(in Once once)
    {
        var value = once._value;
        Unsafe.AsRef(in once)._value = false;
        return value;
    }

    /// <inheritdoc />
    public bool Equals(Once other)
        => _value == other._value;

    /// <inheritdoc />
    public int CompareTo(Once other)
        => _value.CompareTo(other._value);

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Once other && _value == other._value;

    /// <inheritdoc />
    public override int GetHashCode() => _value.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(object? obj)
        => _value.CompareTo(obj);

    /// <inheritdoc />
    public bool Equals(Once x, Once y)
        => x._value == y._value;

    /// <inheritdoc />
    public int GetHashCode(Once obj)
        => _value.GetHashCode();

    public override string ToString() => _value.ToString();

    /// <inheritdoc />
    public static bool operator ==(Once left, Once right)
        => left._value == right._value;

    /// <inheritdoc />
    public static bool operator !=(Once left, Once right)
        => left._value != right._value;
}