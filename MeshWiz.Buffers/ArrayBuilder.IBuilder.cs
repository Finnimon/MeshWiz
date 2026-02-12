using System.Runtime.CompilerServices;

namespace MeshWiz.Buffers;

public static partial class ArrayBuilder
{
    public interface IBuilder<TSelf, T>
        where TSelf : IBuilder<TSelf, T>, allows ref struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract T[] ToArrayInlined(IEnumerable<T> data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract List<T> ToListInlined(IEnumerable<T> data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract T[] ToArrayInlined<TIter>(TIter iter)
            where TIter : IEnumerator<T>, allows ref struct;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract List<T> ToListInlined<TIter>(TIter iter)
            where TIter : IEnumerator<T>, allows ref struct;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static virtual T[] ToArray(IEnumerable<T> data) => TSelf.ToArrayInlined(data);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static virtual List<T> ToList(IEnumerable<T> data) => TSelf.ToListInlined(data);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static virtual T[] ToArray<TIter>(TIter iter)
            where TIter : IEnumerator<T>, allows ref struct
            => TSelf.ToArrayInlined<TIter>(iter);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static virtual List<T> ToList<TIter>(TIter iter)
            where TIter : IEnumerator<T>, allows ref struct
            => TSelf.ToListInlined<TIter>(iter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddEnumeratingInlined(IEnumerable<T> enumerable);

        [MethodImpl(MethodImplOptions.NoInlining)]
        void AddEnumerating(IEnumerable<T> enumerable) => AddEnumeratingInlined(enumerable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddEnumeratorInlined<TIter>(TIter iter)
            where TIter : IEnumerator<T>, allows ref struct;

        [MethodImpl(MethodImplOptions.NoInlining)]
        void AddEnumerator<TIter>(TIter iter)
            where TIter : IEnumerator<T>, allows ref struct 
            => AddEnumeratorInlined(iter);
    }
}