using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace MeshWiz.Collections;

public static class Edge
{
    public readonly record struct Unweighted(int Target) : IListGraphEdge<Unweighted, int>
    {
        public int Weight => 1;

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Unweighted Create<TNode>(ListGraph<TNode,Unweighted,int> context, int start, int end) where TNode : IEquatable<TNode> 
            => new(end);

        /// <inheritdoc />
        public static bool IsWeighted => false;

    }
}