using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed class BoundedVolumeList<TNum>
    : IReadOnlyList<BoundedVolume<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public BoundedVolume<TNum>[] Nodes;
    public int Count { get; private set; }

    public const int GrowthFloor = 256;
    public int GrowthCap
    {
        get => int.Max(field, GrowthFloor);
        init => field = int.Max(value, GrowthFloor);
    }

    public BoundedVolumeList()
    {
        Nodes = new BoundedVolume<TNum>[256];
        Count = 0;
        GrowthCap = 4096;
    }

    
    public int Add(BoundedVolume<TNum> node)
    {
        if (Count >= Nodes.Length) 
            Array.Resize(ref Nodes, Nodes.Length+int.Min(Nodes.Length,GrowthCap));
        var idx = Count++;
        Nodes[idx] = node;
        return idx;
    }

    BoundedVolume<TNum> IReadOnlyList<BoundedVolume<TNum>>.this[int index] => this[index];

    public ref BoundedVolume<TNum> this[int index]
    {
        get
        {
            if (index.InsideInclusiveRange(0, Count - 1)) return ref Nodes[index];
            throw new IndexOutOfRangeException();
        }
    }

    private ref BoundedVolume<TNum> ThrowIndexOutOfRange()
    =>throw new IndexOutOfRangeException();

    public IEnumerator<BoundedVolume<TNum>> GetEnumerator()
    {
        for(var i=0;i<Count;i++) yield return Nodes[i]; 
    }

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
    
    public void Trim()=>Array.Resize(ref Nodes, Count);
}