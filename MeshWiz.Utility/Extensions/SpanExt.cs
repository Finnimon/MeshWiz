using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility.Extensions;

public static class SpanExt
{
    public static unsafe Span<TOut> As<TIn, TOut>(this Span<TIn> span)
        where TIn : unmanaged
        where TOut : unmanaged
    {
        if (span.IsEmpty)
            return Span<TOut>.Empty;
        var sourceByteCount = span.Length * Unsafe.SizeOf<TIn>();
        Debug.Assert(sizeof(TIn) % sizeof(TOut) == 0 || sizeof(TOut) % sizeof(TIn) == 0);
        var resultCount = sourceByteCount / Unsafe.SizeOf<TOut>();
        return new Span<TOut>(Unsafe.AsPointer(in span[0]), resultCount);
    }
    
    public static unsafe ReadOnlySpan<TOut> As<TIn, TOut>(this ReadOnlySpan<TIn> span)
        where TIn : unmanaged
        where TOut : unmanaged
    {
        if (span.IsEmpty)
            return ReadOnlySpan<TOut>.Empty;
        var sourceByteCount = span.Length * Unsafe.SizeOf<TIn>();
        Debug.Assert(sizeof(TIn) % sizeof(TOut) == 0 || sizeof(TOut) % sizeof(TIn) == 0);
        var resultCount = sourceByteCount / Unsafe.SizeOf<TOut>();
        return new ReadOnlySpan<TOut>(Unsafe.AsPointer(in span[0]), resultCount);
    }
    
}