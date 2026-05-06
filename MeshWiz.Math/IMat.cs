using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IMat<TSelf, TRow, TCol, TNum>
    : IEquatable<TSelf>,
        IUnmanagedDataVector<TNum>,
        IAdditionOperators<TSelf, TSelf, TSelf>,
        ISubtractionOperators<TSelf, TSelf, TSelf>,
        ISpanParsable<TSelf>,
        ISpanFormattable
    where TSelf : IMat<TSelf, TRow, TCol, TNum>
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
    public TNum this[int row, int col] { get; }

    public TNum[,] ToArrayFast();
    static abstract TSelf operator *(TNum l, TSelf r);
    static abstract TSelf operator *(TSelf l,TNum r);
    public static abstract bool operator ==(TSelf left, TSelf right);
    static abstract bool operator !=(TSelf left, TSelf right);


    private protected sealed class Converter: JsonConverter<TSelf>
    {
        /// <inheritdoc />
        public override TSelf? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var asArr= JsonSerializer.Deserialize<TRow[]>(ref reader, options);
            if (asArr is null || asArr.Length != TSelf.RowCount) throw new JsonException();
            return Unsafe.ReadUnaligned<TSelf>(in Unsafe.As<TRow,byte>(ref MemoryMarshal.GetArrayDataReference(asArr)));
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TSelf value, JsonSerializerOptions options)
        {
            var arr=new TRow[TSelf.RowCount];
            for (var i = 0; i < arr.Length; i++) arr[i] = value.GetRow(i);
            JsonSerializer.Serialize(writer, arr, options);
        }
    }
}