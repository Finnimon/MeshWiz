using System.Diagnostics.Contracts;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Mesh
{
    public static class Create
    {
        public static IndexedMesh<TNum> LoftInterleavedRibs<TNum>(Vector3<TNum>[] interleavedOutsideRibs, int ribCount)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var rowCount = interleavedOutsideRibs.Length / ribCount;
            var triCount = (rowCount - 1) * 2 * (ribCount - 1);
            var indices = new TriangleIndexer[triCount];
            var curTri = -1;

            for (var rowStart = 0; rowStart < interleavedOutsideRibs.Length - ribCount; rowStart += ribCount)
            {
                for (var prevOffset = 0; prevOffset < ribCount; prevOffset++)
                {
                    var a = prevOffset + rowStart;
                    var b = a + 1;
                    var c = a + ribCount;
                    indices[++curTri] = new TriangleIndexer(a, b, c);

                    var revA = b;
                    var revB = revA + ribCount;
                    var revC = revB - 1;
                    indices[++curTri] = new TriangleIndexer(revA, revB, revC);
                }
            }

            return new IndexedMesh<TNum>(interleavedOutsideRibs, indices);
        }

        public static IndexedMesh<TNum> LoftRibs<TNum>(IReadOnlyList<Vector3<TNum>[]> ribs)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var ribCount = ribs.Count;
            var rowCount = ribs[0].Length;

            var triCount = (rowCount - 1) * 2 * (ribCount - 1);
            var indices = new TriangleIndexer[triCount];
            var curTri = -1;

            for (var row = 0; row < rowCount - 1; row++)
            {
                for (var rib = 0; rib < ribCount; rib++)
                {
                    var a = rib * rowCount + row;
                    var b = a + rowCount;
                    var c = a + 1;
                    indices[++curTri] = new TriangleIndexer(a, b, c);

                    var revA = b;
                    var revB = revA + 1;
                    var revC = revB - rowCount;
                    indices[++curTri] = new(revA, revB, revC);
                }
            }


            var verts = new Vector3<TNum>[ribCount * rowCount];
            for (var i = 0; i < ribs.Count; i++) Array.Copy(ribs[i], 0, verts, i * rowCount, rowCount);
            return new(verts, indices);
        }

        public static IndexedMesh<TNum> LoftRibsInterleaving<TNum>(IReadOnlyList<Vector3<TNum>[]> ribs)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var ribCount = ribs.Count;
            var rowCount = ribs[0].Length;
            var vertCount = rowCount * ribCount;
            var verts = new Vector3<TNum>[vertCount];
            for (var i = 0; i < vertCount; i++)
            {
                var rib = i % ribCount;
                var row = i / ribCount;
                verts[i] = ribs[rib][row];
            }

            return LoftInterleavedRibs(verts, ribCount);
        }

        public static IndexedMesh<TNum> LoftRibsClosed<TNum>(IReadOnlyList<Vector3<TNum>[]> ribs)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (ribs.Count < 2) ThrowHelper.ThrowArgumentException("At least two ribs required.", nameof(ribs));

            var ribCount = ribs.Count;
            var rowCount = ribs[0].Length;

            // check uniform rowCount
            for (var i = 1; i < ribCount; i++)
                if (ribs[i].Length != rowCount)
                    ThrowHelper.ThrowArgumentException("All ribs must have same number of points.");

            var verts = ribs.SelectMany(v => v).ToArray();

            var triCount = (rowCount - 1) * 2 * ribCount;
            var indices = new TriangleIndexer[triCount];
            var t = 0;

            for (var row = 0; row < rowCount - 1; row++)
            {
                for (var rib = 0; rib < ribCount; rib++)
                {
                    var nextRib = (rib + 1) % ribCount;

                    var a = rib * rowCount + row;
                    var b = nextRib * rowCount + row;
                    var c = a + 1;
                    indices[t++] = new TriangleIndexer(a, b, c);

                    var revA = b;
                    var revB = nextRib * rowCount + row + 1;
                    var revC = a + 1;
                    indices[t++] = new TriangleIndexer(revA, revB, revC);
                }
            }

            return new IndexedMesh<TNum>(verts, indices);
        }

        public static IndexedMesh<TNum> PipeAlong<TNum>(Polyline<Vector3<TNum>, TNum> baseFace,
            Polyline<Vector3<TNum>, TNum> along)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (along.Count == 0) return IndexedMesh<TNum>.Empty;
            var sourcePoints = (baseFace.IsClosed ? baseFace.Points[1..] : baseFace.Points[..]).ToArray();

            var points = Enumerable.Range(0, sourcePoints.Length).Select(_ => new Vector3<TNum>[along.Points.Length])
                .ToArray();
            for (var rib = 0; rib < sourcePoints.Length; rib++) points[rib][0] = sourcePoints[rib];
            for (var row = 0; row < along.Count; row++)
            {
                var shift = along[row].AxisVector;
                for (var i = 0; i < sourcePoints.Length; i++)
                    points[i][row + 1] = (sourcePoints[i] += shift);
            }

            return baseFace.IsClosed ? LoftRibsClosed(points) : LoftRibs(points);
        }

        public static IndexedMesh<TNum> PipeAlongAligning<TNum>(
            Polyline<Vector2<TNum>, TNum> baseFace,
            Polyline<Vector3<TNum>, TNum> along)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (along.Count == 0)
                return IndexedMesh<TNum>.Empty;

            var basePoints2D = baseFace.IsClosed ? baseFace.Points[1..] : baseFace.Points[..];

            var points = Enumerable.Range(0, basePoints2D.Length)
                .Select(_ => new Vector3<TNum>[along.Points.Length])
                .ToArray();

            for (int row = 0; row < along.Points.Length; row++)
            {
                var origin = along.Points[row];
                Vector3<TNum> normal;
                if (row == 0)
                    normal = along[0].Direction;
                else if (row == along.Points.Length - 1)
                    normal = along[^1].Direction;
                else
                {
                    var l1 = along[row - 1];
                    var l2 = along[row];
                    var n1 = l1.Direction;
                    var n2 = l2.Direction;
                    normal = Vector3<TNum>.Lerp(n1, n2, Numbers<TNum>.Half).Normalized();
                    if (!normal.SquaredLength.IsApprox(TNum.One, Numbers<TNum>.Eps5)) normal = n2;
                }

                Plane3<TNum> projector = new(normal, origin);
                var planeOrigin = projector.Origin;
                var current = projector.ProjectIntoWorld(basePoints2D);
                var shift = origin - planeOrigin;
                if (!shift.IsApprox(Vector3<TNum>.Zero))
                    for (var i = 0; i < current.Length; i++)
                        current[i] += shift;
                for (var rib = 0; rib < basePoints2D.Length; rib++) points[rib][row] = current[rib];
            }

            return baseFace.IsClosed ? LoftRibsClosed(points) : LoftRibs(points);
        }

        public static IndexedMesh<TNum> PipeAlongAligned<TNum>(
            Polyline<Vector2<TNum>, TNum> baseFace,
            Polyline<Vector3<TNum>, TNum> along)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (along.Count == 0) return IndexedMesh<TNum>.Empty;

            var sourcePoints = baseFace.IsClosed ? baseFace.Points[1..] : baseFace.Points[..];

            // We'll build a 2D array [rib][row] of points like in your original version.
            var points = Enumerable.Range(0, sourcePoints.Length)
                .Select(_ => new Vector3<TNum>[along.Points.Length])
                .ToArray();

            for (int row = 0; row < along.Points.Length; row++)
            {
                Vector3<TNum> tangent;
                tangent = row > along.Count - 1 ? along[^1].Direction : along[row].Direction;

                var up = Vector3<TNum>.UnitX;
                if (up.IsApprox(tangent, Numbers<TNum>.Eps5))
                    up = Vector3<TNum>.UnitY;

                var normal = up.Cross(tangent).Normalized();
                var binormal = tangent.Cross(normal).Normalized();

                var origin = along.Points[row];

                for (int rib = 0; rib < sourcePoints.Length; rib++)
                {
                    var local = sourcePoints[rib];

                    var rotated =
                        origin +
                        local.X * normal +
                        local.Y * binormal;

                    points[rib][row] = rotated;
                }
            }

            return baseFace.IsClosed ? LoftRibsClosed(points) : LoftRibs(points);
        }

        [Pure]
        public static IndexedMesh<TNum> Loft<TNum>(ReadOnlySpan<Pose3<TNum>> centerline,
            TNum width)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (centerline.Length < 2) return IndexedMesh<TNum>.Empty;
            if (TNum.Zero.IsApproxGreaterOrEqual(width) || !TNum.IsFinite(width))
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(width), width, "width must be a finite number >0");
            var leftSpine = new Vector3<TNum>[centerline.Length];
            var rightSpine = new Vector3<TNum>[centerline.Length];
            for (var i = 0; i < centerline.Length; i++)
            {
                ref readonly var center = ref centerline[i];
                var pos = center.Origin;
                var right = center.Right;
                right *= width;
                rightSpine[i] = pos + right;
                leftSpine[i] = pos - right;
            }

            var result = LoftRibs([leftSpine, rightSpine]);
            return result;
        }
    }
}