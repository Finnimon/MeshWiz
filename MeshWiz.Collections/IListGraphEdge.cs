using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Collections;

public interface IListGraphEdge<TSelf, TNum>
    where TSelf : struct, IListGraphEdge<TSelf, TNum>
    where TNum : INumber<TNum>
{
    int Target { get; }
    TNum Weight => TNum.One;
    static virtual bool IsWeighted => true;

    [Pure]
    static abstract TSelf Create<TNode>(ListGraph<TNode,TSelf,TNum> context,int start, int end) where TNode : IEquatable<TNode>;
}