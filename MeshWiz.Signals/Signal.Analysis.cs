using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Numerics;
using System.Text;
using MeshWiz.Math;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Signals;

public static partial class Signal
{
    public static class Analysis
    {
        public static SignalResult<TIn, TOut>[] Sweep<TSignal, TIn, TOut>(TSignal signal, IEnumerable<TIn> sweep)
            where TSignal : ISignal<TIn, TOut>
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut> =>
            sweep.Select(input => SignalResult<TIn, TOut>.Create(signal, input)).ToArray();

        public static SignalResult<TIn, TOut>[] SweepParallel<TSignal, TIn, TOut>(TSignal signal,
            IReadOnlyList<TIn> sweep)
            where TSignal : ISignal<TIn, TOut>
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var result = new SignalResult<TIn, TOut>[sweep.Count];
            Parallel.For(0, result.Length, i => result[i] = SignalResult<TIn, TOut>.Create(signal, sweep[i]));
            return result;
        }

        public static SignalResult<TIn, TOut>[] SweepParallel<TSignal, TIn, TOut>(TSignal signal,
            AABB<TIn> sweepRange,
            int stepCount)
            where TSignal : ISignal<TIn, TOut>
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var totalMeasurements = stepCount + 1;
            var totalNum = TIn.CreateTruncating(totalMeasurements);
            var result = new SignalResult<TIn, TOut>[totalMeasurements];
            Parallel.For(0, totalMeasurements,
                i => result[i] =
                    SignalResult<TIn, TOut>.Create(signal, sweepRange.Lerp(TIn.CreateTruncating(i) / totalNum)));
            return result;
        }

        public static SignalResult<TIn, TOut>[] Sweep<TSignal, TIn, TOut>(TSignal signal, AABB<TIn> sweepRange,
            int stepCount)
            where TSignal : ISignal<TIn, TOut>
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var steps = GetSweepSteps(sweepRange, stepCount);
            return Sweep<TSignal, TIn, TOut>(signal, steps);
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

        public static SignalResult<TIn, TOut> BestFitBinary<TSignal, TIn, TOut>(TSignal signal, TOut target,
            AABB<TIn> searchRange, TIn minRangeSize = default)
            where TSignal : ISignal<TIn, TOut>
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var searchMin = SignalResult<TIn, TOut>.Create(signal, searchRange.Min);
            var searchMid = SignalResult<TIn, TOut>.Create(signal, searchRange.Center);
            var searchMax = SignalResult<TIn, TOut>.Create(signal, searchRange.Max);
            var half = Numbers<TIn>.Half;
            if (minRangeSize == default)
                minRangeSize = TIn.Epsilon;
            while (searchRange.Size > minRangeSize)
            {
                if ((searchMid.Result - target).IsApproxZero())
                    return searchMid;
                var lowerRange = AABB.From(searchMin.Result, searchMid.Result);
                var upperRange = AABB.From(searchMid.Result, searchMax.Result);
                var lowDist = lowerRange.DistanceTo(target);
                var upperDist = upperRange.DistanceTo(target);

                var lowerCloser = lowDist < upperDist;
                if (lowDist.IsApprox(upperDist))
                    lowerCloser = TOut.Abs(lowerRange.Center - target) < TOut.Abs(upperRange.Center - target);

                if (lowerCloser)
                    searchMax = searchMid;
                else
                    searchMin = searchMid;

                searchMid = SignalResult<TIn, TOut>.Create(signal, TIn.Lerp(searchMin.Input, searchMax.Input, half));
                searchRange = AABB.From(searchMid.Input, searchMax.Input);
            }

            SignalResult<TIn, TOut>[] res = [searchMid, searchMin, searchMax];

            return res.OrderBy(v => TOut.Abs(v.Result - target)).First();
        }

        public static SignalResult<TIn, TOut> BestFitNewton<TIn, TOut>(
            ISignal<TIn, TOut> signal,
            TOut target,
            AABB<TIn> searchRange,
            int maxIterations = 32,
            TOut tolerance = default) where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var shifted = signal.Shift(-target);

            if (tolerance == default)
                tolerance = Numbers<TOut>.ZeroEpsilon;
            var epsilonIn = Numbers<TIn>.ZeroEpsilon;

            var x = searchRange.Center;

            var best = SignalResult<TIn, TOut>.Create(shifted, x);
            var bestError = TOut.Abs(best.Result);

            for (var i = 0; i < maxIterations; i++)
            {
                var fx = best.Result;

                if (TOut.Abs(fx) <= tolerance)
                    break;

                var x0 = searchRange.Clamp(x - epsilonIn);
                var x1 = searchRange.Clamp(x + epsilonIn);

                var f0 = shifted.Sample(x0);
                var f1 = shifted.Sample(x1);

                var dfdx = (f1 - f0) / TOut.CreateTruncating(x1 - x0);

                if (dfdx.IsApproxZero())
                    break;

                var nextX = searchRange.Clamp(
                    x - TIn.CreateTruncating(fx / dfdx));

                if (nextX == x)
                    break;

                var next = SignalResult<TIn, TOut>.Create(shifted, nextX);
                var error = TOut.Abs(next.Result);

                if (error < bestError)
                {
                    best = next;
                    bestError = error;
                }

                x = nextX;
            }

            var final = best.Shift(target);

            var min = SignalResult<TIn, TOut>.Create(signal, searchRange.Min);
            var max = SignalResult<TIn, TOut>.Create(signal, searchRange.Max);

            SignalResult<TIn, TOut>[] res = [min, final, max];
            return res.OrderBy(v => TOut.Abs(v.Result - target)).First();
        }

        public static SignalResult<TIn, TOut> BestFitNewtonRetrying<TIn, TOut>(
            ISignal<TIn, TOut> signal,
            TOut target,
            AABB<TIn> initialSearchRange,
            AABB<TIn> maxSearchRange,
            int maxTries = 4,
            int maxIterationsPerTry = 32,
            TOut tolerance = default)
            where TIn : unmanaged, IFloatingPointIeee754<TIn>
            where TOut : unmanaged, IFloatingPointIeee754<TOut>
        {
            var activeSearch = initialSearchRange;
            var best = signal.GetResult(initialSearchRange.Center);
            while (maxTries-- > 0)
            {
                var curTry = BestFitNewton(signal, target, activeSearch, maxIterationsPerTry, tolerance);
                var newBest = SignalResult<TIn, TOut>.Closest(target, best, curTry);
                if (newBest == best)
                    return newBest;
                best = newBest;
                var success = best.Result.IsApprox(target);
                if (success)
                    return best;
                var directionUnknown = best.Input.IsApprox(activeSearch.Center);
                if (directionUnknown)
                    return best;
                activeSearch = AABB.Around(curTry.Input, activeSearch.Size);
                activeSearch = maxSearchRange.Clamp(activeSearch);
            }

            var finalized = BestFitNewton(signal, target,
                AABB<TIn>.Around(best.Input, initialSearchRange.Size * Numbers<TIn>.Eps3),
                maxIterationsPerTry, tolerance);
            return finalized;
        }

        public static int[] BestFitRanges<TIn, TOut>(SignalResult<TIn, TOut>[] sweep, TOut target,
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

                var box = AABB.From(l.Result, r.Result);
                if (box.DistanceTo(target) > epsilon)
                    continue;
                possible.Add(i);
            }

            return possible.ToArray();
        }

        public static SignalResult<TIn, TOut> BestFitSweepAdaptive<TIn, TOut>(
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

            var best = SignalResult<TIn, TOut>.Create(signal, searchRange.Clamp(initialGuess));
            var bestError = TOut.Abs(best.Result - target);

            var step = initialStep;
            var epsIn = Numbers<TIn>.ZeroEpsilon;

            var improvedLast = true;

            for (var i = 0; i < maxSteps; i++)
            {
                if (!improvedLast)
                    step *= Numbers<TIn>.Two; // accelerate escape
                
                improvedLast = false;

                foreach (var dir in new[] { -TIn.One, TIn.One })
                {
                    var nextX = searchRange.Clamp(best.Input + step * dir);
                    if (nextX == best.Input)
                        continue;

                    var next = SignalResult<TIn, TOut>.Create(signal, nextX);
                    var error = TOut.Abs(next.Result - target);

                    if (error <= tolerance)
                        return next;

                    var x0 = searchRange.Clamp(nextX - epsIn);
                    var x1 = searchRange.Clamp(nextX + epsIn);
                    var f0 = signal.Sample(x0) - target;
                    var f1 = signal.Sample(x1) - target;
                    var dfdx = (f1 - f0) / TOut.CreateTruncating(x1 - x0);

                    var prevSign = TOut.Sign(best.Result - target);
                    var nextSign = TOut.Sign(next.Result - target);
                    if (prevSign != nextSign)
                    {
                        var range = AABB.From(best.Input, nextX);
                        var bin = BestFitBinary(signal, target, range);
                        return SignalResult<TIn, TOut>.Closest(target, best, bin);
                    }

                    if (!dfdx.IsApproxZero() && TOut.Abs(dfdx) > tolerance)
                    {
                        var localRange = AABB.Around(nextX, step * Numbers<TIn>.Two);
                        localRange = searchRange.Clamp(localRange);

                        var newton = BestFitNewton(signal, target, localRange, 16, tolerance);
                        
                        if (newton.IsAcceptable(target,tolerance))
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