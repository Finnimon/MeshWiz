using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Collections;
using MeshWiz.RefLinq;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static partial class Bvh
{
    public sealed class TransformableMesh<TNum> : IMesh<TNum>, IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        private readonly Mesh<TNum> _untransformed;
        // private readonly TriangleIndexer[] _indexers;
        // public ReadOnlySpan<Vec3<TNum>> SourceVertices => _srcVerts;
        // private readonly Vec3<TNum>[] _srcVerts;
        /// <inheritdoc />
        public bool IsTransforming => true;
        public SelectList<Node<Vec3<TNum>, TNum>,Node<Vec3<TNum>, TNum>> Nodes { get; }
        IReadOnlyList<Node<Vec3<TNum>, TNum>> IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>.Nodes => Nodes;
        public int Depth => _untransformed.Depth;

        /// <inheritdoc />
        IReadOnlyList<Triangle3<TNum>> IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>.Elements => this;

        public ReadOnlySpan<Triangle3<TNum>> UntransformedTriangles => _untransformed._triangles;

        public IReadOnlyList<Triangle3<TNum>> TransformedTriangles => this;

        /// <inheritdoc />
        public int Count => _untransformed.Count;

        /// <inheritdoc />
        public Triangle3<TNum> this[int index] =>
            Triangle3<TNum>.Transform(_transform, _untransformed._triangles[index]);


        private TNum? _surfaceArea;

        /// <inheritdoc />
        public TNum SurfaceArea => _surfaceArea ??= Mesh.Math.SurfaceArea(this);


        /// <inheritdoc />
        public AABB<Vec3<TNum>> BBox => Count == 0 ? AABB<Vec3<TNum>>.Empty : Nodes[0].Bounds;

        private TNum? _volume;

        /// <inheritdoc />
        public TNum Volume => _volume ??= Mesh.Math.Volume(this);

        private Vec3<TNum>? _vertCentroid;

        /// <inheritdoc />
        public Vec3<TNum> VertexCentroid => _vertCentroid ??= Mesh.Math.VertexCentroid(this);

        private Vec3<TNum>? _surfaceCentroid;

        /// <inheritdoc />
        public Vec3<TNum> SurfaceCentroid => _surfaceCentroid ??= Mesh.Math.SurfaceCentroid(this);

        private Vec3<TNum>? _volCentroid;

        /// <inheritdoc />
        public Vec3<TNum> VolumeCentroid => _volCentroid ??= Mesh.Math.VolumeCentroid(this);


        public TransformableMesh(Mesh<TNum> source)
        {
            _untransformed = source;
             Nodes = _untransformed._nodes.SelectList(n => n.WithBounds(AABB.Transform(_transform, n.Bounds)));

        }

        private Mat4x4<TNum> _transform = Mat4x4<TNum>.Identity;

        public Mat4x4<TNum> Transform
        {
            get => _transform;
            set
            {
                if (Mat4x4<TNum>.AsNested(value).IsApprox(_transform, Vec4<TNum>.Create(Numbers<TNum>.ZeroEpsilon)))
                    return;
                _transform = value;
                OnTransformed();
            }
        }

        private void OnTransformed()
        {
            var det = _transform.AsMat3x3().Det;
            _surfaceArea = _untransformed.SurfaceArea * det;
            _volume = _untransformed.Volume * det;
            _vertCentroid = Mat4x4<TNum>.MultiplyPoint(_transform, _untransformed.VertexCentroid);
            _surfaceCentroid = Mat4x4<TNum>.MultiplyPoint(_transform, _untransformed.SurfaceCentroid);
            _volCentroid = Mat4x4<TNum>.MultiplyPoint(_transform, _untransformed.VolumeCentroid);
            // RecomputeTransformedTriangles();
            // RecomputeNodeBounds();
        }


        /// <inheritdoc />
        public void InitializeLazies()
        {
            var anyNull = _surfaceArea is null
                          || _volume is null
                          || _vertCentroid is null
                          || _surfaceCentroid is null
                          || _volCentroid is null;
            if (!anyNull) return;

            var allInfo = Mesh.Math.AllInfo(_untransformed);
            _surfaceArea = allInfo.SurfaceArea;
            _volume = allInfo.Volume;
            _vertCentroid = allInfo.VertexCentroid;
            _surfaceCentroid = allInfo.SurfaceCentroid;
            _volCentroid = allInfo.VolumeCentroid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TraverseBvh<TTraverser, TIntersection>(TTraverser traverser)
            where TTraverser : ITraverser<Triangle3<TNum>, TIntersection, Vec3<TNum>, TNum>, allows ref struct =>
            Traverse<TTraverser, Triangle3<TNum>, TIntersection, Vec3<TNum>, TNum>(this, traverser);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TraverseBvh<TIntersection>(
            Func<AABB<Vec3<TNum>>, bool> bBoxDoIntersect,
            Func<Triangle3<TNum>, (TIntersection, bool)> elementIntersect,
            Func<int, Triangle3<TNum>, TIntersection, Bvh.HitReact> acceptHitReact)
            => Bvh.Traverse(this, bBoxDoIntersect, elementIntersect, acceptHitReact);

        public bool Intersects(IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum> other) =>
            TraverseAgainst(this, other, ignoreTouching: true, Triangle3<TNum>.DoIntersect);
        
        public bool IntersectsOrTouches(IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum> other) =>
            TraverseAgainst(this, other, ignoreTouching: false, Triangle3<TNum>.DoIntersectOrTouch);
    
        [Pure]
        public Mesh<TNum> ToStatic() => Transform.IsIdentity ? _untransformed : new Mesh<TNum>(this, Nodes, Depth);
    }
}