using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Math;

public sealed partial record RotationalSurface<TNum>
{
    public readonly struct ChildSurface : IRotationalSurface<TNum>, IGeodesicProvider<ChildGeodesic, TNum>,
        IEquatable<ChildSurface>
    {
        public readonly ChildSurfaceType Type;
        public readonly int Index;

#pragma warning disable CS0649
        [SuppressMessage("ReSharper", "UnassignedReadonlyField")] 
        private readonly InlineArray8<TNum> _data;
#pragma warning restore CS0649
        static ChildSurface()
        {
            //Asserts
            var containerSize = Unsafe.SizeOf<InlineArray8<TNum>>();
            var valid = containerSize >= Unsafe.SizeOf<Cone<TNum>>()
                && containerSize>=Unsafe.SizeOf<Circle3<TNum>>()
                && containerSize>=Unsafe.SizeOf<Circle3Section<TNum>>()
                &&containerSize>=Unsafe.SizeOf<ConeSection<TNum>>()
                &&containerSize>=Unsafe.SizeOf<Cylinder<TNum>>();
            if(valid) return;
            ThrowHelper.ThrowInvalidOperationException($"{nameof(ChildSurface)} data container is too small for union");
        }
        private Cone<TNum> Cone
        {
            get => ReadData<Cone<TNum>>();
            init => WriteData(value);
        }

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void* Data() => Unsafe.AsPointer(in _data);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe T ReadData<T>()
            where T : unmanaged
            => Unsafe.ReadUnaligned<T>(Data());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteData<T>(T value)
            where T : unmanaged
            => Unsafe.WriteUnaligned(Data(), value);

        private ConeSection<TNum> ConeSection
        {
            get => ReadData<ConeSection<TNum>>();
            init => WriteData(value);
        }

        private Circle3<TNum> Circle
        {
            get => ReadData<Circle3<TNum>>();
            init => WriteData(value);
        }

        private Circle3Section<TNum> CircleSection
        {
            get => ReadData<Circle3Section<TNum>>();
            init => WriteData(value);
        }

        private Cylinder<TNum> Cylinder
        {
            get => ReadData<Cylinder<TNum>>();
            init => WriteData(value);
        }

        private ChildSurface(ChildSurfaceType type, int index)
        {
            Type = type;
            Index = index;
        }

        [Pure]
        public static ChildSurface Create(int index, Cone<TNum> surface)
            => new(ChildSurfaceType.Cone, index) { Cone = surface };

        [Pure]
        public static ChildSurface Create(int index, ConeSection<TNum> surface)
            => new(ChildSurfaceType.ConeSection, index) { ConeSection = surface };

        [Pure]
        public static ChildSurface Create(int index, Cylinder<TNum> surface)
            => new(ChildSurfaceType.Cylinder, index) { Cylinder = surface };

        [Pure]
        public static ChildSurface Create(int index, Circle3<TNum> surface)
            => new(ChildSurfaceType.Circle, index) { Circle = surface };

        [Pure]
        public static ChildSurface Create(int index, Circle3Section<TNum> surface)
            => new(ChildSurfaceType.CircleSection, index) { CircleSection = surface };


        [Pure]
        public IRotationalSurface<TNum> Surface => Type switch
        {
            ChildSurfaceType.Cylinder => Cylinder,
            ChildSurfaceType.Cone => Cone,
            ChildSurfaceType.ConeSection => ConeSection,
            ChildSurfaceType.Circle => Circle,
            ChildSurfaceType.CircleSection => CircleSection,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<IRotationalSurface<TNum>>()
        };

        /// <inheritdoc />
        public Vec3<TNum> Centroid => Type switch
        {
            ChildSurfaceType.Cylinder => Cylinder.Centroid,
            ChildSurfaceType.Cone => Cone.Centroid,
            ChildSurfaceType.ConeSection => ConeSection.Centroid,
            ChildSurfaceType.Circle => Circle.Centroid,
            ChildSurfaceType.CircleSection => CircleSection.Centroid,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vec3<TNum>>()
        };

        /// <inheritdoc />
        public TNum SurfaceArea => Type switch
        {
            ChildSurfaceType.Cylinder => Cylinder.SurfaceArea,
            ChildSurfaceType.Cone => Cone.SurfaceArea,
            ChildSurfaceType.ConeSection => ConeSection.SurfaceArea,
            ChildSurfaceType.Circle => Circle.SurfaceArea,
            ChildSurfaceType.CircleSection => CircleSection.SurfaceArea,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<TNum>()
        };

        /// <inheritdoc />
        public AABB<Vec3<TNum>> BBox => Type switch
        {
            ChildSurfaceType.Cylinder => Cylinder.BBox,
            ChildSurfaceType.Cone => Cone.BBox,
            ChildSurfaceType.ConeSection => ConeSection.BBox,
            ChildSurfaceType.Circle => Circle.BBox,
            ChildSurfaceType.CircleSection => CircleSection.BBox,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<AABB<Vec3<TNum>>>()
        };

        /// <inheritdoc />
        public IMesh<TNum> Tessellate() => Type switch
        {
            ChildSurfaceType.Cylinder => Cylinder.Tessellate(),
            ChildSurfaceType.Cone => Cone.Tessellate(),
            ChildSurfaceType.ConeSection => ConeSection.Tessellate(),
            ChildSurfaceType.Circle => Circle.Tessellate(),
            ChildSurfaceType.CircleSection => CircleSection.Tessellate(),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<IMesh<TNum>>()
        };

        /// <inheritdoc />
        public Vec3<TNum> NormalAt(Vec3<TNum> p)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Cylinder.NormalAt(p),
                ChildSurfaceType.Cone => Cone.NormalAt(p),
                ChildSurfaceType.ConeSection => ConeSection.NormalAt(p),
                ChildSurfaceType.Circle => Circle.NormalAt(p),
                ChildSurfaceType.CircleSection => CircleSection.NormalAt(p),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vec3<TNum>>()
            };

