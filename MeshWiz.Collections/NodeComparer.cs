using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace MeshWiz.Collections;

public sealed class NodeComparer<TNode> : IEqualityComparer<TNode>
    where TNode : IEquatable<TNode>
{
    public static NodeComparer<TNode> Create() => new();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining),Pure]
    public bool Equals(TNode? x, TNode? y) => x is null && y is null || x is not null && y is not null && x.Equals(y);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining),Pure]
    public int GetHashCode(TNode obj)
        => obj.GetHashCode();
}