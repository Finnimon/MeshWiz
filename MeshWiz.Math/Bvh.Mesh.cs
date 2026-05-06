using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeshWiz.Collections;
using MeshWiz.RefLinq;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static partial class Bvh
{
    [JsonConverter(typeof(MeshWizJsonConverter))]
    public sealed class Mesh<TNum> : IMesh<TNum>, IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>, IEquatable<Mesh<TNum>>, IJsonConverterSelfProvider
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {

        [JsonPropertyName("triangles"), JsonRequired]
        internal readonly Triangle3<TNum>[] _triangles;

        [JsonPropertyName("nodes"), JsonRequired]
        internal readonly Node<Vec3<TNum>, TNum>[] _nodes;

        [JsonPropertyName("depth"), JsonRequired]
        public int Depth { get; }
        [JsonIgnore]
        public IReadOnlyList<Node<Vec3<TNum>, TNum>> Nodes => _nodes;

        /// <inheritdoc />
        [JsonIgnore]
        IReadOnlyList<Triangle3<TNum>> IHierarchy<Triangle3<TNum>, Vec3<TNum>, TNum>.Elements => _triangles;

        [JsonIgnore]
        public ReadOnlySpan<Triangle3<TNum>> Triangles => _triangles;

        /// <inheritdoc />
        [JsonIgnore]
        public int Count => _triangles.Length;

        /// <inheritdoc />
        [JsonIgnore]
        public Triangle3<TNum> this[int index] => _triangles[index];

        [JsonIgnore]
        private TNum? _surfaceArea;

        /// <inheritdoc />
        [JsonIgnore]
        public TNum SurfaceArea => _surfaceArea ??= Mesh.Math.SurfaceArea(_triangles);


        /// <inheritdoc />
        [JsonIgnore]
        public AABB<Vec3<TNum>> BBox => Count == 0 ? AABB<Vec3<TNum>>.Empty : _nodes[0].Bounds;

        [JsonIgnore]
        private TNum? _volume;

        /// <inheritdoc />
        [JsonIgnore]
        public TNum Volume => _volume ??= Mesh.Math.Volume(_triangles);

        [JsonIgnore]
        private Vec3<TNum>? _vertCentroid;

        /// <inheritdoc />
        [JsonIgnore]
        public Vec3<TNum> VertexCentroid => _vertCentroid ??= Mesh.Math.VertexCentroid(_triangles);

        [JsonIgnore]
        private Vec3<TNum>? _surfaceCentroid;

        /// <inheritdoc />
        [JsonIgnore]
        public Vec3<TNum> SurfaceCentroid => _surfaceCentroid ??= Mesh.Math.SurfaceCentroid(_triangles);

        [JsonIgnore]
        private Vec3<TNum>? _volCentroid;

        /// <inheritdoc />
        [JsonIgnore]
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

        [JsonConstructor]
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

        /// <inheritdoc />
        public bool Equals(Mesh<TNum>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Triangles.SequenceEqual(other.Triangles) 
                   && _nodes.SequenceEqual(other._nodes) 
                   && Depth == other.Depth;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Mesh<TNum>)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(_triangles, _nodes, Depth);
        }

        public static bool operator ==(Mesh<TNum>? left, Mesh<TNum>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Mesh<TNum>? left, Mesh<TNum>? right)
        {
            return !Equals(left, right);
        }
        
        
        private sealed class Converter:JsonConverter<Mesh<TNum>>
        {
            public override Mesh<TNum> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();
                
                Triangle3<TNum>[]? triangles = null;
                Node<Vec3<TNum>, TNum>[]? nodes = null;
                int? depth = null;

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
                        case "triangles":
                            triangles = JsonSerializer.Deserialize<Triangle3<TNum>[]>(ref reader, options);
                            break;

                        case "nodes":
                            nodes = JsonSerializer.Deserialize<Node<Vec3<TNum>, TNum>[]>(ref reader, options);
                            break;

                        case "depth":
                            depth = reader.GetInt32();
                            break;

                        default:
                            reader.Skip(); // important for forward compatibility
                            break;
                    }
                }

                if (triangles is null || nodes is null || depth is null)
                    throw new JsonException("Missing required properties");

                return new Mesh<TNum>(triangles, nodes, depth.Value);
            }
            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, Mesh<TNum> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("triangles");
                JsonSerializer.Serialize(writer, value._triangles, options);
                
                writer.WritePropertyName("nodes");
                JsonSerializer.Serialize(writer, value._nodes, options);
                writer.WritePropertyName("depth");
                writer.WriteNumberValue(value.Depth);
                writer.WriteEndObject();
            }
        }

        /// <inheritdoc />
        static JsonConverter IJsonConverterSelfProvider.CreateConverter(JsonSerializerOptions options) => new Converter();

    }
}