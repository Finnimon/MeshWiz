using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Collections;

namespace MeshWiz.Math;

public sealed class BvhPolyline<TVec, TNum>
    : IPolyline<BvhPolyline<TVec, TNum>, Line<TVec, TNum>, TVec, TVec, TNum>,
        IReadOnlyList<Line<TVec, TNum>>, Bvh.IHierarchy<Line<TVec, TNum>, TVec, TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Polyline<TVec, TNum> Underlying;
    private readonly Bvh.Node<TVec, TNum>[] _nodes;
    IReadOnlyList<Line<TVec, TNum>> Bvh.IHierarchy<Line<TVec, TNum>, TVec, TNum>.Elements => Underlying;
    public IReadOnlyList<Bvh.Node<TVec, TNum>> Nodes => _nodes;
    public int Depth { get; }


    private BvhPolyline(Polyline<TVec, TNum> underlying, Bvh.Node<TVec, TNum>[] nodes, int depth)
    {
        _nodes = nodes;
        Underlying = underlying;
        Depth = depth;
    }

    public BvhPolyline(Polyline<TVec, TNum> underlying, IEnumerable<Bvh.Node<TVec, TNum>> nodes, int depth) : this(
        underlying,
        nodes.ToArray(), depth) { }

    public static BvhPolyline<TVec, TNum> Sah(Polyline<TVec, TNum> underlying, int maxDepth=32, int splitTests=4)
    {
        if (underlying.Count == 0)
            return new BvhPolyline<TVec, TNum>(underlying, [], 0);
        var info = Bvh.Create.SahNonReordering<Line<TVec, TNum>, TVec, TNum>(underlying, maxDepth, splitTests);
        Debug.Assert(info.IndexShuffle is null);//index shuffle break compatibility
        return new BvhPolyline<TVec, TNum>(underlying, info.Nodes, info.Depth);
    }
    public static BvhPolyline<TVec, TNum> BinaryBalanced(Polyline<TVec, TNum> underlying, int maxDepth=32)
    {
        if (underlying.Count == 0)
            return new BvhPolyline<TVec, TNum>(underlying, [], 0);
        var info = Bvh.Create.BinaryBalancedNonReordering<Line<TVec, TNum>, TVec, TNum>(underlying, maxDepth);
        Debug.Assert(info.IndexShuffle is null);//index shuffle break compatibility
        return new BvhPolyline<TVec, TNum>(underlying, info.Nodes, info.Depth);
    }
    

    /// <inheritdoc />
    public TVec Traverse(TNum t) => Underlying.Traverse(t);

    /// <inheritdoc />
    public TVec GetTangent(TNum t) => Underlying.GetTangent(t);

    /// <inheritdoc />
    public TVec Start => Underlying.Start;

    /// <inheritdoc />
    public TVec End => Underlying.End;

    /// <inheritdoc />
    public TVec TraverseOnCurve(TNum t) => Underlying.TraverseOnCurve(t);

    /// <inheritdoc />
    public TNum Length => Underlying.Length;

    /// <inheritdoc />
    public Polyline<TVec, TNum> ToPolyline() => Underlying;

    /// <inheritdoc />
    public Polyline<TVec, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter) =>
        Underlying.ToPolyline(tessellationParameter);

    /// <inheritdoc />
    public TVec EntryDirection => Underlying.EntryDirection;

    /// <inheritdoc />
    public TVec ExitDirection => Underlying.ExitDirection;

    /// <inheritdoc />
    public bool Contains(Line<TVec, TNum> item) => Underlying.Contains(item);

    /// <inheritdoc />
    public void CopyTo(Line<TVec, TNum>[] array, int arrayIndex) => Underlying.CopyTo(array, arrayIndex);

    public int Count => Underlying.Count;

    /// <inheritdoc />
    public int IndexOf(Line<TVec, TNum> item) => Underlying.IndexOf(item);

    /// <inheritdoc />
    int IVersionedList<Line<TVec, TNum>>.Version => 0;

    /// <inheritdoc />
    public AABB<TVec> BBox => Underlying.BBox;

    /// <inheritdoc />
    public IReadOnlyList<TVec> Vertices => Underlying.Vertices;

    /// <inheritdoc />
    public IReadOnlyList<TNum> CumulativeLengths => Underlying.CumulativeLengths;

    /// <inheritdoc />
    public static BvhPolyline<TVec, TNum> CreateNonCopying(TVec[] vertices)
    {
        var rootNode = Bvh.Node<TVec, TNum>.MakeLeaf(AABB.From(vertices), 0, vertices.Length - 1);
        var poly = Polyline<TVec, TNum>.CreateNonCopying(vertices);
        poly._bbox = rootNode.Bounds;
        return new BvhPolyline<TVec, TNum>(poly, [rootNode], 1);
    }

    public Line<TVec, TNum> this[int index] => Underlying[index];
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TraverseBvh<TTraverser, TIntersection>(TTraverser traverser) 
        where TTraverser : Bvh.ITraverser<Line<TVec,TNum>, TIntersection, TVec, TNum>, allows ref struct
        => Bvh.Traverse<TTraverser, Line<TVec,TNum>, TIntersection, TVec, TNum>(this, traverser);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TraverseBvh<TIntersection>(
        Func<AABB<TVec>, bool> bBoxDoIntersect,
        Func<Line<TVec,TNum>, (TIntersection, bool)> elementIntersect,
        Func<int, Line<TVec,TNum>, TIntersection, Bvh.HitReact> acceptHitReact) 
        => Bvh.Traverse(this, bBoxDoIntersect, elementIntersect, acceptHitReact);

}