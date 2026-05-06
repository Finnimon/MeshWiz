using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public static partial class Iterator
{
    public static bool SequenceEqual<TLeft, TRight, T>(this TLeft l, TRight r, IEqualityComparer<T>? comparer = null)
        where TLeft : IRefIterator<TLeft, T>, allows ref struct
        where TRight : IRefIterator<TRight, T>, allows ref struct
    {
        l.Reset();
        r.Reset();
        if (l.TryGetNonEnumeratedCount(out var lCount) && r.TryGetNonEnumeratedCount(out var rCount))
        {
            if (rCount != lCount)
                return false;
            if (rCount == 0)
                return true;
        }

        if ((comparer is null || Equals(comparer, EqualityComparer<T>.Default)) && !RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            return VectorizedSequenceEqual<TLeft, TRight, T>(l, r);
        return ComparerSequenceEqual(l, r, comparer);
    }

    private static bool ComparerSequenceEqual<TLeft, TRight, T>(TLeft left, TRight right,
        IEqualityComparer<T>? comparer)
        where TLeft : IRefIterator<TLeft, T>, allows ref struct
        where TRight : IRefIterator<TRight, T>, allows ref struct
    {
        comparer ??= EqualityComparer<T>.Default;
        while (left.MoveNext())
            if (!right.MoveNext()
                || !comparer.Equals(left.Current, right.Current))
                return false;
        return true;
    }

    private static bool VectorizedSequenceEqual<TLeft, TRight, T>(TLeft left, TRight right)
        where TLeft : IRefIterator<TLeft, T>, allows ref struct
        where TRight : IRefIterator<TRight, T>, allows ref struct
    {
        Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        if (left.TryConvertToSpanIter<TLeft, T>(out var lSpanIter) &&
            right.TryConvertToSpanIter<TRight, T>(out var rSpanIter))
            return lSpanIter.OriginalSource.SequenceEqual(rSpanIter.OriginalSource);
        var size = Unsafe.SizeOf<T>();
        var l = Vector256<byte>.Zero;
        var r = Vector256<byte>.Zero;
        var writes = Vector256<byte>.Count / size;
        if (writes == 0)
            return ComparerSequenceEqual(left, right, EqualityComparer<T>.Default);
        ref var lPin = ref Unsafe.As<Vector256<byte>, byte>(ref l);
        ref var rPin = ref Unsafe.As<Vector256<byte>, byte>(ref r);
        while (true)
        {
            for (var i = 0; i < writes; i++)
            {
                var lMoved = left.MoveNext();
                var rMoved = right.MoveNext();
                if (lMoved != rMoved)
                    return false;
                var notMoved = !lMoved && !rMoved;
                if (notMoved)
                    goto finalCheck;
                if (i == 0 && notMoved)
                    return true;
                if (notMoved)
                    break;
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref lPin, size * i), left.Current);
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref rPin, size * i), right.Current);
            }

            var vEq = l == r;
            if (!vEq)
                return false;
        }

        finalCheck:
        return l == r;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOrDefault<T>(this ReadOnlySpan<T> span, Index index)
    {
        var idx = index.IsFromEnd ? span.Length - index.Value : index.Value;
        return span.GetOrDefault(idx);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanIterator<T> Iterate<T>(this Span<T> span) => span;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OfTypeIterator<SpanIterator<TIn>, TIn, TOut> OfType<TIn, TOut>(this ReadOnlySpan<TIn> sp)
        => new(sp);
}