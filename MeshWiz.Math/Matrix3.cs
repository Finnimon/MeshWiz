using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Math
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct Matrix3<TNum> : IMatrix<TNum>, IEquatable<Matrix3<TNum>> 
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public static int RowCount => 3;
        public static int ColumnCount => 3;

        public static Matrix3<TNum> Identity { [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
            = new(Vector3<TNum>.UnitX, Vector3<TNum>.UnitY, Vector3<TNum>.UnitZ);
        public static Matrix3<TNum> Zero { [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new(TNum.Zero);
        public static Matrix3<TNum> One { [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new(TNum.One);

        public TNum Det => Determinant();

        
// @formatter:off
    public TNum M00 => X.X; public TNum M01 => X.Y; public TNum M02 => X.Z;
    public TNum M10 => Y.X; public TNum M11 => Y.Y; public TNum M12 => Y.Z;
    public TNum M20 => Z.X; public TNum M21 => Z.Y; public TNum M22 => Z.Z;
// @formatter:on
        
        public readonly Vector3<TNum> X, Y, Z;


        public Matrix3(
            TNum m00, TNum m01, TNum m02,
            TNum m10, TNum m11, TNum m12,
            TNum m20, TNum m21, TNum m22)
        {
            X = new Vector3<TNum>(m00, m01, m02);
            Y=new Vector3<TNum>(m10,m11,m12);
            Z=new Vector3<TNum>(m20,m21,m22);
        }

        public Matrix3(Vector3<TNum> x, Vector3<TNum> y, Vector3<TNum> z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Matrix3(TNum value)
        {
            X = new Vector3<TNum>(value);
            Y = X;
            Z = Y;
        }

        public static unsafe Matrix3<TNum> FromComponents(TNum[] components)
        {
            if (components.Length == 9)
                fixed (TNum* ptr = &components[0])
                    return *(Matrix3<TNum>*)ptr;
            throw new ArgumentException("Components must be of length 9");
        }

        public static unsafe Matrix3<TNum> FromComponents(TNum[,] components)
        {
            if (components.Length == 9 && components.Rank == 2)
                fixed (TNum* ptr = &components[0, 0])
                    return *(Matrix3<TNum>*)ptr;
            throw new ArgumentException("Components must be of length 9");
        }

        public static Matrix3<TNum> FromComponents(IReadOnlyList<TNum> components)
            => new(
                components[0], components[1], components[2],
                components[3], components[4], components[5],
                components[6], components[7], components[8]);

        public static Matrix3<TNum> FromRows(Vector3<TNum> x, Vector3<TNum> y, Vector3<TNum> z)
            => new(x, y, z);
        public static Matrix3<TNum> FromColumns(Vector3<TNum> c0, Vector3<TNum> c1, Vector3<TNum> c2)
            => new(c0.X, c1.X, c2.X,
                c0.Y, c1.Y, c2.Y,
                c0.Z, c1.Z, c2.Z);
        
        public unsafe TNum this[int row, int column]
        {
            get
            {
                if (RowCount>(uint)row&&ColumnCount>(uint)column)
                    fixed (TNum* ptr = &X.X)
                        return ptr[ColumnCount * row + column];
                throw new IndexOutOfRangeException();
            }
        }

        public TNum Determinant()
            => M00 * M11 * M22
             + M01 * M12 * M20
             + M02 * M10 * M21
             - M02 * M11 * M20
             - M01 * M10 * M22
             - M00 * M12 * M21;

        public unsafe TNum[,] ToArrayFast()
        {
            var result = new TNum[RowCount, ColumnCount];
            fixed (void* sourcePtr = &this)
            fixed (TNum* destPtr = &result[0, 0])
            {
                var bytes = (long)RowCount * ColumnCount * sizeof(TNum);
                Buffer.MemoryCopy(sourcePtr, destPtr, bytes, bytes);
            }
            return result;
        }

        public Vector3<TNum> Solve(Vector3<TNum> b)
        {
            var det = Det;
            if (det == TNum.Zero)
                throw new InvalidOperationException("Matrix is singular and cannot solve.");

            // Cramer's rule
            TNum d0 = b.X * (M11 * M22 - M12 * M21)
                      - M01 * (b.Y * M22 - M12 * b.Z)
                      + M02 * (b.Y * M21 - M11 * b.Z);

            TNum d1 =
                M00 * (b.Y * M22 - M12 * b.Z)
              - b.X * (M10 * M22 - M12 * M20)
              + M02 * (M10 * b.Z - b.Y * M20);

            TNum d2 =
                M00 * (M11 * b.Z - b.Y * M21)
              - M01 * (M10 * b.Z - b.Y * M20)
              + b.X * (M10 * M21 - M11 * M20);

            return new Vector3<TNum>(d0 / det, d1 / det, d2 / det);
        }

        public Matrix3<TNum> Transpose()
        =>FromColumns(X,Y,Z);


        public static Matrix3<TNum> Transpose(Matrix3<TNum> m) => m.Transpose();

        public Matrix3<TNum> Inverse()
        {
            var det = Determinant();
            if (TNum.Abs(det) < TNum.Epsilon)
                throw new InvalidOperationException("Matrix is singular and cannot invert.");

            // Cofactors
            var c00 =  M11 * M22 - M12 * M21;
            var c01 = - (M10 * M22 - M12 * M20);
            var c02 =  M10 * M21 - M11 * M20;

            var c10 = - (M01 * M22 - M02 * M21);
            var c11 =  M00 * M22 - M02 * M20;
            var c12 = - (M00 * M21 - M01 * M20);

            var c20 =  M01 * M12 - M02 * M11;
            var c21 = - (M00 * M12 - M02 * M10);
            var c22 =  M00 * M11 - M01 * M10;

            // Adjugate is transpose of cofactor matrix
            var adj = new Matrix3<TNum>(
                c00, c10, c20,
                c01, c11, c21,
                c02, c12, c22);

            return adj / det;
        }

        public static bool TryInvert(Matrix3<TNum> m, out Matrix3<TNum> result)
        {
            var det = m.Determinant();
            if (det == TNum.Zero)
            {
                result = default;
                return false;
            }
            result = m.Inverse();
            return true;
        }

        public static Matrix3<TNum> operator *(Matrix3<TNum> a, Matrix3<TNum> b)
            => new(
                a.M00 * b.M00 + a.M01 * b.M10 + a.M02 * b.M20,
                a.M00 * b.M01 + a.M01 * b.M11 + a.M02 * b.M21,
                a.M00 * b.M02 + a.M01 * b.M12 + a.M02 * b.M22,

                a.M10 * b.M00 + a.M11 * b.M10 + a.M12 * b.M20,
                a.M10 * b.M01 + a.M11 * b.M11 + a.M12 * b.M21,
                a.M10 * b.M02 + a.M11 * b.M12 + a.M12 * b.M22,

                a.M20 * b.M00 + a.M21 * b.M10 + a.M22 * b.M20,
                a.M20 * b.M01 + a.M21 * b.M11 + a.M22 * b.M21,
                a.M20 * b.M02 + a.M21 * b.M12 + a.M22 * b.M22);

        public Vector3<TNum> Multiply(Vector3<TNum> v)
            => new(X * v, Y * v, Z * v);

        public static Vector3<TNum> operator *(Matrix3<TNum> m, Vector3<TNum> v) => m.Multiply(v);

        public static Vector3<TNum> operator *(Vector3<TNum> v, Matrix3<TNum> m) => m.Transpose().Multiply(v);

        public static Matrix3<TNum> operator *(Matrix3<TNum> mat, TNum scalar)
            => new(mat.X * scalar, mat.Y * scalar, mat.Z * scalar);

        public static Matrix3<TNum> operator /(Matrix3<TNum> mat, TNum divisor)
            => mat * (TNum.One / divisor);

        public static Matrix3<TNum> operator *(TNum scalar, Matrix3<TNum> mat)
            => mat * scalar;

        public static Matrix3<TNum> operator +(Matrix3<TNum> left, Matrix3<TNum> right)
            => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        public static Matrix3<TNum> operator -(Matrix3<TNum> left, Matrix3<TNum> right)
            => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        public static Matrix3<TNum> operator -(Matrix3<TNum> mat)
            => new(-mat.X, -mat.Y, -mat.Z);

        public override bool Equals(object? obj)
            => obj is Matrix3<TNum> m && this == m;

        public static bool operator ==(Matrix3<TNum> a, Matrix3<TNum> b)
            => a.X==b.X&&a.Y==b.Y&&a.Z==b.Z;
        public static bool operator !=(Matrix3<TNum> a, Matrix3<TNum> b) => !(a == b);

        public override int GetHashCode()
            => HashCode.Combine(X,Y,Z);

        public override string ToString()
            => $"[{X}; {Y}; {Z}]";

        public bool Equals(Matrix3<TNum> other)
            => this == other;
    }
}
