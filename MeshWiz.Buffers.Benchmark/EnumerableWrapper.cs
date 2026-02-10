using System.Collections;
using System.Runtime.CompilerServices;
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

public class EnumerableReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>
{
    private readonly IReadOnlyCollection<T> _data;
    public EnumerableReadOnlyCollectionWrapper(IReadOnlyCollection<T> data)
    {
        _data = data;
    }
    public int Count => _data.Count;

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_data).GetEnumerator();
    }
}