        /// <inheritdoc />
        public Vec3<TNum> ClampToSurface(Vec3<TNum> p)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Cylinder.ClampToSurface(p),
                ChildSurfaceType.Cone => Cone.ClampToSurface(p),
                ChildSurfaceType.ConeSection => ConeSection.ClampToSurface(p),
                ChildSurfaceType.Circle => Circle.ClampToSurface(p),
                ChildSurfaceType.CircleSection => CircleSection.ClampToSurface(p),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vec3<TNum>>()
            };

        /// <inheritdoc />
        IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesic(Vec3<TNum> p1,
            Vec3<TNum> p2)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Cylinder.GetGeodesic(p1, p2),
                ChildSurfaceType.Cone => Cone.GetGeodesic(p1, p2),
                ChildSurfaceType.ConeSection => ConeSection.GetGeodesic(p1, p2),
                ChildSurfaceType.Circle => Circle.GetGeodesic(p1, p2),
                ChildSurfaceType.CircleSection => CircleSection.GetGeodesic(p1, p2),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum>>()
            };

        /// <inheritdoc />
        public ChildGeodesic GetGeodesicFromEntry(Vec3<TNum> entryPoint, Vec3<TNum> direction)
            => Type switch
            {
                ChildSurfaceType.Cylinder => ChildGeodesic.CreateCylinder(Index,
                    Cylinder.GetGeodesicFromEntry(entryPoint, direction)),
                ChildSurfaceType.Cone => ChildGeodesic.CreateCone(Index,
                    Cone.GetGeodesicFromEntry(entryPoint, direction)),
                ChildSurfaceType.ConeSection => ChildGeodesic.CreateConeSection(Index,
                    ConeSection.GetGeodesicFromEntry(entryPoint, direction)),
                ChildSurfaceType.Circle => ChildGeodesic.CreateCircle(Index,
                    Circle.GetGeodesicFromEntry(entryPoint, direction)),
                ChildSurfaceType.CircleSection => ChildGeodesic.CreateCircleSection(Index,
                    CircleSection.GetGeodesicFromEntry(entryPoint, direction)),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<ChildGeodesic>()
            };

        /// <inheritdoc />
        public ChildGeodesic GetGeodesic(Vec3<TNum> p1, Vec3<TNum> p2)
            => Type switch
            {
                ChildSurfaceType.Cylinder => ChildGeodesic.CreateCylinder(Index, Cylinder.GetGeodesic(p1, p2)),
                ChildSurfaceType.Cone => ChildGeodesic.CreateCone(Index, Cone.GetGeodesic(p1, p2)),
                ChildSurfaceType.ConeSection => ChildGeodesic.CreateConeSection(Index, ConeSection.GetGeodesic(p1, p2)),
                ChildSurfaceType.Circle => ChildGeodesic.CreateCircle(Index, Circle.GetGeodesic(p1, p2)),
                ChildSurfaceType.CircleSection => ChildGeodesic.CreateCircleSection(Index,
                    CircleSection.GetGeodesic(p1, p2)),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<ChildGeodesic>()
            };

        /// <inheritdoc />
        IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesicFromEntry(
            Vec3<TNum> entryPoint,
            Vec3<TNum> direction)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Cylinder.GetGeodesicFromEntry(entryPoint, direction),
                ChildSurfaceType.Cone => Cone.GetGeodesicFromEntry(entryPoint, direction),
                ChildSurfaceType.ConeSection => ConeSection.GetGeodesicFromEntry(entryPoint, direction),
                ChildSurfaceType.Circle => Circle.GetGeodesicFromEntry(entryPoint, direction),
                ChildSurfaceType.CircleSection => CircleSection.GetGeodesicFromEntry(entryPoint, direction),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum>>()
            };

        /// <inheritdoc />
        public IDiscreteCurve<Vec3<TNum>, TNum> SweepCurve => Type switch
        {
            ChildSurfaceType.Cylinder => Cylinder.SweepCurve,
            ChildSurfaceType.Cone => Cone.SweepCurve,
            ChildSurfaceType.ConeSection => ConeSection.SweepCurve,
            ChildSurfaceType.Circle => Circle.SweepCurve,
            ChildSurfaceType.CircleSection => CircleSection.SweepCurve,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<IDiscreteCurve<Vec3<TNum>, TNum>>()
        };

        /// <inheritdoc />
        public Ray3<TNum> SweepAxis => Type switch
        {
            ChildSurfaceType.Cylinder => Cylinder.SweepAxis,
            ChildSurfaceType.Cone => Cone.SweepAxis,
            ChildSurfaceType.ConeSection => ConeSection.SweepAxis,
            ChildSurfaceType.Circle => Circle.SweepAxis,
            ChildSurfaceType.CircleSection => CircleSection.SweepAxis,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Ray3<TNum>>()
        };

        /// <inheritdoc />
        public bool Equals(ChildSurface other) =>
            Type == other.Type
            && Index == other.Index
            && ((ReadOnlySpan<TNum>)_data).SequenceEqual(other._data);


        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is ChildSurface other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine((int)Type, Index);

        public static bool operator ==(ChildSurface left, ChildSurface right) => left.Equals(right);

        public static bool operator !=(ChildSurface left, ChildSurface right) => !left.Equals(right);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChildSurface CreateDead(int index = -1) => new(ChildSurfaceType.Dead, index);
    }
}