using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

public static partial class Bvh
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Node<TVec, TNum> : IEquatable<Node<TVec, TNum>>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public readonly AABB<TVec> Bounds;
        private readonly int _first;
        private readonly int _second;

        [SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
        public int Start => _first;

        public int Length => -_second;
        public int FirstChild => _first;
        public int SecondChild => _second;
        public bool IsLeaf => _second <= 0; //the zeroeth node may never be a parent
        public bool IsParent => _second > 0;

        private Node(AABB<TVec> bounds, int first, int second)
        {
            Bounds = bounds;
            _first = first;
            _second = second;
        }

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node<TVec, TNum> MakeLeaf(AABB<TVec> bounds, int start, int length)
            => new(bounds, start, -length);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Node<TVec, TNum> MakeParent(AABB<TVec> bounds, int left, int right)
            => new(bounds, left, right);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node<TVec, TNum> WithChildren(int firstChild, int secondChild)
            => MakeParent(Bounds, firstChild, secondChild);


        public TNum LeafCost => Bounds.Size.SquaredLength * TNum.CreateTruncating(Length);
        public int End => Start + Length;
        public Range LeafRange => Start..End;

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node<TOtherVec, TOther> To<TOtherVec, TOther>()
            where TOtherVec : unmanaged, IVec<TOtherVec, TOther>
            where TOther : unmanaged, IFloatingPointIeee754<TOther>
            => new(Bounds.To<TOtherVec>(), _first, _second);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node<TVec, TNum> WithBounds(AABB<TVec> bbox)
        {
            var copy = this;
            Unsafe.AsRef(in copy.Bounds) = bbox;
            return copy;
        }

        /// <inheritdoc />
        public bool Equals(Node<TVec, TNum> other) =>
            Bounds.Equals(other.Bounds) && _first == other._first && _second == other._second;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Node<TVec, TNum> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Bounds, _first, _second);

        public static bool operator ==(Node<TVec, TNum> left, Node<TVec, TNum> right) => left.Equals(right);

        public static bool operator !=(Node<TVec, TNum> left, Node<TVec, TNum> right) => !left.Equals(right);

        public override string ToString() => IsLeaf
            ? $"LeafNode {{ {nameof(Bounds)} {Bounds}; {nameof(Start)} {Start}; {nameof(Length)}; {Length} }}"
            : $"ParentNode {{ {nameof(Bounds)} {Bounds}; {nameof(FirstChild)} {FirstChild}; {nameof(SecondChild)}; {SecondChild} }}";
    }
}