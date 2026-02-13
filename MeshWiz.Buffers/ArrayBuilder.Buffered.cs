using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Buffers;

public static partial class ArrayBuilder
{
    [SuppressMessage("ReSharper", "NotDisposedResource")]
    [Obsolete]
    public ref struct Buffered<T> : IBuilder<Buffered<T>, T>
    where T:unmanaged
    {
        private Arrays _laterSegments;
        private Freelist.Buffer<T> _firstSegment;
        private Span<T> _currentSegment;
        private int _countInFinished;
        private int _poolBufCount;
        private int _curSegmentPosition = -1;
        public readonly int Count => checked(_countInFinished + _curSegmentPosition + 1);

        public readonly bool OnFirstSegment
        {
            [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _poolBufCount == 0;
        }

        public const int MinInitialSize = 512;

        public Buffered()
        {
            _firstSegment = Freelist.Shared.Rent<T>(MinInitialSize);
            _currentSegment = _firstSegment.Span;
        }

        public Buffered(int capacity)
        {
            _firstSegment = Freelist.Shared.Rent<T>(int.Max(capacity, MinInitialSize));
            _currentSegment = _firstSegment.Span;
        }

        private Buffered(int initial, bool _)
        {
            _firstSegment = Freelist.Shared.Rent<T>(initial);
            _currentSegment = _firstSegment.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int SpaceInCurrentSeg()
        {
            var countInCurSeg = _curSegmentPosition + 1;
            return _currentSegment.Length - countInCurSeg;
        }

        public void AddRange(IReadOnlyCollection<T> c) => AddRange(c, c.Count);
        public void AddRange(ICollection<T> c) => AddRange(c, c.Count);

        private void AddRange(IEnumerable<T> collection, int count)
        {
            if (count == 0) return;
            var space = SpaceInCurrentSeg();
            var gotSpace = space >= count;
            if (!gotSpace && _curSegmentPosition == -1) gotSpace = TryExpandByAtLeast(count - SpaceInCurrentSeg());
            if (!gotSpace)
            {
                AddEnumerating(collection);
                return;
            }

            foreach (var elem in collection) AddAssumingSpace(elem);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddEnumerating(IEnumerable<T> collection) => AddEnumeratingInlined(collection);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArrayInlined(IEnumerable<T> data)
        {
            using Buffered<T> b = new();
            b.AddEnumeratingInlined(data);
            return b.ToArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T[] ToArray(IEnumerable<T> data) => ToArrayInlined(data);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<T> ToList(IEnumerable<T> data) => ToListInlined(data);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T[] ToArray<TIter>(TIter iter)
            where TIter : IEnumerator<T>, allows ref struct
            => ToArrayInlined(iter);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<T> ToList<TIter>(TIter iter)
            where TIter : IEnumerator<T>, allows ref struct
            => ToListInlined(iter);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> ToListInlined(IEnumerable<T> data)
        {
            using Buffered<T> b = new();
            b.AddEnumeratingInlined(data);
            return b.ToList();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArrayInlined<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct
        {
            using Buffered<T> b = new();
            b.AddEnumeratorInlined(iter);
            return b.ToArray();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> ToListInlined<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct
        {
            using Buffered<T> b = new();
            b.AddEnumeratorInlined(iter);
            return b.ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEnumeratingInlined(IEnumerable<T> collection)
        {
            using var iter = collection.GetEnumerator();
            AddEnumeratorInlined(iter);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddEnumerator<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct =>
            AddEnumeratorInlined(iter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEnumeratorInlined<TIter>(TIter iterator)
            where TIter : IEnumerator<T>, allows ref struct
        {
            while (iterator.MoveNext())
            {
                var current = iterator.Current;
                ++_curSegmentPosition;
                if (_currentSegment.Length > _curSegmentPosition)
                {
                    _currentSegment[_curSegmentPosition] = current;
                    continue;
                }

                Expand();
                _currentSegment[_curSegmentPosition] = current;
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public void AddInlined(T current)
        // {
        //     ++_size;
        //     ++_curSegmentPosition;
        //     if (_currentSegment.Length != _curSegmentPosition)
        //     {
        //         _currentSegment[_curSegmentPosition] = current;
        //         return;
        //     }
        //
        //     (_currentSegment = Expand())[_curSegmentPosition]=current;
        // }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Expand()
        {
            if (_poolBufCount == 0 && Freelist.TryGrow(in _firstSegment, _curSegmentPosition))
            {
                _currentSegment = _firstSegment.Span;
            }
            else
            {
                SlowPoolBufExpand();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SlowPoolBufExpand()
        {
            _countInFinished += _curSegmentPosition;
            var nextSize = checked(_curSegmentPosition * 2);
            nextSize = int.Min(nextSize, Array.MaxLength);
            var poolBuf = Pool.Rent<T>(nextSize);
            _laterSegments[_poolBufCount++] = poolBuf;
            _currentSegment = poolBuf.Span;
            _curSegmentPosition = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddAssumingSpace(T current)
        {
            _currentSegment[++_curSegmentPosition] = current;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public readonly void ToSpan(Span<T> target) => ToSpanInlined(target);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToSpanInlined(Span<T> target)
        {
            var count = Count;
            if (count != target.Length) ThrowHelper.ThrowArgumentException(nameof(target));
            if (count == 0) return;
            var firstSegment = _firstSegment.Span;
            var poolBufCount = _poolBufCount;
            if (poolBufCount == 0)
            {
                firstSegment.Slice(0, count).CopyTo(target);
                return;
            }

            firstSegment.CopyTo(target);
            var offset = firstSegment.Length;
            var countFullPoolBuf = poolBufCount - 1;
            for (var i = 0; i < countFullPoolBuf; i++)
            {
                var fullPoolBuf = _laterSegments[i];
                fullPoolBuf.Span.CopyTo(target.Slice(offset));
                offset += fullPoolBuf.Span.Length;
            }

            _currentSegment.Slice(0, _curSegmentPosition + 1).CopyTo(target.Slice(offset));
        }


        private bool TryExpandByAtLeast(int target)
        {
            if (OnFirstSegment)
            {
                var growth = Freelist.GrowGreedy(in _firstSegment);
                _currentSegment = _firstSegment.Span;
                if (growth != 0) return growth >= target;
            }

            var nextSize = target * 2;
            if (nextSize < 0) ThrowHelper.ThrowInsufficientMemoryException();
            nextSize = int.Min(nextSize, Array.MaxLength);
            var poolBuf = Pool.Rent<T>(nextSize);
            _laterSegments[_poolBufCount++] = poolBuf;
            _curSegmentPosition = -1;
            _currentSegment = poolBuf.Span;
            return nextSize >= target;
        }

        public void Dispose()
        {
            if (_poolBufCount == 0)
            {
                _firstSegment.Dispose();
                return;
            }

            _firstSegment.Dispose();
            for (var i = 0; i < _poolBufCount; i++)
                _laterSegments[i].Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private ref struct Arrays
        {
        // @formatter:off
 #pragma warning disable CS0169 // Field is never used
 #pragma warning disable CS0649 // Field is never used
 // ReSharper disable once UnassignedField.Local
        private Pool.Buffer<T> _0,_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13,_14,_15,_16,_17,_18,_19;   
 #pragma warning restore CS0649 // Field is never used
 #pragma warning restore CS0169 // Field is never used
            // @formatter:on
            public ref Pool.Buffer<T> this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref Unsafe.Add(ref Unsafe.AsRef(ref _0), index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray()
        {
            var count = Count;
            if (count == 0)
                return [];
            var arr = GC.AllocateUninitializedArray<T>(count);
            ToSpanInlined(arr);
            return arr;
        }

        public readonly List<T> ToList()
        {
            var count = Count;
            if (count == 0) return [];
            var list = new List<T>(count);
            CollectionsMarshal.SetCount(list, count);
            ToSpanInlined(CollectionsMarshal.AsSpan(list));
            return list;
        }
    }
}