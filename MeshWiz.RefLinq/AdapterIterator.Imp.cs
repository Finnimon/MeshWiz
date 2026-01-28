using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace MeshWiz.RefLinq;

public readonly partial struct AdapterIterator<T>
{
    internal static class Imp
    {
        
        public static bool TryGetSpan(IImp imp,out ReadOnlySpan<T> data) => imp.Underlying.TryGetSpan(out data);

        internal interface IImp : IEnumerator<T>
        {
            public IEnumerable<T> Underlying { get; }
            bool TryGetNonEnumeratedCount(out int count);
            int Count();
            bool TryTakeRange(Range r,out IImp? range);
            bool TryGetLast(out T? item);
            bool TryGetFirst(out T? item);
            void CopyTo(Span<T> dest);
            bool TryGetSpan(out ReadOnlySpan<T> data);
        }

        internal sealed class BaseImp : IImp
        {
            public IEnumerable<T> Underlying => _source;
            private readonly IEnumerable<T> _source;
            private IEnumerator<T> _enumerator;

            public BaseImp(IEnumerable<T> source)
            {
                _source = source;
                _enumerator = _source.GetEnumerator();
            }
            
            /// <inheritdoc />
            public bool MoveNext() => _enumerator.MoveNext();

            /// <inheritdoc />
            public void Reset()
            {
                _enumerator.Dispose();
                _enumerator = _source.GetEnumerator();
            }

            /// <inheritdoc />
            public T Current => _enumerator.Current;

            /// <inheritdoc />
            object? IEnumerator.Current => _enumerator.Current;

            /// <inheritdoc />
            public void Dispose() => _enumerator.Dispose();

            /// <inheritdoc />
            public bool TryGetNonEnumeratedCount(out int count)
            {
                switch (_source)
                {
                    case IReadOnlyCollection<T> col:
                        count = col.Count;
                        return true;
                    case ICollection<T> col2:
                        count = col2.Count;
                        return true;
                    default:
                        count = 0;
                        return false;
                }
            }

            /// <inheritdoc />
            public int Count()
            {
                if (TryGetNonEnumeratedCount(out var count))
                    return count;
                count = 0;
                Reset();
                while (_enumerator.MoveNext()) ++count;
                return count;
            }

            /// <inheritdoc />
            public bool TryTakeRange(Range r, out IImp? range)
            {
                if (_source is IReadOnlyList<T> or IList<T>)
                    return Create(_source).TryTakeRange(r,out range);
                range = null;
                return false;
            }

            /// <inheritdoc />
            public bool TryGetLast(out T? item)
            {
                Reset();
                var moved = false;
                item = default;
                while (_enumerator.MoveNext())
                {
                    moved = true;
                    item = _enumerator.Current;
                }

                return moved;
            }

            /// <inheritdoc />
            public bool TryGetFirst(out T? item)
            {
                Reset();
                item = default;
                if (!MoveNext())
                    return false;

                item = Current;
                return true;
            }

            /// <inheritdoc />
            public void CopyTo(Span<T> dest)
            {
                
                switch (_source)
                {
                    case T[] arr:
                        arr.CopyTo(dest);
                        break;
                    case List<T> list:
                        CollectionsMarshal.AsSpan(list).CopyTo(dest);
                        break;
                    default:
                        var i = -1;
                        foreach (var item in _source) dest[++i] = item;
                        break;
                }
            }

            /// <inheritdoc />
            public bool TryGetSpan(out ReadOnlySpan<T> data)
                => Imp.TryGetSpan(this, out data);
        }
        internal sealed class ListImp : IImp
        {
            private readonly IReadOnlyList<T> _data;
            private int _pos;
            private readonly int _offset;
            private readonly int _length;

            public ListImp(IReadOnlyList<T> data, int offset, int length)
            {
                _pos = -1;
                _data = data;
                _offset = offset;
                _length = length;
            }

            /// <inheritdoc />
            public void Dispose() { }

            /// <inheritdoc />
            public bool MoveNext() => ++_pos < _length;

            /// <inheritdoc />
            public void Reset() => _pos = -1;

            public T Current => _data[_pos+_offset];

            /// <inheritdoc />
            object? IEnumerator.Current => Current;

            /// <inheritdoc />
            public IEnumerable<T> Underlying => _data;

            /// <inheritdoc />
            public bool TryGetNonEnumeratedCount(out int count)
            {
                count = _length;
                return true;
            }

            /// <inheritdoc />
            public int Count() => _length;

            /// <inheritdoc />
            public bool TryTakeRange(Range r, out IImp range)
            {
                var (offset, length) = r.GetOffsetAndLength(_length);
                range = new ListImp(_data, offset + _offset, length);
                return true;
            }

            /// <inheritdoc />
            public bool TryGetLast(out T? item) => TryGet(_length-1, out item);


            /// <inheritdoc />
            public bool TryGetFirst(out T? item) => TryGet(0, out item);

            /// <inheritdoc />
            public void CopyTo(Span<T> dest)
            {
                if (TryGetSpan(out var span))
                {
                    span.CopyTo(dest);
                    return;
                }
                
                for (var i = 0; i < _length; i++) 
                    dest[i] = _data[i+_offset];
            }

            public bool TryGet(int index, out T? item)
            {
                index += _offset;
                if (int.Min(_data.Count,_offset+_length) > (uint)index)
                {
                    item = _data[index];
                    return true;
                }

                item = default;
                return false;
            }
            
            /// <inheritdoc />
            public bool TryGetSpan(out ReadOnlySpan<T> data)
                => Imp.TryGetSpan(this, out data);
        }

        public static IImp Create(IEnumerable<T> enumerable)
            => enumerable switch
            {
                IReadOnlyList<T> l => new ListImp(l, 0, l.Count),
                IList<T> l=>new ListImp(new ListToList(l),0,l.Count),
                _ => new BaseImp(enumerable)
            };

        private sealed record ListToList(IList<T> Underlying) : IReadOnlyList<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IEnumerator<T> GetEnumerator() => Underlying.GetEnumerator();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator IEnumerable.GetEnumerator() => Underlying.GetEnumerator();

            public int Count => Underlying.Count;

            public T this[int index] => Underlying[index];
        }
    }
}