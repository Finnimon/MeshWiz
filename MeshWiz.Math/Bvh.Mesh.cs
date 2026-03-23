using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Collections;
using MeshWiz.RefLinq;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static partial class Bvh
{
    public class Mesh<TNum> : IMesh<TNum>, IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        internal readonly Triangle3<TNum>[] _triangles;
        internal readonly Node<Vec3<TNum>, TNum>[] _nodes;
        public IReadOnlyList<Node<Vec3<TNum>, TNum>> Nodes => _nodes;

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

        [Pure]
        public static Mesh<TNum> Sah(IReadOnlyList<Triangle3<TNum>> mesh, int maxDepth = 32, int splitTests = 4,
            int minNodeSize = 1)
        {
            var info = Create.Sah<Triangle3<TNum>, Vec3<TNum>, TNum>(mesh, maxDepth, splitTests, minNodeSize);
            var tris = info.IndexShuffle is null
                ? mesh.Iterate().ToArray()
                : info.IndexShuffle.Iterate().Select(i => mesh[i]).ToArray();
            return new Mesh<TNum>(tris, info.Nodes, info.Depth);
        }

        [Pure]
        public Mesh<TNum> Clone() => new(_triangles.AsSpan().ToArray(), _nodes.AsSpan().ToArray(), Depth);

        [Pure]
        public TransformableMesh<TNum> ToTransformable() => new(this);


        public ICollection<Line<Vec3<TNum>,TNum>> GetAllIntersections(Plane<TNum> plane,
            ICollection<Line<Vec3<TNum>, TNum>>? buf = null)
        {
            var res = buf ?? new List<Line<Vec3<TNum>, TNum>>();
            var hit = Traverse(this,
                plane.DoIntersect,
                t =>
                {
                    var hit = plane.Intersect(t, out var l);
                    return (l, hit);
                },
                (_, _, l) =>
                {
                    res.Add(l);
                    return HitReact.ContinueCurrentNode;
                });
            return res;
        }

        
        public TCollection Intersect<TCollection>(Plane<TNum> plane,
            TCollection? buf = default)
        where TCollection: ICollection<Line<Vec2<TNum>,TNum>>, new()
        {
            var posed = plane.Precalculated();
            buf ??= [];
            var hit = Traverse(this,
                posed.DoIntersect,
                t =>
                {
                    var hit = posed.Intersect(t, out var l);
                    return (l, hit);
                },
                (_, _, l) =>
                {
                    buf.Add(plane.ProjectIntoLocal(l));
                    return HitReact.ContinueCurrentNode;
                });
            return buf;
        }

        private readonly struct PlaneIntersecter(Plane<TNum> plane)
            : ITraverser<Triangle3<TNum>, Line<Vec3<TNum>, TNum>, Vec3<TNum>, TNum>
        {
            /// <inheritdoc />
            public bool DoIntersect(AABB<Vec3<TNum>> t) => plane.DoIntersect(t);

            /// <inheritdoc />
            public bool Intersect(Triangle3<TNum> element, out Line<Vec3<TNum>, TNum> intersection)
                => plane.Intersect(element, out intersection);

            /// <inheritdoc />
            public HitReact AcceptHit(int index, Triangle3<TNum> element, Line<Vec3<TNum>, TNum> hit)
            {
                throw new NotImplementedException();
            }
        }
    }
}