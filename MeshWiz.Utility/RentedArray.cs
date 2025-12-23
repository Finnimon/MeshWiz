using System.Buffers;

namespace MeshWiz.Utility;

public ref struct RentedArray<T> : IDisposable
{
    private readonly T[] _array;
    private readonly ArrayPool<T>? _source;
    private Once _rented;

    public RentedArray()
    {
        _array = [];
        _rented = default;
    }
    
    private RentedArray(ArrayPool<T> source, int length)
    {
        _source = source;
        _array = _source.Rent(length);
        _rented = Bool.Once();
    }
    public static RentedArray<T> Rent(int length)=>Rent(ArrayPool<T>.Shared, length);

    public static RentedArray<T> Rent(ArrayPool<T> pool, int length) => new(pool, length);
    public static RentedArray<T> Empty() => new();
    /// <inheritdoc />
    public void Dispose()
    {
        if (!_rented) return;
        if (_source is null) return;
        if (!TypeOf<T>.Unmanaged) Array.Clear(_array);
        _source.Return(_array);
    }

    public static implicit operator Span<T>(RentedArray<T> arr) => arr._array;
    public static implicit operator T[](RentedArray<T> arr) => arr._array;
}