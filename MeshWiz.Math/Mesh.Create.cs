using System.Numerics;

namespace MeshWiz.Math;

public static partial class Mesh
{
    public static class Create
    {
        public static IndexedMesh<TNum> LoftInterleavedRibs<TNum>(Vector3<TNum>[] interleavedOutsideRibs, int ribCount)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var rowCount = interleavedOutsideRibs.Length / ribCount;
            var triCount= (rowCount - 1)*2* (ribCount-1);
            var indices = new TriangleIndexer[triCount];
            var curTri = -1;
            
            for (var rowStart = 0; rowStart < interleavedOutsideRibs.Length - ribCount; rowStart += ribCount)
            {
                for (var prevOffset = 0; prevOffset < ribCount; prevOffset++)
                {
                    var a = prevOffset + rowStart;
                    var b = a + 1;
                    var c = a + ribCount;
                    indices[++curTri]=new TriangleIndexer(a, b, c);

                    var revA = b;
                    var revB = revA + ribCount;
                    var revC = revB - 1;
                    indices[++curTri]=new TriangleIndexer(revA, revB, revC);
                }
            }
            return new IndexedMesh<TNum>(interleavedOutsideRibs, indices);
        }

        public static IndexedMesh<TNum> LoftRibs<TNum>(IReadOnlyList<Vector3<TNum>[]> ribs)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var ribCount = ribs.Count;
            var rowCount = ribs[0].Length;
               
            var triCount= (rowCount - 1)*2* (ribCount-1);
            var indices = new TriangleIndexer[triCount];
            var curTri = -1;

            for (var row = 0; row < rowCount-1; row++)
            {
                for (var rib = 0; rib < ribCount; rib++)
                {
                    var a = rib * rowCount+row;
                    var b = a + rowCount;
                    var c = a + 1;
                    indices[++curTri] = new TriangleIndexer(a, b, c);

                    var revA = b;
                    var revB = revA + 1;
                    var revC=revB-rowCount;
                    indices[++curTri]=new(revA,revB,revC);
                }
            }

            
            var verts=new Vector3<TNum>[ribCount*rowCount];
            for (var i = 0; i < ribs.Count; i++) Array.Copy(ribs[i],0,verts,i*rowCount,rowCount);
            return new(verts,indices);
        }

        public static IndexedMesh<TNum> LoftRibsInterleaving<TNum>(IReadOnlyList<Vector3<TNum>[]> ribs)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var ribCount = ribs.Count;
            var rowCount = ribs[0].Length;
            var vertCount=rowCount*ribCount;
            var verts = new Vector3<TNum>[vertCount];
            for (var i = 0; i < vertCount; i++)
            {
                var rib = i % ribCount;
                var row=i / ribCount;
                verts[i] = ribs[rib][row];
            }
            return LoftInterleavedRibs(verts, ribCount);
        }

        public static IndexedMesh<TNum> PipeAlong<TNum>(IReadOnlyList<Vector3<TNum>> center, TNum radius,
            int ribCount = 12)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            throw new NotImplementedException();
        }

        public static TMesh Combine<TMesh, TNum>(params TMesh[] meshes)
            where TMesh : IMesh<TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            => throw new NotImplementedException();

        public static IndexedMesh<TNum> LoftRibsClosed<TNum>(IReadOnlyList<Vector3<TNum>[]> ribs) 
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (ribs.Count < 2) throw new ArgumentException("At least two ribs required.", nameof(ribs));

            var ribCount = ribs.Count;
            var rowCount = ribs[0].Length;

            // check uniform rowCount
            for (var i = 1; i < ribCount; i++)
                if (ribs[i].Length != rowCount) 
                    throw new ArgumentException("All ribs must have same number of points.");

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

    }
}