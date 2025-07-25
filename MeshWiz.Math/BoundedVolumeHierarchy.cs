using System.Collections;
using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed class BoundedVolumeHierarchy<TNum>
    : IReadOnlyList<BoundedVolume<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private BoundedVolume<TNum>[] _nodes;
    public int Count { get; private set; }

    public const int GrowthFloor = 256;
    public int GrowthCap
    {
        get => int.Max(field, GrowthFloor);
        init => field = int.Max(value, GrowthFloor);
    }

    public BoundedVolumeHierarchy()
    {
        _nodes = new BoundedVolume<TNum>[256];
        Count = 0;
        GrowthCap = 4096;
    }

    
    public int Add(BoundedVolume<TNum> node)
    {
        if (Count >= _nodes.Length) 
            Array.Resize(ref _nodes, _nodes.Length+int.Min(_nodes.Length,GrowthCap));
        var idx = Count++;
        _nodes[idx] = node;
        return idx;
    }

    BoundedVolume<TNum> IReadOnlyList<BoundedVolume<TNum>>.this[int index] => this[index];

    public ref readonly BoundedVolume<TNum> this[int index]
    {
        get
        {
            if (index.InsideInclusiveRange(0, Count - 1)) return ref _nodes[index];
            throw new IndexOutOfRangeException();
        }
    }
    
    public BoundedVolume<TNum>[] GetUnsafeAccess()=>_nodes;
    
    internal ref BoundedVolume<TNum> GetWritable(int index)=>ref _nodes[index];
    
    private ref BoundedVolume<TNum> ThrowIndexOutOfRange()
    =>throw new IndexOutOfRangeException();

    public IEnumerator<BoundedVolume<TNum>> GetEnumerator()
    {
        for(var i=0;i<Count;i++) yield return _nodes[i]; 
    }

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
    
    public void Trim()=>Array.Resize(ref _nodes, Count);
}