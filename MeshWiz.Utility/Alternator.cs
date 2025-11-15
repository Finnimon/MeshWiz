using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Utility;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct Alternator : IEquatable<Alternator>,
    IComparable,
    IComparable<Alternator>,
    IEqualityComparer<Alternator>,
    IEqualityOperators<Alternator, Alternator, bool>
{
    private bool _negatedValue;

    public Alternator() => _negatedValue = true;
    public Alternator(bool initialValue) => _negatedValue = !initialValue;    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(in Alternator alternator) 
        => Unsafe.AsRef(in alternator)._negatedValue = !alternator._negatedValue;

    /// <inheritdoc />
    public bool Equals(Alternator other)
        => _negatedValue == other._negatedValue;

    /// <inheritdoc />
    public int CompareTo(Alternator other)
        =>_negatedValue.CompareTo(other._negatedValue);

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
        =>obj is Alternator other && _negatedValue==other._negatedValue;

    /// <inheritdoc />
    public override int GetHashCode() => _negatedValue.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(object? obj)
        =>_negatedValue.CompareTo(obj);

    /// <inheritdoc />
    public bool Equals(Alternator x, Alternator y)
        => x._negatedValue == y._negatedValue;

    /// <inheritdoc />
    public int GetHashCode(Alternator obj)
        =>_negatedValue.GetHashCode();
    public override string ToString()=>_negatedValue.ToString();

    /// <inheritdoc />
    public static bool operator ==(Alternator left, Alternator right)
        => left._negatedValue == right._negatedValue;

    /// <inheritdoc />
    public static bool operator !=(Alternator left, Alternator right)
        => left._negatedValue != right._negatedValue;
}