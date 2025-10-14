using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public static partial class Solver
{
    public static class Linear
    {
        [Pure]
        public static bool TrySolveForZero<TNum>(TNum a, TNum b,
        [NotNullWhen(returnValue: true)] out TNum? result)
        where TNum : INumber<TNum>
        =>TrySolve(a,b,TNum.Zero,out result);
        [Pure]
        public static bool TrySolve<TNum>(TNum a, TNum b, TNum target,
            [NotNullWhen(returnValue: true)] out TNum? result)
            where TNum : INumber<TNum>
        {
            result = default;
            if (a == target)
            {
                result = TNum.Zero;
                return true;
            }
            if (b == target)
            {
                result = TNum.One;
                return true;
            }
            if (a == b) return false;
            var aDiffT = target-a;
            var range = TNum.Abs(a - b);
            result = aDiffT / range;
            return true;
        }
    }
}