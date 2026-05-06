using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MeshWiz.Buffers;

public static partial class ArrayBuilder
{
    /// <summary>
    /// Bindings to current best performance safe Array Builder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Helper<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray(IEnumerable<T> enumerable)
            => Segmented<T>.ToArray(enumerable);
        // => RuntimeHelpers.IsReferenceOrContainsReferences<T>()
        //     ? SegmentedUnmanaged<T>.ToArray(enumerable)
        //     : Pooled<T>.ToArray(enumerable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> ToList(IEnumerable<T> enumerable)
            => Segmented<T>.ToList(enumerable);
        // => RuntimeHelpers.IsReferenceOrContainsReferences<T>()
        //     ? SegmentedUnmanaged<T>.ToList(enumerable)
        //     : Pooled<T>.ToList(enumerable);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<TIter>(TIter enumerable)
            where TIter : IEnumerator<T>, allows ref struct
            => Segmented<T>.ToArray(enumerable);
        // => RuntimeHelpers.IsReferenceOrContainsReferences<T>()
        //     ? SegmentedUnmanaged<T>.ToArray(enumerable)
        //     : Pooled<T>.ToArray(enumerable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> ToList<TIter>(TIter enumerable)
            where TIter : IEnumerator<T>, allows ref struct
            => Segmented<T>.ToList(enumerable);
        // => RuntimeHelpers.IsReferenceOrContainsReferences<T>()
        //         ? SegmentedUnmanaged<T>.ToList(enumerable)
        //         : Pooled<T>.ToList(enumerable);
    }
}