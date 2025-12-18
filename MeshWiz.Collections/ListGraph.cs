using System.Numerics;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Collections;

public sealed class ListGraph<TNode, TEdge, TWeight>
    where TNode : IEquatable<TNode>
    where TEdge : struct, IListGraphEdge<TEdge, TWeight>
    where TWeight : INumber<TWeight>
{
    public readonly bool Directed;
    public bool Undirected => !Directed;
    public int Count => _nodes.Count;
    private readonly HashSet<TNode> _nodeCuller;
    private readonly List<TNode> _nodes;
    private readonly List<List<TEdge>> _edges;

    public ListGraph(int initialCapacity, bool directed)
    {
        _nodes = new List<TNode>(initialCapacity);
        _edges = new List<List<TEdge>>(initialCapacity);
        _nodeCuller = new HashSet<TNode>(initialCapacity, NodeComparer<TNode>.Create());
        Directed = directed;
    }

    public int Add(TNode node)
    {
        if (!_nodeCuller.Add(node))
            return _nodes.IndexOf(node);
        var index = _nodes.Count;
        _nodes.Add(node);
        _edges.Add([]);
        return index;
    }

    public void AddEdges(int target, params ReadOnlySpan<int> edges)
    {
        if (Count <= (uint)target)
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(target));
        var targetNode = _nodes[target];
        var existing = _edges[target];
        var directed = Directed;
        foreach (var edge in edges)
        {
            if (Count <= (uint)edge)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(edges));
            if (target == edge)
                continue;
            var targetEdge = _nodes[edge];
            var newEdge = TEdge.Create(this,target, edge);
            var old = existing.FindIndex(e => e.Target == edge);
            TEdge reverseEdge;
            if (old is -1)
            {
                existing.Add(newEdge);
                if (directed) continue;
                reverseEdge = TEdge.Create(this, edge, target);
                _edges[edge].Add(reverseEdge);
                continue;
            }

            var oldEdge = existing[old];
            var keepOldEdge = !TEdge.IsWeighted || oldEdge.Weight < newEdge.Weight;
            existing[old] = keepOldEdge ? oldEdge : newEdge;
            if (directed || keepOldEdge) continue;
            reverseEdge = TEdge.Create(this, edge, target);
            var otherEdges = _edges[edge];
            var index = otherEdges.FindIndex(e => e.Target == target);
            otherEdges[index] = reverseEdge;
        }
    }


    public int IndexOf(TNode node) => _nodes.IndexOf(node);

    public IReadOnlyList<TEdge> GetEdges(int target) => _edges[target];
    public IEnumerable<TNode> GetNeighbors(int target) => GetEdges(target).Select(e => _nodes[e.Target]);

    public IEnumerable<TNode> EnumerateAllNeighbors(int target)
    {
        var visited = new bool[Count];
        RollingList<int> targets = [target];
        visited[target] = true;
        while (targets.TryPopFront(out var index))
        {
            var edges = _edges[index];
            foreach (var edge in edges)
            {
                var edgeTarget = edge.Target;
                if (visited[edgeTarget])
                    continue;
                visited[edgeTarget] = true;
                targets.PushFront(edgeTarget);
                yield return _nodes[edgeTarget];
            }
        }
    }
}