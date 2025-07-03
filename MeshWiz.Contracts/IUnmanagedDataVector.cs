
namespace MeshWiz.Contracts;

public interface IUnmanagedDataVector<TValue> 
where TValue : unmanaged
{
    ReadOnlySpan<TValue> AsSpan();
}