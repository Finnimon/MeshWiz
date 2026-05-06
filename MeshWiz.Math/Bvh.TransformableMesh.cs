using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeshWiz.Collections;
using MeshWiz.RefLinq;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static partial class Bvh
{
    [JsonConverter(typeof(MeshWizJsonConverter))]
    public sealed class TransformableMesh<TNum> : IMesh<TNum>, IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>, IJsonConverterSelfProvider, IEquatable<TransformableMesh<TNum>>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        private readonly Mesh<TNum> _untransformed;

        // private readonly TriangleIndexer[] _indexers;
        // public ReadOnlySpan<Vec3<TNum>> SourceVertices => _srcVerts;
        // private readonly Vec3<TNum>[] _srcVerts;
        /// <inheritdoc />
        public bool IsTransforming => true;

        public SelectList<Node<Vec3<TNum>, TNum>, Node<Vec3<TNum>, TNum>> Nodes { get; }
        IReadOnlyList<Node<Vec3<TNum>, TNum>> IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>.Nodes => Nodes;
        public int Depth => _untransformed.Depth;

        /// <inheritdoc />
        IReadOnlyList<Triangle3<TNum>> IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>.Elements => this;

        public ReadOnlySpan<Triangle3<TNum>> UntransformedTriangles => _untransformed._triangles;


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

        private sealed class Converter : JsonConverter<TransformableMesh<TNum>>
        {
            public override TransformableMesh<TNum> Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();

                Mesh<TNum>? untransformed = null;
                Mat4x4<TNum>? transform = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException();

                    string propName = reader.GetString()!;
                    reader.Read();

                    switch (propName)
                    {
                        case nameof(untransformed):
                            untransformed = JsonSerializer.Deserialize<Mesh<TNum>>(ref reader, options);
                            break;
                        case nameof(transform):
                            transform = JsonSerializer.Deserialize<Mat4x4<TNum>>(ref reader, options);
                            break;
                        default:
                            reader.Skip(); // important for forward compatibility
                            break;
                    }
                }

                if (untransformed is null || transform is null)
                    throw new JsonException("Missing required properties");

                return new TransformableMesh<TNum>(untransformed) { Transform = transform.Value };
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, TransformableMesh<TNum> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("untransformed");
                JsonSerializer.Serialize(writer, value._untransformed, options);
                writer.WritePropertyName("transform");
                JsonSerializer.Serialize(writer, value._transform, options);
                writer.WriteEndObject();
            }
        }

        /// <inheritdoc />
        public static JsonConverter CreateConverter(JsonSerializerOptions options) => new Converter();

        /// <inheritdoc />
        public bool Equals(TransformableMesh<TNum>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _untransformed.Equals(other._untransformed) 
                   && _transform.Equals(other._transform);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is TransformableMesh<TNum> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(_untransformed);

        public static bool operator ==(TransformableMesh<TNum>? left, TransformableMesh<TNum>? right) => Equals(left, right);

        public static bool operator !=(TransformableMesh<TNum>? left, TransformableMesh<TNum>? right) => !Equals(left, right);
    }
}