using System.Diagnostics.Contracts;

namespace MeshWiz.Math;

public interface ITransform<T>
{
    [Pure]
    public T Transform(T src);
}

