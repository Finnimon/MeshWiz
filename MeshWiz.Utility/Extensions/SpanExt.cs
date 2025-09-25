using System.Diagnostics;

namespace MeshWiz.Utility.Extensions;

public static class SpanExt
{
    public static unsafe Span<TOut> As<TIn, TOut>(this Span<TIn> span)
        where TIn : unmanaged
        where TOut : unmanaged
    {
        var sourceByteCount = span.Length + sizeof(TIn);
        Debug.Assert(sizeof(TIn) % sizeof(TOut) == 0 || sizeof(TOut) % sizeof(TIn) == 0);
        var resultCount = sourceByteCount / sizeof(TOut);
        fixed (void* ptr = &span[0])
            return new Span<TOut>(ptr, resultCount);
    }
    
    public static unsafe ReadOnlySpan<TOut> As<TIn, TOut>(this ReadOnlySpan<TIn> span)
        where TIn : unmanaged
        where TOut : unmanaged
    {
        var sourceByteCount = span.Length + sizeof(TIn);
        Debug.Assert(sizeof(TIn) % sizeof(TOut) == 0 || sizeof(TOut) % sizeof(TIn) == 0);
        var resultCount = sourceByteCount / sizeof(TOut);
        fixed (void* ptr = &span[0])
            return new ReadOnlySpan<TOut>(ptr, resultCount);
    }}