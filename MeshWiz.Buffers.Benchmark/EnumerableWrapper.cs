using System.Collections;
using MeshWiz.RefLinq;

namespace MeshWiz.Buffers.Benchmark;

public class EnumerableWrapper<T> : IEnumerable<T>
{
    private readonly List<T> _data;

    public EnumerableWrapper(T[] data)
    {
        _data = data.ToList();
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}