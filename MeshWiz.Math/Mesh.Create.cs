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

    }
}