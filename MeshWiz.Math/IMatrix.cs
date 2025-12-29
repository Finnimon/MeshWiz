using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IMatrix<TSelf, out TRow, out TCol, TNum>
    : IEquatable<TSelf>,
        IUnmanagedDataVector<TNum>,
        IAdditionOperators<TSelf, TSelf, TSelf>,
        ISubtractionOperators<TSelf, TSelf, TSelf>,
        ISpanParsable<TSelf>,
        ISpanFormattable
    where TSelf : IMatrix<TSelf, TRow, TCol, TNum>
    where TRow : unmanaged, IVec<TRow, TNum>
    where TCol : unmanaged, IVec<TCol, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public TCol GetCol(int column);
    public TRow GetRow(int row);
    public static abstract int RowCount { get; }
    public static abstract int ColCount { get; }

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Det { get; }

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum this[int row, int column] { get; }

    public TNum[,] ToArrayFast();

    public static abstract bool operator ==(TSelf left, TSelf right);
    static abstract bool operator !=(TSelf left, TSelf right);
}