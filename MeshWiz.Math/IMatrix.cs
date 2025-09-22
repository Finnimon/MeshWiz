using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IMatrix<TNum> : IUnmanagedDataVector<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static abstract int RowCount { get; }
    public static abstract int ColumnCount { get; }
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Det { get; }
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum this[int  row, int column] { get; }

    public TNum[,] ToArrayFast();
}