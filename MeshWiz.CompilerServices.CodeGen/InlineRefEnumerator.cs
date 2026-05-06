using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MeshWiz.CompilerServices.CodeGen;

#pragma warning disable CS8500
public sealed class UnsafeRefEnumerator<T> : IEnumerator<T>
    where T : allows ref struct
{
    private readonly nint _start;
    private readonly int _length;
    private int _pos;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe UnsafeRefEnumerator(ref T start, int length)
    {
        _start = (nint)Unsafe.AsPointer(ref start);
        _length = length;
        _pos = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        var index = checked(_pos + 1);
        if (index < _length)
        {
            _pos = index;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => _pos = -1;

    public T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.AddByteOffset(ref Unsafe.NullRef<T>(), _start + Unsafe.SizeOf<T>() * _pos);
    }

    object IEnumerator.Current => InlineRefArrayThrowHelper.NotSupported<object>();

    /// <inheritdoc />
    public void Dispose() { }
}

public unsafe ref struct InlineRefEnumerator<T> : IEnumerator<T>
    where T : allows ref struct
{
    private readonly T* _start;
    private readonly nint _length;
    private nint _pos;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InlineRefEnumerator(T* start, nint length)
    {
        _start = start;
        _length = length;
        _pos = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        var index = checked(_pos + (nint)1);
        if (index < _length)
        {
            _pos = index;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => _pos = -1;

    public readonly T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _start[_pos];
    }

    object IEnumerator.Current => InlineRefArrayThrowHelper.NotSupported<object>();

    /// <inheritdoc />
    public void Dispose() { }
}