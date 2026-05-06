using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Modifications: Took method into another class
namespace MeshWiz.RefLinq;

public static partial class Iterator
{
    internal static void FillIncrementing<T>(Span<T> destination, T value) where T : struct, INumber<T>
    {
        ref var local1 = ref MemoryMarshal.GetReference(destination);
        ref var local2 = ref Unsafe.Add(ref local1, destination.Length);
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported && destination.Length >= Vector<T>.Count)
        {
            var indices = Vector<T>.Indices;
            var source = new Vector<T>(value) + indices;
            var vector = new Vector<T>(T.CreateTruncating(Vector<T>.Count));
            ref var local3 = ref Unsafe.Subtract(ref local2, Vector<T>.Count);
            do
            {
                source.StoreUnsafe(ref local1);
                source += vector;
                local1 = ref Unsafe.Add(ref local1, Vector<T>.Count);
            } while (Unsafe.IsAddressLessThanOrEqualTo(ref local1, ref local3));

            value = source[0];
        }

        for (; Unsafe.IsAddressLessThan(ref local1, ref local2); local1 = ref Unsafe.Add(ref local1, 1))
            local1 = value++;
    }

    public static void FillIncrementing<T>(Span<T> destination, T value, T step) where T : struct, INumber<T>
    {
        ref var local1 = ref MemoryMarshal.GetReference(destination);
        ref var local2 = ref Unsafe.Add(ref local1, destination.Length);
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported && destination.Length >= Vector<T>.Count)
        {
            var indices = Vector<T>.Indices * step;
            var source = Vector.Create(value) + indices;
            var vector = Vector.Create(T.CreateTruncating(Vector<T>.Count) * step);
            ref var local3 = ref Unsafe.Subtract(ref local2, Vector<T>.Count);
            do
            {
                source.StoreUnsafe(ref local1);
                source += vector;
                local1 = ref Unsafe.Add(ref local1, Vector<T>.Count);
            } while (Unsafe.IsAddressLessThanOrEqualTo(ref local1, ref local3));

            value = source[0];
        }

        for (; Unsafe.IsAddressLessThan(ref local1, ref local2); local1 = ref Unsafe.Add(ref local1, 1))
            local1 = (value += step);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<T>(this ReadOnlySpan<T> span, int index, out T? value)
    {
        if (span.Length > (uint)index)
        {
            value = span[index];
            return true;
        }

        value = default;
        return false;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SmartSelectIterator<TIn, TOut> Select<TIn, TOut>(this ReadOnlySpan<TIn> span, Func<TIn, TOut> sel)
        => new(span, sel);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe Span<T> AsSpan<TInline, T>(this ref TInline array, int size)
        where TInline : struct =>
        new(Unsafe.AsPointer(ref array), size);
}