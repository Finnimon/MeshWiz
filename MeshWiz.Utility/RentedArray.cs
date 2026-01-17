using System.Buffers;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Utility;

public readonly ref struct RentedArray<T> : IDisposable
{
    private readonly T[] _array;
    private readonly ArrayPool<T>? _source;
    private readonly int _targetLength;
    private readonly Once _rented;
    private readonly bool _clearUnmanaged;

    public RentedArray()
    {
        _array = [];
        _rented = default;
    }

    private RentedArray(ArrayPool<T> source, int length, bool clearUnmanagedTypes)
    {
        _rented = Bool.Once();
        if (length is 0)
        {
            _source = null;
            _array = [];
            return;
        }
        _source = source;
        _array = _source.Rent(length);
        _targetLength = length;
        _clearUnmanaged = clearUnmanagedTypes;
    }

    public static RentedArray<T> Rent(int length, bool clearUnmanagedTypes = false) =>
        Rent(ArrayPool<T>.Shared, length, clearUnmanagedTypes);

    public static RentedArray<T> Rent(ArrayPool<T> pool, int length, bool clearUnmanagedTypes = false) =>
        new(pool, length, clearUnmanagedTypes);

    public static RentedArray<T> Empty() => new();

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_rented) return;
        _source?.Return(_array, clearArray: _clearUnmanaged || !TypeOf<T>.Unmanaged);
    }

    public static implicit operator Span<T>(RentedArray<T> arr) => GetArray(arr).AsSpan(0, arr._targetLength);

    public static implicit operator T[](RentedArray<T> arr) => GetArray(arr);
    public T[] GetCompleteArray() => GetArray(this);

    private static T[] GetArray(RentedArray<T> arr)
    {
        if (arr._targetLength == 0)
            return [];
        if (!arr._rented.ReadValue())
            ThrowHelper.ThrowInvalidOperationException();
        return arr._array;
    }
}