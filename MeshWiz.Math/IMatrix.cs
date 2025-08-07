using System.Numerics;

namespace MeshWiz.Math;

public interface IMatrix<out TNum>
    where TNum : IFloatingPointIeee754<TNum>
{
    public static abstract int RowCount { get; }
    public static abstract int ColumnCount { get; }
    public TNum Det { get; }
    public TNum this[int  row, int column] { get; }

    public TNum[,] ToArrayFast();
}