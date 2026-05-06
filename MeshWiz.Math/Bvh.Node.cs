using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace MeshWiz.Math;

public static partial class Bvh
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Node<TVec, TNum> : IEquatable<Node<TVec, TNum>>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        [JsonInclude]
        public readonly AABB<TVec> Bounds;
        [JsonInclude]
        internal readonly int first, second;

        [SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
        [JsonIgnore]
        public int Start => first;

        [JsonIgnore]
        public int Length => -second;
        [JsonIgnore]
        public int FirstChild => first;
        [JsonIgnore]
        public int SecondChild => second;
        [JsonIgnore]
        public bool IsLeaf => second <= 0; //the zeroeth node may never be a parent
        [JsonIgnore]
        public bool IsParent => second > 0;

        [JsonConstructor]
        private Node(AABB<TVec> bounds, int first, int second)
        {
            Bounds = bounds;
            this.first = first;
            this.second = second;
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


        [JsonIgnore]
        public TNum LeafCost => Bounds.Size.SquaredLength * TNum.CreateTruncating(Length);
        [JsonIgnore]
        public int End => Start + Length;
        [JsonIgnore]
        public Range LeafRange => Start..End;

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node<TOtherVec, TOther> To<TOtherVec, TOther>()
            where TOtherVec : unmanaged, IVec<TOtherVec, TOther>
            where TOther : unmanaged, IFloatingPointIeee754<TOther>
            => new(Bounds.To<TOtherVec>(), first, second);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node<TVec, TNum> WithBounds(AABB<TVec> bbox)
        {
            var copy = this;
            Unsafe.AsRef(in copy.Bounds) = bbox;
            return copy;
        }

        /// <inheritdoc />
        public bool Equals(Node<TVec, TNum> other) =>
            Bounds.Equals(other.Bounds) && first == other.first && second == other.second;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Node<TVec, TNum> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Bounds, first, second);

        public static bool operator ==(Node<TVec, TNum> left, Node<TVec, TNum> right) => left.Equals(right);

        public static bool operator !=(Node<TVec, TNum> left, Node<TVec, TNum> right) => !left.Equals(right);

        public override string ToString() => IsLeaf
            ? $"LeafNode {{ {nameof(Bounds)} {Bounds}; {nameof(Start)} {Start}; {nameof(Length)}; {Length} }}"
            : $"ParentNode {{ {nameof(Bounds)} {Bounds}; {nameof(FirstChild)} {FirstChild}; {nameof(SecondChild)}; {SecondChild} }}";
    }
}