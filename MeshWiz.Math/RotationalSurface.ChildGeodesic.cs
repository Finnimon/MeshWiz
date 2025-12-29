using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public sealed partial record RotationalSurface<TNum>
{
    public readonly struct ChildGeodesic : IDiscretePoseCurve<Pose3<TNum>, Vector3<TNum>, TNum>,
        IEquatable<ChildGeodesic>
    {
        public readonly ChildSurfaceType Type;
        public readonly int Index;

#pragma warning disable CS0649
        [SuppressMessage("ReSharper", "UnassignedReadonlyField")] 
        private readonly InlineArray14<TNum> _data;
#pragma warning restore CS0649
        
        static ChildGeodesic()
        {
            var containerSize = Unsafe.SizeOf<InlineArray14<TNum>>();
            var coneGeodesicSize = Unsafe.SizeOf<ConeGeodesic<TNum>>();
            var poseLineSize = Unsafe.SizeOf<PoseLine<Pose3<TNum>, Vector3<TNum>, TNum>>();
            var helixSize = Unsafe.SizeOf<Helix<TNum>>();
            var valid = containerSize >= coneGeodesicSize
                        && containerSize >= poseLineSize
                        && containerSize >= helixSize;
            if(valid) return;
            ThrowHelper.ThrowInvalidOperationException($"{nameof(ChildSurface)} data container is too small for union");
        }
        private Helix<TNum> Helix
        {
            get => ReadData<Helix<TNum>>();
            init => WriteData(value);
        }

        private ConeGeodesic<TNum> ConeGeodesic
        {
            get => ReadData<ConeGeodesic<TNum>>();
            init => WriteData(value);
        }

        private PoseLine<Pose3<TNum>, Vector3<TNum>, TNum> Line
        {
            get => ReadData<PoseLine<Pose3<TNum>, Vector3<TNum>, TNum>>();
            init => WriteData(value);
        }

        public IDiscretePoseCurve<Pose3<TNum>, Vector3<TNum>, TNum> PoseCurve => Type switch
        {
            ChildSurfaceType.Cylinder => Helix,
            ChildSurfaceType.Cone => ConeGeodesic,
            ChildSurfaceType.ConeSection => ConeGeodesic,
            ChildSurfaceType.Circle => Line,
            ChildSurfaceType.CircleSection => Line,
            _ => ThrowHelper.ThrowInvalidOperationException<IDiscretePoseCurve<Pose3<TNum>, Vector3<TNum>, TNum>>()
        };

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

        private ChildGeodesic(ChildSurfaceType type, int index)
        {
            Type = type;
            Index = index;
        }

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChildGeodesic CreateCone(int index, ConeGeodesic<TNum> geodesic)
            => new(ChildSurfaceType.Cone, index) { ConeGeodesic = geodesic };

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChildGeodesic CreateConeSection(int index, ConeGeodesic<TNum> geodesic)
            => new(ChildSurfaceType.ConeSection, index) { ConeGeodesic = geodesic };

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChildGeodesic CreateCylinder(int index, Helix<TNum> geodesic)
            => new(ChildSurfaceType.Cylinder, index) { Helix = geodesic };

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChildGeodesic CreateCircle(int index, PoseLine<Pose3<TNum>, Vector3<TNum>, TNum> geodesic)
            => new(ChildSurfaceType.Circle, index) { Line = geodesic };

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChildGeodesic CreateCircleSection(int index, PoseLine<Pose3<TNum>, Vector3<TNum>, TNum> geodesic)
            => new(ChildSurfaceType.CircleSection, index) { Line = geodesic };

        /// <inheritdoc />
        public Vector3<TNum> Traverse(TNum t)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Helix.Traverse(t),
                ChildSurfaceType.Cone => ConeGeodesic.Traverse(t),
                ChildSurfaceType.ConeSection => ConeGeodesic.Traverse(t),
                ChildSurfaceType.Circle => Line.Traverse(t),
                ChildSurfaceType.CircleSection => Line.Traverse(t),
                _ => ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>()
            };

        /// <inheritdoc />
        public Vector3<TNum> GetTangent(TNum t)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Helix.GetTangent(t),
                ChildSurfaceType.Cone => ConeGeodesic.GetTangent(t),
                ChildSurfaceType.ConeSection => ConeGeodesic.GetTangent(t),
                ChildSurfaceType.Circle => Line.GetTangent(t),
                ChildSurfaceType.CircleSection => Line.GetTangent(t),
                _ => ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>()
            };


        /// <inheritdoc />
        public Pose3<TNum> GetPose(TNum t) => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.GetPose(t),
            ChildSurfaceType.Cone => ConeGeodesic.GetPose(t),
            ChildSurfaceType.ConeSection => ConeGeodesic.GetPose(t),
            ChildSurfaceType.Circle => Line.GetPose(t),
            ChildSurfaceType.CircleSection => Line.GetPose(t),
            _ => ThrowHelper.ThrowInvalidOperationException<Pose3<TNum>>()
        };

        /// <inheritdoc />
        public Vector3<TNum> Start => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.Start,
            ChildSurfaceType.Cone => ConeGeodesic.Start,
            ChildSurfaceType.ConeSection => ConeGeodesic.Start,
            ChildSurfaceType.Circle => Line.Start,
            ChildSurfaceType.CircleSection => Line.Start,
            _ => ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>()
        };


        /// <inheritdoc />
        public Vector3<TNum> End => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.End,
            ChildSurfaceType.Cone => ConeGeodesic.End,
            ChildSurfaceType.ConeSection => ConeGeodesic.End,
            ChildSurfaceType.Circle => Line.End,
            ChildSurfaceType.CircleSection => Line.End,
            _ => ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>()
        };

        /// <inheritdoc />
        public Vector3<TNum> TraverseOnCurve(TNum t) => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.TraverseOnCurve(t),
            ChildSurfaceType.Cone => ConeGeodesic.TraverseOnCurve(t),
            ChildSurfaceType.ConeSection => ConeGeodesic.TraverseOnCurve(t),
            ChildSurfaceType.Circle => Line.TraverseOnCurve(t),
            ChildSurfaceType.CircleSection => Line.TraverseOnCurve(t),
            _ => ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>()
        };

        /// <inheritdoc />
        public TNum Length => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.Length,
            ChildSurfaceType.Cone => ConeGeodesic.Length,
            ChildSurfaceType.ConeSection => ConeGeodesic.Length,
            ChildSurfaceType.Circle => Line.Length,
            ChildSurfaceType.CircleSection => Line.Length,
            _ => ThrowHelper.ThrowInvalidOperationException<TNum>()
        };

        /// <inheritdoc />
        public Polyline<Vector3<TNum>, TNum> ToPolyline() => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.ToPolyline(),
            ChildSurfaceType.Cone => ConeGeodesic.ToPolyline(),
            ChildSurfaceType.ConeSection => ConeGeodesic.ToPolyline(),
            ChildSurfaceType.Circle => Line.ToPolyline(),
            ChildSurfaceType.CircleSection => Line.ToPolyline(),
            _ => ThrowHelper.ThrowInvalidOperationException<Polyline<Vector3<TNum>, TNum>>()
        };

        /// <inheritdoc />
        public Polyline<Vector3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Helix.ToPolyline(tessellationParameter),
                ChildSurfaceType.Cone => ConeGeodesic.ToPolyline(tessellationParameter),
                ChildSurfaceType.ConeSection => ConeGeodesic.ToPolyline(tessellationParameter),
                ChildSurfaceType.Circle => Line.ToPolyline(tessellationParameter),
                ChildSurfaceType.CircleSection => Line.ToPolyline(tessellationParameter),
                _ => ThrowHelper.ThrowInvalidOperationException<Polyline<Vector3<TNum>, TNum>>()
            };

        /// <inheritdoc />
        public Vector3<TNum> EntryDirection => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.EntryDirection,
            ChildSurfaceType.Cone => ConeGeodesic.EntryDirection,
            ChildSurfaceType.ConeSection => ConeGeodesic.EntryDirection,
            ChildSurfaceType.Circle => Line.EntryDirection,
            ChildSurfaceType.CircleSection => Line.EntryDirection,
            _ => ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>()
        };

        /// <inheritdoc />
        public Vector3<TNum> ExitDirection => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.ExitDirection,
            ChildSurfaceType.Cone => ConeGeodesic.ExitDirection,
            ChildSurfaceType.ConeSection => ConeGeodesic.ExitDirection,
            ChildSurfaceType.Circle => Line.ExitDirection,
            ChildSurfaceType.CircleSection => Line.ExitDirection,
            _ => ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>()
        };

        /// <inheritdoc />
        public Pose3<TNum> StartPose => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.StartPose,
            ChildSurfaceType.Cone => ConeGeodesic.StartPose,
            ChildSurfaceType.ConeSection => ConeGeodesic.StartPose,
            ChildSurfaceType.Circle => Line.StartPose,
            ChildSurfaceType.CircleSection => Line.StartPose,
            _ => ThrowHelper.ThrowInvalidOperationException<Pose3<TNum>>()
        };

        /// <inheritdoc />
        public Pose3<TNum> EndPose => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.EndPose,
            ChildSurfaceType.Cone => ConeGeodesic.EndPose,
            ChildSurfaceType.ConeSection => ConeGeodesic.EndPose,
            ChildSurfaceType.Circle => Line.EndPose,
            ChildSurfaceType.CircleSection => Line.EndPose,
            _ => ThrowHelper.ThrowInvalidOperationException<Pose3<TNum>>()
        };

        /// <inheritdoc />
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum> ToPosePolyline()
            => Type switch
            {
                ChildSurfaceType.Cylinder => Helix.ToPosePolyline(),
                ChildSurfaceType.Cone => ConeGeodesic.ToPosePolyline(),
                ChildSurfaceType.ConeSection => ConeGeodesic.ToPosePolyline(),
                ChildSurfaceType.Circle => Line.ToPosePolyline(),
                ChildSurfaceType.CircleSection => Line.ToPosePolyline(),
                _ => ThrowHelper.ThrowInvalidOperationException<PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>>()
            };

        /// <inheritdoc />
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum> ToPosePolyline(
            PolylineTessellationParameter<TNum> tessellationParameter)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Helix.ToPosePolyline(tessellationParameter),
                ChildSurfaceType.Cone => ConeGeodesic.ToPosePolyline(tessellationParameter),
                ChildSurfaceType.ConeSection => ConeGeodesic.ToPosePolyline(tessellationParameter),
                ChildSurfaceType.Circle => Line.ToPosePolyline(tessellationParameter),
                ChildSurfaceType.CircleSection => Line.ToPosePolyline(tessellationParameter),
                _ => ThrowHelper.ThrowInvalidOperationException<PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>>()
            };

        /// <inheritdoc />
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ChildGeodesic other) =>
            Type == other.Type
            && Index == other.Index
            && ((ReadOnlySpan<TNum>)_data).SequenceEqual(other._data);


        /// <inheritdoc />
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is ChildGeodesic other && Equals(other);

        /// <inheritdoc />
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() =>
            HashCode.Combine((int)Type, Index);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ChildGeodesic left, ChildGeodesic right) => left.Equals(right);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ChildGeodesic left, ChildGeodesic right) => !left.Equals(right);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChildGeodesic CreateDead(int index = -1) => new(ChildSurfaceType.Dead, index);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<Arithmetics, TNum> SolveIntersection(Plane3<TNum> plane)
            => Type switch
            {
                ChildSurfaceType.Cylinder => Curve.Solver.IntersectionNewton(Helix, plane),
                ChildSurfaceType.Cone => Curve.Solver.IntersectionNewton(ConeGeodesic, plane),
                ChildSurfaceType.ConeSection => Curve.Solver.IntersectionNewton(ConeGeodesic, plane),
                ChildSurfaceType.Circle => LineIntersect(plane),
                ChildSurfaceType.CircleSection => LineIntersect(plane),
                _ => Result<Arithmetics, TNum>.DefaultFailure
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<Arithmetics, TNum> LineIntersect(Plane3<TNum> plane) =>
            plane.IntersectParameter(Line, out var t)
                ? t
                : Result<Arithmetics, TNum>.DefaultFailure;

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ray3<TNum> GetRay(TNum t) => Type switch
        {
            ChildSurfaceType.Cylinder => Helix.GetRay(t),
            ChildSurfaceType.Cone => ConeGeodesic.GetRay(t),
            ChildSurfaceType.ConeSection => ConeGeodesic.GetRay(t),
            ChildSurfaceType.Circle => (Line<Vector3<TNum>, TNum>)Line,
            ChildSurfaceType.CircleSection => (Line<Vector3<TNum>, TNum>)Line,
            _ => ThrowHelper.ThrowInvalidOperationException<Ray3<TNum>>()
        };
    }
}