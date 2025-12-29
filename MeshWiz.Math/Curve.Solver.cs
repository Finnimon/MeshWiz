using System.Numerics;
using MeshWiz.Math.Signals;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Curve
{
    public static class Solver
    {
        public static Result<Arithmetics, TNum> IntersectionNewton<TCurve, TNum>(
            TCurve curve,
            Plane3<TNum> plane,
            AABB<TNum> search = default)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TCurve : ICurve<Vector3<TNum>, TNum> 
            => Intersection(curve, 
                plane, 
                (sig, searchRange) => Signal.Analysis.BestFitNewton(sig, searchRange));

        public static Result<Arithmetics, TNum> Intersection<TCurve, TNum>(
            TCurve curve,
            Plane3<TNum> plane,
            Func<ISignal<TNum, TNum>, AABB<TNum>, SignalDataPoint<TNum, TNum>> engine,
            AABB<TNum> search = default)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TCurve : ICurve<Vector3<TNum>, TNum>
        {
            FSignal<TNum, TNum> sig = new(t => plane.SignedDistance(curve.Traverse(t)));
            if (search == default)
                search = AABB<TNum>.Saturate;
            var intersect = engine(sig, search);
            
            return Result<Arithmetics, TNum>.Success(intersect.Input)
                .When(intersect.OutPut.IsApproxZero());
        }

        public static Result<Arithmetics, TNum> IntersectionBinary<TCurve, TNum>(TCurve curve, Plane3<TNum> plane,
            AABB<TNum> search = default)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TCurve : ICurve<Vector3<TNum>, TNum>
            => Intersection(curve, 
                plane,
                (sig, searchRange) => Signal.Analysis.BestFitBinary(sig, TNum.Zero, searchRange));

    }
}