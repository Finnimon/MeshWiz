using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Buffers;

public static partial class ArrayBuilder
{
    
    [SuppressMessage("ReSharper", "NotDisposedResource")]
    [Obsolete]
    public ref struct Pooled<T> : IBuilder<Pooled<T>,T>
    where T:unmanaged
    {
        private PoolBufs _bufs;
        private int _poolBufCount;
        private readonly Span<T> _firstSegment;
        private Span<T> _currentSegment;
        private int _countInCurrentSegment;
        private int _countInFinished;
        public readonly int Count => checked(_countInCurrentSegment + _countInFinished);
        public  Pooled(Span<T> firstSegment)
        {
            _firstSegment = firstSegment;
            _currentSegment = _firstSegment;
        }

        /// <inheritdoc />
        public static T[] ToArrayInlined(IEnumerable<T> data)
        {
            Unsafe.SkipInit(out Scratch<T> s);
            using Pooled<T> b = new(s);
            b.AddEnumeratingInlined(data);
            return b.ToArray();
        }

        /// <inheritdoc />
        public static List<T> ToListInlined(IEnumerable<T> data)
        {
            Unsafe.SkipInit(out Scratch<T> s);
            using Pooled<T> b = new(s);
            b.AddEnumeratingInlined(data);
            return b.ToList();
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T[] ToArray(IEnumerable<T> data) => ToArrayInlined(data);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<T> ToList(IEnumerable<T> data) => ToListInlined(data);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T[] ToArray<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct => ToArrayInlined(iter);
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<T> ToList<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct => ToListInlined(iter);

        /// <inheritdoc />
        public static T[] ToArrayInlined<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct
        {
            Unsafe.SkipInit(out Scratch<T> s);
            using Pooled<T> b = new(s);
            b.AddEnumeratorInlined(iter);
            return b.ToArray();
        }

        /// <inheritdoc />
        public static List<T> ToListInlined<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct
        {
            Unsafe.SkipInit(out Scratch<T> s);
            using Pooled<T> b = new(s);
            b.AddEnumeratorInlined(iter);
            return b.ToList();
        }

        /// <inheritdoc />
        public void AddEnumeratingInlined(IEnumerable<T> enumerable)
        {
            using var iter = enumerable.GetEnumerator();
            AddEnumeratorInlined(iter);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddEnumerating(IEnumerable<T> enumerable)
            => AddEnumeratingInlined(enumerable);
        


        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddEnumerator<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct
            => AddEnumeratorInlined(iter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEnumeratorInlined<TITer>(TITer iter)
            where TITer : IEnumerator<T>, allows ref struct
        {
            var seg = _currentSegment;
            var countInCurrentSegment = _countInCurrentSegment;
            while (iter.MoveNext())
            {
                    var current = iter.Current;
                if (countInCurrentSegment < seg.Length)
                {
                    seg[countInCurrentSegment++] = current;
                }
                else
                {
                    var buf= Pool.Rent<T>(int.Max(1,checked(countInCurrentSegment * 2)));
                    _bufs[_poolBufCount++] = buf;
                    _countInFinished += countInCurrentSegment;
                    seg = buf.Span;
                    seg[0] = current;
                    countInCurrentSegment = 1;
                }
            }

            _currentSegment = seg;
            _countInCurrentSegment = countInCurrentSegment;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            var count = Count;
            if (count == 0) return [];
            var arr = GC.AllocateUninitializedArray<T>(count);
            ToSpanInlined(arr);
            return arr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ToList()
        {
            var count = Count;
            if (count == 0) return [];
            List<T> list = new(count);
            CollectionsMarshal.SetCount(list,count);
            ToSpanInlined(CollectionsMarshal.AsSpan(list));
            return list;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public readonly void ToSpan(Span<T> destination) => ToSpanInlined(destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void ToSpanInlined(Span<T> destination)
        {
            var count = Count;
            if(count >destination.Length) ThrowHelper.ThrowArgumentException(nameof(destination));
            var firstSegment = _firstSegment;
            if (_poolBufCount == 0)
            {
                firstSegment.Slice(0,count).CopyTo(destination);
            }
            else
            {
                firstSegment.CopyTo(destination);
                var offset = firstSegment.Length;
                for(var i=0;i<_poolBufCount-1;i++)
                {
                    var bufSpan=_bufs[i].Span;
                    bufSpan.CopyTo(destination.Slice(offset));
                    offset += bufSpan.Length;
                }
                _currentSegment.Slice(0,_countInCurrentSegment).CopyTo(destination.Slice(offset));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            var poolBufCount = _poolBufCount;
            for(var i=0;i<poolBufCount;i++)
                _bufs[i].Dispose();
        }
        [StructLayout(LayoutKind.Sequential)]
        private ref struct PoolBufs
        {
        // @formatter:off
 #pragma warning disable CS0169 // Field is never used
 #pragma warning disable CS0649 // Field is never used
 // ReSharper disable once UnassignedField.Local
        private Pool.Buffer<T> _0,_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13,_14,_15,_16,_17,_18,_19,_20,_21,_22,_23,_24,_25,_26;   
 #pragma warning restore CS0649 // Field is never used
 #pragma warning restore CS0169 // Field is never used
            // @formatter:on
            public ref Pool.Buffer<T> this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref Unsafe.Add(ref Unsafe.AsRef(ref _0), index);
            }
        }
    }
}