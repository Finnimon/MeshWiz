using System.Collections;

namespace MeshWiz.Collections;

public interface IVersionedList<T> : IList<T>
{
    protected internal int Version { get; }
    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator<IVersionedList<T>,T>(this);
}