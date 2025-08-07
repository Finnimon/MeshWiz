using System.Numerics;

namespace MeshWiz.Math;

public static class Matrices
{
    public static TNum Determinant<TNum, TMatrix>(ref this TMatrix matrix)
        where TNum : IFloatingPointIeee754<TNum>
        where TMatrix : struct, IMatrix<TNum>
    
    {
        int n = TMatrix.RowCount;
        if (n != TMatrix.ColumnCount)
            throw new InvalidOperationException("Matrix must be square.");

        // 1) Copy into a temp array so we can mutate it
        var a = matrix.ToArrayFast();

        TNum det = TNum.One;
        int sign = 1;

        for (int i = 0; i < n; i++)
        {
            // 2) Partial pivot: find the row with largest |a[row,i]|
            int pivotRow = i;
            TNum maxVal = TNum.Abs(a[i, i]);
            for (int r = i + 1; r < n; r++)
            {
                var cur = TNum.Abs(a[r, i]);
                if (cur > maxVal)
                {
                    maxVal = cur;
                    pivotRow = r;
                }
            }

            // If pivot is zero, det is zero
            if (maxVal == TNum.Zero)
                return TNum.Zero;

            // 3) If we swapped, flip the sign
            if (pivotRow != i)
            {
                sign = -sign;
                for (int c = 0; c < n; c++)
                {
                    (a[i, c], a[pivotRow, c]) = (a[pivotRow, c], a[i, c]);
                }
            }

            // 4) Eliminate below
            var pivot = a[i, i];
            det *= pivot;
            for (int r = i + 1; r < n; r++)
            {
                // factor = a[r,i] / pivot
                var factor = a[r, i] / pivot;
                for (int c = i; c < n; c++)
                    a[r, c] -= factor * a[i, c];
            }
        }

        // 5) Apply the sign from our row-swaps
        if (sign < 0)
            det = -det;

        return det;
    }
    
    
}