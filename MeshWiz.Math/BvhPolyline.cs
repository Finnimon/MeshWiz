using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class BvhPolyline
{
    public readonly ref struct AnyHit<TNum> : Bvh.ITraverser<Line<Vec2<TNum>, TNum>, TNum, Vec2<TNum>, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        private readonly Ray2<TNum> _ray;
        private readonly ref TNum _hit;

        public AnyHit(Ray2<TNum> ray, ref TNum hit)
        {
            _ray = ray;
            _hit = ref hit;
            _hit = TNum.PositiveInfinity;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public bool Intersect(Line<Vec2<TNum>, TNum> test, out TNum result) =>
            _ray.Intersect(test, out result);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public bool DoIntersect(AABB<Vec2<TNum>> t) => _ray.DoIntersect(t);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bvh.HitReact AcceptHit(int index, Line<Vec2<TNum>, TNum> element, TNum hit)
        {
            _hit = hit;
            return Bvh.HitReact.BreakCompletely;
        }
    }


    public readonly ref struct AllHits<TCol, TNum> : Bvh.ITraverser<Line<Vec2<TNum>, TNum>, TNum, Vec2<TNum>, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TCol : ICollection<TNum>, allows ref struct
    {
        private readonly Ray2<TNum> _ray;
        public readonly TCol Hits;

        public AllHits(Ray2<TNum> ray, TCol buffer)
        {
            _ray = ray;
            Hits = buffer;
            Debug.Assert(!Hits.IsReadOnly);
        }


        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public bool Intersect(Line<Vec2<TNum>, TNum> test, out TNum result) =>
            _ray.Intersect(test, out result);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public bool DoIntersect(AABB<Vec2<TNum>> t) => _ray.DoIntersect(t);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bvh.HitReact AcceptHit(int index, Line<Vec2<TNum>, TNum> element, TNum hit)
        {
            Hits.Add(hit);
            return Bvh.HitReact.ContinueCurrentNode;
        }
    }

    public readonly ref struct ClosestHit<TNum> : Bvh.ITraverser<Line<Vec2<TNum>, TNum>, TNum, Vec2<TNum>, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        private readonly Ray2<TNum> _ray;
        private readonly ref TNum _hit;
        private readonly ref int _hitTarget;

        public ClosestHit(Ray2<TNum> ray, ref TNum hit, ref int hitTarget)
        {
            _ray = ray;
            _hit = ref hit;
            _hit = TNum.PositiveInfinity;
            _hitTarget = ref hitTarget;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public bool Intersect(Line<Vec2<TNum>, TNum> test, out TNum result) =>
            _ray.Intersect(test, out result) && result < _hit;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public bool DoIntersect(AABB<Vec2<TNum>> t) =>
            _ray.Intersect(t, out var hit) && hit < _hit; //can not be closer then

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bvh.HitReact AcceptHit(int index, Line<Vec2<TNum>, TNum> element, TNum hit)
        {
            _hit = hit;
            _hitTarget = index;
            return Bvh.HitReact.ContinueCurrentNode;
        }
    }

    public readonly ref struct ContainsPoint<TNum> : Bvh.ITraverser<Line<Vec2<TNum>, TNum>, TNum, Vec2<TNum>, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        private readonly Ray2<TNum> _ray;
        private readonly ref TNum _hit;
        private readonly ref int _hitTarget;

        public ContainsPoint(Vec2<TNum> p, ref TNum hit, ref int hitTarget)
        {
            _ray = Ray2<TNum>.CreateUnsafe(p, Vec2<TNum>.UnitX);
            _hit = ref hit;
            _hit = TNum.PositiveInfinity;
            _hitTarget = ref hitTarget;
            _hitTarget = -1;
        }

        public ContainsPoint(Ray2<TNum> ray, ref TNum hit, ref int hitTarget)
        {
            _ray = ray;
            _hit = ref hit;
            _hit = TNum.PositiveInfinity;
            _hitTarget = ref hitTarget;
            _hitTarget = -1;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public bool Intersect(Line<Vec2<TNum>, TNum> test, out TNum result) =>
            _ray.Intersect(test, out result) && result < _hit;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public bool DoIntersect(AABB<Vec2<TNum>> t) =>
            _ray.Intersect(t, out var hit) && hit < _hit; //can not be closer then

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bvh.HitReact AcceptHit(int index, Line<Vec2<TNum>, TNum> element, TNum hit)
        {
            _hit = hit;
            _hitTarget = index;
            return hit.IsApproxZero() ? Bvh.HitReact.BreakCompletely : Bvh.HitReact.ContinueCurrentNode;
        }

        public IntersectionLevel FinalEval<TList>(TList l, WindingOrder windingOrder)
            where TList : IReadOnlyList<Line<Vec2<TNum>, TNum>>
        {
            if (_hitTarget == -1) return IntersectionLevel.None;
            if (_hit.IsApproxZero()) return IntersectionLevel.Intersects;

            var intersected = l[_hitTarget];
            var outwards = intersected.AxisVector.Right();
            var dotSignHit = windingOrder switch
            {
                WindingOrder.Clockwise => -1,
                WindingOrder.CounterClockwise => 1,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<int>(nameof(windingOrder))
            };
            var dotSign = TNum.Sign(Vec2<TNum>.Dot(_ray.Direction, outwards));

            var mayContain = dotSign == dotSignHit;
            if (!mayContain)
                return IntersectionLevel.None;
            var intersectionPt = _ray.Traverse(_hit);
            int otherLineIndex;
            var parameter = intersected.Start.DistanceTo(intersectionPt) / intersected.Length;
            if (parameter.IsApproxZero())
                otherLineIndex = -1;
            else if (parameter.IsApprox(TNum.One))
                otherLineIndex = +1;
            else
                return IntersectionLevel.Contains;

            otherLineIndex = (_hitTarget + otherLineIndex).WrapZeroBound(l.Count);
            var otherLine = l[otherLineIndex];
            var isHit = TNum.Sign(Vec2<TNum>.Dot(otherLine.AxisVector.Right(), _ray.Direction)) == dotSignHit;
            return isHit ? IntersectionLevel.Contains : IntersectionLevel.None;
        }
    }
}