using System.Collections;
using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public sealed class BoundedVolumeHierarchy<TNum>
    : IReadOnlyList<BoundedVolume<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private BoundedVolume<TNum>[] _nodes;
    public int Count { get; private set; }


    public BoundedVolumeHierarchy()
    {
        _nodes = new BoundedVolume<TNum>[256];
        Count = 0;
    }

    public BoundedVolumeHierarchy(int capacity)
    {
        _nodes = new BoundedVolume<TNum>[capacity];
        Count = 0;
    }


    public int Add(BoundedVolume<TNum> node)
    {
        if (Count >= _nodes.Length)
            Array.Resize(ref _nodes, 2 * _nodes.Length);
        var idx = Count++;
        _nodes[idx] = node;
        return idx;
    }

    BoundedVolume<TNum> IReadOnlyList<BoundedVolume<TNum>>.this[int index] => this[index];

    public ref readonly BoundedVolume<TNum> this[int index]
    {
        get
        {
            if (Count <= (uint)index)
                IndexThrowHelper.Throw();
            return ref _nodes[index];
        }
    }

    public BoundedVolume<TNum>[] GetUnsafeAccess() => _nodes;

    internal ref BoundedVolume<TNum> GetWritable(int index) => ref _nodes[index];

    public IEnumerator<BoundedVolume<TNum>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return _nodes[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public void Trim()
    {
        if (_nodes.Length == Count) return;
        Array.Resize(ref _nodes, Count);
    }
}