using System.Formats.Asn1;
using System.Numerics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math.Signals;

public static partial class Signal
{
    public static class Analysis
    {
        public static SignalDataPoint<TIn, TOut>[] Sweep<TIn, TOut>(ISignal<TIn, TOut> signal, IEnumerable<TIn> sweep)
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut> =>
            sweep.Select(input => SignalDataPoint<TIn, TOut>.Create(signal, input)).ToArray();

        public static SignalDataPoint<TIn, TOut>[] SweepParallel<TIn, TOut>(ISignal<TIn, TOut> signal,
            IReadOnlyList<TIn> sweep) where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var result = new SignalDataPoint<TIn, TOut>[sweep.Count];
            Parallel.For(0, result.Length, i => result[i] = SignalDataPoint<TIn, TOut>.Create(signal, sweep[i]));
            return result;
        }

        public static SignalDataPoint<TIn, TOut>[] SweepParallel<TIn, TOut>(ISignal<TIn, TOut> signal,
            AABB<TIn> sweepRange,
            int stepCount) where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var totalMeasurements = stepCount + 1;
            var totalNum = TIn.CreateTruncating(totalMeasurements);
            var result = new SignalDataPoint<TIn, TOut>[totalMeasurements];
            Parallel.For(0, totalMeasurements,
                i => result[i] =
                    SignalDataPoint<TIn, TOut>.Create(signal, sweepRange.Lerp(TIn.CreateTruncating(i) / totalNum)));
            return result;
        }

        public static SignalDataPoint<TIn, TOut>[] Sweep<TIn, TOut>(ISignal<TIn, TOut> signal, AABB<TIn> sweepRange,
            int stepCount) where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var steps = GetSweepSteps(sweepRange, stepCount);
            return Sweep<TIn, TOut>(signal, steps);
        }

        private static IEnumerable<TIn> GetSweepSteps<TIn>(AABB<TIn> sweepRange, int stepCount)
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
        {
            var step = sweepRange.Size / TIn.CreateTruncating(stepCount);
            var max = Numbers<TIn>.Half * step + sweepRange.Max; //half step to avoid floating point max being culled
            var min = sweepRange.Min;
            var steps = Enumerable.Sequence(min, max, step);
            return steps;
        }

        public static SignalDataPoint<TIn, TOut> BestFitBinary<TIn, TOut>(ISignal<TIn, TOut> signal, TOut target,
            AABB<TIn> searchRange, TIn minRangeSize = default) where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var searchMin = SignalDataPoint<TIn, TOut>.Create(signal, searchRange.Min);
            var searchMid = SignalDataPoint<TIn, TOut>.Create(signal, searchRange.Center);
            var searchMax = SignalDataPoint<TIn, TOut>.Create(signal, searchRange.Max);
            var half = Numbers<TIn>.Half;
            if (minRangeSize == default)
                minRangeSize = TIn.Epsilon;
            while (searchRange.Size > minRangeSize)
            {
                if ((searchMid.OutPut - target).IsApproxZero())
                    return searchMid;
                var lowerRange = AABB.From(searchMin.OutPut, searchMid.OutPut);
                var upperRange = AABB.From(searchMid.OutPut, searchMax.OutPut);
                var lowDist = lowerRange.DistanceTo(target);
                var upperDist = upperRange.DistanceTo(target);

                var lowerCloser = lowDist < upperDist;
                if (lowDist.IsApprox(upperDist))
                    lowerCloser = TOut.Abs(lowerRange.Center - target) < TOut.Abs(upperRange.Center - target);

                if (lowerCloser)
                    searchMax = searchMid;
                else
                    searchMin = searchMid;

                searchMid = SignalDataPoint<TIn, TOut>.Create(signal, TIn.Lerp(searchMin.Input, searchMax.Input, half));
                searchRange = AABB.From(searchMid.Input, searchMax.Input);
            }

            SignalDataPoint<TIn, TOut>[] res = [searchMid, searchMin, searchMax];

            return res.OrderBy(v => TOut.Abs(v.OutPut - target)).First();
        }
        
        
        public static SignalDataPoint<TIn, TOut> BestFitNewton<TIn, TOut>(
            ISignal<TIn, TOut> signal,
            AABB<TIn> searchRange,
            int maxIterations = 32,
            TOut tolerance = default) where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            if (tolerance == default)
                tolerance = Numbers<TOut>.ZeroEpsilon;
            var epsilonIn = Numbers<TIn>.ZeroEpsilon;

            var x = searchRange.Center;

            var best = SignalDataPoint<TIn, TOut>.Create(signal, x);
            var bestError = TOut.Abs(best.OutPut);

            for (var i = 0; i < maxIterations; i++)
            {
                var fx = best.OutPut;

                if (TOut.Abs(fx) <= tolerance)
                    break;

                var x0 = searchRange.Clamp(x - epsilonIn);
                var x1 = searchRange.Clamp(x + epsilonIn);

                var f0 = signal.Sample(x0);
                var f1 = signal.Sample(x1);

                var dfdx = (f1 - f0) / TOut.CreateTruncating(x1 - x0);

                if (dfdx.IsApproxZero())
                    break;

                var nextX = searchRange.Clamp(
                    x - TIn.CreateTruncating(fx / dfdx));

                if (nextX == x)
                    break;

                var next = SignalDataPoint<TIn, TOut>.Create(signal, nextX);
                var error = TOut.Abs(next.OutPut);

                if (error < bestError)
                {
                    best = next;
                    bestError = error;
                }

                x = nextX;
            }


            var min = SignalDataPoint<TIn, TOut>.Create(signal, searchRange.Min);
            var max = SignalDataPoint<TIn, TOut>.Create(signal, searchRange.Max);

            SignalDataPoint<TIn, TOut>[] res = [min, best, max];
            return SignalDataPoint<TIn, TOut>.Closest(TOut.Zero, best, min, max);
        }

        
        public static SignalDataPoint<TIn, TOut> BestFitNewton<TIn, TOut>(
            ISignal<TIn, TOut> signal,
            TOut target,
            AABB<TIn> searchRange,
            int maxIterations = 32,
            TOut tolerance = default) where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        =>BestFitNewton(signal.Shift(-target),searchRange,maxIterations,tolerance);


        public static int[] BestFitRanges<TIn, TOut>(SignalDataPoint<TIn, TOut>[] sweep, TOut target,
            TOut epsilon = default)
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            if (epsilon == default)
                epsilon = Numbers<TOut>.ZeroEpsilon;
            List<int> possible = new(sweep.Length - 1);
            for (var i = 0; i < sweep.Length - 1; i++)
            {
                var l = sweep[i];
                var r = sweep[i + 1];

                var box = AABB.From(l.OutPut, r.OutPut);
                if (box.DistanceTo(target) > epsilon)
                    continue;
                possible.Add(i);
            }

            return possible.ToArray();
        }

        public static SignalDataPoint<TIn, TOut> BestFitSweepAdaptive<TIn, TOut>(
            ISignal<TIn, TOut> signal,
            TOut target,
            TIn initialGuess,
            AABB<TIn> searchRange,
            TIn initialStep,
            int maxSteps = 64,
            TOut tolerance = default)
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            if (tolerance == default)
                tolerance = Numbers<TOut>.ZeroEpsilon;

            var best = SignalDataPoint<TIn, TOut>.Create(signal, searchRange.Clamp(initialGuess));
            var bestError = TOut.Abs(best.OutPut - target);

            var step = initialStep;
            var epsIn = Numbers<TIn>.ZeroEpsilon;

            var improvedLast = true;

            for (var i = 0; i < maxSteps; i++)
            {
                if (!improvedLast)
                    step *= Numbers<TIn>.Two; // accelerate escape

                improvedLast = false;

                for (var dirI = -1; dirI <= 1; dirI += 2)
                {
                    var dir = dirI == -1 ? -TIn.One : TIn.One;
                    var nextX = searchRange.Clamp(best.Input + step * dir);
                    if (nextX == best.Input)
                        continue;

                    var next = SignalDataPoint<TIn, TOut>.Create(signal, nextX);
                    var error = TOut.Abs(next.OutPut - target);

                    if (error <= tolerance)
                        return next;

                    var x0 = searchRange.Clamp(nextX - epsIn);
                    var x1 = searchRange.Clamp(nextX + epsIn);
                    var f0 = signal.Sample(x0) - target;
                    var f1 = signal.Sample(x1) - target;
                    var dfdx = (f1 - f0) / TOut.CreateTruncating(x1 - x0);

                    var prevSign = TOut.Sign(best.OutPut - target);
                    var nextSign = TOut.Sign(next.OutPut - target);
                    if (prevSign != nextSign)
                    {
                        var range = AABB.From(best.Input, nextX);
                        var bin = BestFitBinary(signal, target, range);
                        return SignalDataPoint<TIn, TOut>.Closest(target, best, bin);
                    }

                    if (!dfdx.IsApproxZero() && TOut.Abs(dfdx) > tolerance)
                    {
                        var localRange = AABB.Around(nextX, step * Numbers<TIn>.Two);
                        localRange = searchRange.Clamp(localRange);

                        var newton = BestFitNewton(signal, target, localRange, 16, tolerance);

                        if (newton.IsAcceptable(target, tolerance))
                            return newton;
                    }

                    if (error >= bestError) continue;
                    best = next;
                    bestError = error;
                    improvedLast = true;
                }

                if (!improvedLast)
                    step *= Numbers<TIn>.Two;

                if (step > searchRange.Size)
                    break;
            }

            return best;
        }
    }
}