using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Curve
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Section<TSource, TVec, TNum> CreateSection<TSource, TVec, TNum>(this TSource sourceCurve, TNum start,
        TNum end)
        where TSource : ICurve<TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum> =>
        new(sourceCurve, start, end);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Section<TSource, TVec, TNum> CreateExactSection<TSource, TVec, TNum>(this TSource sourceCurve,
        TNum start,
        TNum end)
        where TSource : IDiscreteCurve<TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var invLen = TNum.One / sourceCurve.Length;
        return new Section<TSource, TVec, TNum>(sourceCurve, start * invLen, end * invLen);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PoseSection<TSource, TPose, TVec, TNum> CreateSection<TSource, TPose, TVec, TNum>(
        this TSource sourceCurve, TNum start,
        TNum end)
        where TSource : IPoseCurve<TPose, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TPose : IPose<TPose, TVec, TNum>
        => new(sourceCurve, start, end);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PoseSection<TSource, TPose, TVec, TNum> CreateExactSection<TSource, TPose, TVec, TNum>(
        this TSource sourceCurve,
        TNum start,
        TNum end)
        where TSource : IDiscretePoseCurve<TPose, TVec, TNum>
        where TPose : IPose<TPose, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var invLen = TNum.One / sourceCurve.Length;
        return new PoseSection<TSource, TPose, TVec, TNum>(sourceCurve, start * invLen, end * invLen);
    }


    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Section<TSource, TVec, TNum>(TSource source, TNum start, TNum end)
        : IDiscreteCurve<TVec, TNum>
        where TSource : ICurve<TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public readonly TSource Source = source;
        public readonly TNum StartParam = start;
        public readonly TNum EndParam = end;
        public AABB<TNum> ParametricBounds => AABB.From(StartParam, EndParam);
        public TVec Start => Source.Traverse(StartParam);
        public TVec End => Source.Traverse(EndParam);

        /// <inheritdoc />
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TVec TraverseOnCurve(TNum t) => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TNum ScaleParameter(TNum t) => TNum.Lerp(StartParam, EndParam, t);

        /// <inheritdoc />
        public TNum Length => ComputeLength();

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        private TNum ComputeLength() =>
            Source is IDiscreteCurve<TVec, TNum> discrete ? DiscreteLength(discrete) : IterativeLength();

        private TNum IterativeLength(int steps = 64)
        {
            var stepsNum = TNum.CreateTruncating(steps);
            var scalar = TNum.One / (stepsNum + TNum.One);
            var previous = Start;
            var len = TNum.Zero;
            for (var i = 0; i < steps; i++)
            {
                var t = TNum.CreateTruncating(i + 1) * scalar;
                var curPt = Traverse(t);
                var curLen = curPt.DistanceTo(previous);

                len += curLen;
                if (i != 1)
                    continue;

                var assumeStableDistr = len.IsApprox(curLen + curLen);
                if (!assumeStableDistr)
                    continue;
                return stepsNum * curLen;
            }

            return len;
        }

        private TNum DiscreteLength(IDiscreteCurve<TVec, TNum> discrete) => discrete.Length * ParametricBounds.Size;

        /// <inheritdoc />
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TVec Traverse(TNum t) => Source.Traverse(ScaleParameter(t));

        /// <inheritdoc />
        public bool IsClosed => !StartParam.IsApprox(EndParam) && Start.IsApprox(End);

        /// <inheritdoc />
        public Polyline<TVec, TNum> ToPolyline()
        {
            const int steps = 64;
            const int n = steps + 1;
            return ToPolyline(n);
        }

        private Polyline<TVec, TNum> ToPolyline(int numPts)
        {
            var scalar = TNum.One / TNum.CreateTruncating(numPts);
            return Polyline<TVec, TNum>.CreateCulled(Enumerable.Range(0, numPts).Select(TNum.CreateTruncating)
                .Select(t => t * scalar)
                .Select(Traverse));
        }

        /// <inheritdoc />
        public Polyline<TVec, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
            => ThrowHelper.ThrowNotSupportedException<Polyline<TVec, TNum>>();
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PoseSection<TSource, TPose, TVec, TNum>(TSource source, TNum start, TNum end)
        : IDiscretePoseCurve<TPose, TVec, TNum>
        where TSource : IPoseCurve<TPose, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TPose : IPose<TPose, TVec, TNum>
    {
        public readonly TSource Source = source;
        public readonly TNum StartParam = start;
        public readonly TNum EndParam = end;
        public AABB<TNum> ParametricBounds => AABB.From(StartParam, EndParam);
        public TVec Start => Source.Traverse(StartParam);
        public TVec End => Source.Traverse(EndParam);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TVec TraverseOnCurve(TNum t) => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TNum ScaleParameter(TNum t) => TNum.Lerp(StartParam, EndParam, t);

        public TNum Length => ComputeLength();

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        private TNum ComputeLength() =>
            Source is IDiscreteCurve<TVec, TNum> discrete ? DiscreteLength(discrete) : IterativeLength();

        private TNum IterativeLength(int steps = 128)
        {
            var stepsNum = TNum.CreateTruncating(steps);
            var scalar = TNum.One / (stepsNum + TNum.One);
            var previous = Start;
            var len = TNum.Zero;
            for (var i = 0; i < steps; i++)
            {
                var t = TNum.CreateTruncating(i + 1) * scalar;
                var curPt = Traverse(t);
                var curLen = curPt.DistanceTo(previous);

                len += curLen;
                if (i != 1)
                    continue;

                var assumeStableDistr = len.IsApprox(curLen + curLen); //normally true
                if (!assumeStableDistr)
                    continue;
                return stepsNum * curLen;
            }

            return len;
        }

        private TNum DiscreteLength(IDiscreteCurve<TVec, TNum> discrete) => discrete.Length * ParametricBounds.Size;

        /// <inheritdoc />
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TVec Traverse(TNum t) => Source.Traverse(ScaleParameter(t));

        /// <inheritdoc />
        public TPose StartPose => Source.GetPose(StartParam);

        /// <inheritdoc />
        public TPose EndPose => Source.GetPose(EndParam);

        /// <inheritdoc />
        public bool IsClosed => !StartParam.IsApprox(EndParam) && Start.IsApprox(End);

        /// <inheritdoc />
        public PosePolyline<TPose, TVec, TNum> ToPosePolyline()
        {
            if (Source is IDiscretePoseCurve<TPose, TVec, TNum> poseCurve)
                return poseCurve.ToPosePolyline().Section(StartParam, EndParam);
            return ToPosePolyline(128);
        }

        /// <inheritdoc />
        public PosePolyline<TPose, TVec, TNum> ToPosePolyline(PolylineTessellationParameter<TNum> tessellationParameter)
            => Source is IDiscretePoseCurve<TPose, TVec, TNum> poseCurve
                ? poseCurve.ToPosePolyline(tessellationParameter).Section(StartParam, EndParam)
                : ThrowHelper.ThrowNotSupportedException<PosePolyline<TPose, TVec, TNum>>();

        /// <inheritdoc />
        public Polyline<TVec, TNum> ToPolyline()
        {
            const int steps = 64;
            const int n = steps + 1;
            return ToPolyline(n);
        }

        private Polyline<TVec, TNum> ToPolyline(int numPts)
        {
            var scalar = TNum.One / TNum.CreateTruncating(numPts);
            return Polyline<TVec, TNum>.CreateCulled(Enumerable.Range(0, numPts).Select(TNum.CreateTruncating)
                .Select(t => t * scalar)
                .Select(Traverse));
        }

        private PosePolyline<TPose, TVec, TNum> ToPosePolyline(int numPts)
        {
            var scalar = TNum.One / TNum.CreateTruncating(numPts);
            return PosePolyline<TPose, TVec, TNum>.CreateCulled(Enumerable.Range(0, numPts)
                .Select(TNum.CreateTruncating)
                .Select(t => t * scalar)
                .Select(GetPose));
        }

        /// <inheritdoc />
        public Polyline<TVec, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
            => Source is IDiscreteCurve<TVec, TNum> discrete
                ? discrete.ToPolyline(tessellationParameter).Section(StartParam, EndParam)
                : ThrowHelper.ThrowNotSupportedException<Polyline<TVec, TNum>>();

        /// <inheritdoc />
        public TVec GetTangent(TNum t)
            => Source.GetTangent(ScaleParameter(t));

        /// <inheritdoc />
        public TPose GetPose(TNum t)
            => Source.GetPose(ScaleParameter(t));

        /// <inheritdoc />
        public TVec EntryDirection => Source.GetTangent(StartParam);

        /// <inheritdoc />
        public TVec ExitDirection => Source.GetTangent(EndParam);
    }
}
