using System.Numerics;
using MeshWiz.RefLinq;

namespace MeshWiz.Math;

public static partial class Bvh
{
    public class Mesh<TNum> : IMesh<TNum>, IHierarchy<Triangle3<TNum>,Vec3<TNum>,TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        internal readonly Triangle3<TNum>[] _triangles;
        internal readonly Node<Vec3<TNum>, TNum>[] _nodes;
        public ReadOnlySpan<Node<Vec3<TNum>, TNum>> Nodes => _nodes;

        /// <inheritdoc />
        IReadOnlyList<Triangle3<TNum>> IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>.Elements => _triangles;
        
        public ReadOnlySpan<Triangle3<TNum>> Triangles => _triangles;

        /// <inheritdoc />
        public int Count => _triangles.Length;

        /// <inheritdoc />
        public Triangle3<TNum> this[int index] => _triangles[index];

        private TNum? _surfaceArea;

        /// <inheritdoc />
        public TNum SurfaceArea => _surfaceArea ??= Mesh.Math.SurfaceArea(_triangles);


        /// <inheritdoc />
        public AABB<Vec3<TNum>> BBox => Count == 0 ? AABB<Vec3<TNum>>.Empty : _nodes[0].Bounds;

        private TNum? _volume;

        /// <inheritdoc />
        public TNum Volume => _volume ??= Mesh.Math.Volume(_triangles);

        private Vec3<TNum>? _vertCentroid;

        /// <inheritdoc />
        public Vec3<TNum> VertexCentroid => _vertCentroid ??= Mesh.Math.VertexCentroid(_triangles);

        private Vec3<TNum>? _surfaceCentroid;

        /// <inheritdoc />
        public Vec3<TNum> SurfaceCentroid => _surfaceCentroid ??= Mesh.Math.SurfaceCentroid(_triangles);

        private Vec3<TNum>? _volCentroid;

        /// <inheritdoc />
        public Vec3<TNum> VolumeCentroid => _volCentroid ??= Mesh.Math.VolumeCentroid(_triangles);

        /// <inheritdoc />
        public void InitializeLazies()
        {
            var anyNull = _surfaceArea is null 
                          || _volume is null  
                          || _vertCentroid is null  
                          || _surfaceCentroid is null  
                          || _volCentroid is null;
            if (!anyNull) return;

            var allInfo = Mesh.Math.AllInfo(_triangles);
            _surfaceArea = allInfo.SurfaceArea;
            _volume = allInfo.Volume;
            _vertCentroid = allInfo.VertexCentroid;
            _surfaceCentroid = allInfo.SurfaceCentroid;
            _volCentroid = allInfo.VolumeCentroid;
        }
        public int Depth { get; }

        public Mesh(IEnumerable<Triangle3<TNum>> triangles, IEnumerable<Node<Vec3<TNum>, TNum>> nodes, int depth) :
            this(triangles.Iterate().ToArray(), nodes.Iterate().ToArray(), depth) { }

        internal Mesh(Triangle3<TNum>[] triangles, Node<Vec3<TNum>, TNum>[] nodes, int depth)
        {

            _triangles = triangles;
            _nodes = nodes;
            Depth = depth;
        }

        public static Mesh<TNum> Sah(IReadOnlyList<Triangle3<TNum>> mesh, int maxDepth=32, int splitTests=4, int minNodeSize=1)
        {
            var info=Create.Sah<Triangle3<TNum>, Vec3<TNum>, TNum>(mesh, maxDepth, splitTests, minNodeSize);
            var tris = info.IndexShuffle is null
                ? mesh.Iterate().ToArray()
                : info.IndexShuffle.Iterate().Select(i => mesh[i]).ToArray();
            return new Mesh<TNum>(tris, info.Nodes, info.Depth);
        }
    }
}