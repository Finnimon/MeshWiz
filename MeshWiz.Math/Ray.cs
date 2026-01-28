using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Ray<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
{
    public readonly TVec Origin;
    public readonly TVec Direction;

    public Ray(TVec origin, TVec direction)
    {
        Origin = origin;
        Direction = direction;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVec ClosestPoint(TVec p)
    {
        var v = p - Origin;
        var dir = Direction;
        var dotProduct = v.Dot(dir);
        var alongVector = dotProduct * dir;
        return Origin + alongVector;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(TVec p) => ClosestPoint(p).DistanceTo(p);


    public bool Intersect(AABB<TVec> test, out TNum result)
    {
        Unsafe.SkipInit(out result);
        var tMin = TNum.NegativeInfinity;
        var tMax = TNum.PositiveInfinity;
        for (var axis = 0; axis < TVec.Dimensions; axis++)
        {
            var d = Direction[axis];
            var origin = Origin[axis];
            var min = test.Min[axis];
            var max = test.Max[axis];
            var axisZero = d.IsApproxZero();
            if (axisZero && origin < min || origin > max) return false;
            if (axisZero) continue;
            var invD = TNum.One / d;
            var tx1 = (min - origin) * invD;
            var tx2 = (max - origin) * invD;

            if (tx1 > tx2) (tx1, tx2) = (tx2, tx1);

            tMin = TNum.Max(tMin, tx1);
            tMax = TNum.Min(tMax, tx2);
        }

        if (tMax < TNum.Max(tMin, TNum.Zero))
            return false;

        result = tMin.IsApproxGreaterOrEqual(TNum.Zero) ? tMin : tMax; // handles ray starting inside box
        return true;
    }
}