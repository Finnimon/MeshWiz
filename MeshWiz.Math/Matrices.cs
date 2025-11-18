using System.Numerics;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Math;

public static class Matrices
{
    public static TNum Determinant<TRow,TCol,TNum, TMatrix>(ref this TMatrix matrix)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TMatrix : unmanaged,IMatrix<TMatrix,TRow,TCol,TNum>
        where TRow : unmanaged, IVector<TRow, TNum>
        where TCol : unmanaged, IVector<TCol, TNum>

    {
        var n = TMatrix.RowCount;
        if (n != TMatrix.ColCount)
            ThrowHelper.ThrowInvalidOperationException("Matrix must be square.");

        var a = matrix.ToArrayFast();
        var det = TNum.One;
        var sign = 1;

        for (var i = 0; i < n; i++)
        {
            var pivotRow = i;
            var maxVal = TNum.Abs(a[i, i]);
            for (var r = i + 1; r < n; r++)
            {
                var cur = TNum.Abs(a[r, i]);
                if (cur <= maxVal) continue;
                maxVal = cur;
                pivotRow = r;
            }

            if (maxVal == TNum.Zero)
                return TNum.Zero;

            if (pivotRow != i)
            {
                sign = -sign;
                for (var c = 0; c < n; c++) (a[i, c], a[pivotRow, c]) = (a[pivotRow, c], a[i, c]);
            }

            var pivot = a[i, i];
            det *= pivot;
            for (var r = i + 1; r < n; r++)
            {
                var factor = a[r, i] / pivot;
                for (var c = i; c < n; c++)
                    a[r, c] -= factor * a[i, c];
            }
        }

        if (sign < 0)
            det = -det;

        return det;
    }
    
    
}