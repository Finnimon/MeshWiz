using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Collections;

public readonly ref struct RollingSpan<T>(Span<T> data)
{
    public RollingSpan() : this(Span<T>.Empty) { }
    public readonly Span<T> Data = data;
    public int Length => Data.Length;

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Data[ResolveIndex(index)];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Data[ResolveIndex(index)] = value;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ResolveIndex(int index) => index.WrapZeroBound(Data.Length);

    public static implicit operator RollingSpan<T>(T[] data) => new(data);
    public static implicit operator RollingSpan<T>(Span<T> data) => new(data);
    public static implicit operator Span<T>(RollingSpan<T> data) => data.Data;
}