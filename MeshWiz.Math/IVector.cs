using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IVector<TSelf, TNum>
    : IReadOnlyList<TNum>,
        IEquatable<TSelf>,
        IComparable<TSelf>,
        IByteSize,
        IUnmanagedDataVector<TNum>
    where TNum : unmanaged, INumber<TNum>
    where TSelf : IVector<TSelf, TNum>
{
    
    static unsafe int IByteSize.ByteSize => (int)(sizeof(TNum)*TSelf.Dimensions);
    static abstract TSelf Zero { get; }
    static abstract TSelf One { get; }
    [Pure] static abstract uint Dimensions { get; }
    [Pure] int IReadOnlyCollection<TNum>.Count => (int)TSelf.Dimensions;
    [Pure] TNum Length { get; }
    [Pure] TNum SquaredLength => Dot((TSelf)this);
    [Pure] TSelf Normalized => this.Divide(Length);

    [Pure]
    TSelf Add(in TSelf other);

    [Pure]
    TSelf Subtract(in TSelf other) => this.Add(other.Scale(-TNum.One));

    [Pure]
    TSelf Scale(in TNum scalar);

    [Pure]
    TSelf Divide(in TNum divisor) => Scale(TNum.One / divisor);

    [Pure]
    TNum Dot(in TSelf other);

    [Pure]
    TNum Distance(in TSelf other) => Subtract(in other).Length;

    static virtual TSelf operator +(in TSelf left, in TSelf right) => left.Add(right);
    static virtual TSelf operator -(in TSelf left, in TSelf right) => left.Subtract(right);
    static virtual TNum operator *(in TSelf left, in TSelf right) => left.Dot(right);
    static virtual TSelf operator *(in TNum scalar, in TSelf vector) => vector.Scale(scalar);
    static virtual TSelf operator *(in TSelf vector, in TNum scalar) => vector.Scale(scalar);
    static virtual TSelf operator /(in TSelf vector, in TNum divisor) => vector.Divide(divisor);
    static virtual bool operator ==(in TSelf vector, in TSelf divisor) => vector.Equals(divisor);
    static virtual bool operator !=(in TSelf vector, in TSelf divisor) => !vector.Equals(divisor);
    static virtual TSelf Lerp(in TSelf from, in TSelf to, TNum normalDistance)
    =>(to-from)*normalDistance+from;
}