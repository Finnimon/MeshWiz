using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.CompilerServices.CodeGen;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace MeshWiz.CompilerServices;

#pragma warning disable CS8500
[StructLayout(LayoutKind.Sequential)]
public ref struct InlineRefArray16<T>
    where T : allows ref struct
{
    private T _0, _1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13,_14,_15;
    public int Length => 16;
    public nint NativeLength => 16;

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (16u <= (uint)index) ThrowHelper.Index();
            return Unsafe.Add(ref _0, index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (16u <= (uint)index) ThrowHelper.Index();
            Unsafe.Add(ref _0, index) = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe InlineRefEnumerator<T> GetEnumerator()
    {
        fixed (T* ptr = &_0) return new InlineRefEnumerator<T>(ptr, 16);
    }
}

[StructLayout(LayoutKind.Sequential)]
public ref struct InlineRefArray2<T> : IEnumerable<T>
    where T : allows ref struct
{
    private T _0, _1;
    public int Length => 2;
    public nint NativeLength => 2;

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (2u <= (uint)index) ThrowHelper.Index();
            return Unsafe.Add(ref _0, index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (2u <= (uint)index) ThrowHelper.Index();
            Unsafe.Add(ref _0, index) = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe InlineRefEnumerator<T> GetEnumerator()
    {
        fixed (T* ptr = &_0) return new InlineRefEnumerator<T>(ptr, 2);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new UnsafeRefEnumerator<T>(ref _0, Length);
    IEnumerator IEnumerable.GetEnumerator() => new UnsafeRefEnumerator<T>(ref _0, Length);
}
