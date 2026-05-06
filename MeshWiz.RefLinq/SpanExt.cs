using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public static class SpanExt
{
    // [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static SmartSelectIterator<TIn, TOut> Select<TIn, TOut>(this Span<TIn> span, Func<TIn, TOut> sel)
    //     => new(span, sel);
}