// using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;
// using CommunityToolkit.Diagnostics;
//
// namespace MeshWiz.RefLinq;
//
// public ref struct OrderedIterator<TIter, TItem>
// where TIter:IRefIterator<TIter,TItem>
// {
//     private TIter _inner;
//     private readonly IComparer<TItem> _comparer;
//     private int[] _keys;
//     private TItem[] _elements;
//
//     public OrderedIterator(TIter inner, IComparer<TItem> comparer)
//     {
//         _inner = inner;
//         _comparer = comparer;
//     }
//
//
//
//
//
//
//
//     public bool Any() => _inner.Any();
//     public int Count() => _inner.Count();
//     public bool TryGetNonEnumeratedCount(out int count) => _inner.TryGetNonEnumeratedCount(out count);
//     
//
//     
//     public TItem Min() => _inner.Min();
//     public TItem Max() => _inner.Max();
//     public TItem MaxBy<TKey>(Func<TItem, TKey> keySel) where TKey : IComparable<TKey> => _inner.MaxBy(keySel);
//     public TItem MinBy<TKey>(Func<TItem, TKey> keySel) where TKey : IComparable<TKey> => _inner.MinBy(keySel);
//     public TItem? MinOrDefault() => _inner.MinOrDefault();
//     public TItem? MaxOrDefault() => _inner.MinOrDefault();
//     public TItem? MaxOrDefaultBy<TKey>(Func<TItem, TKey> keySel) where TKey : IComparable<TKey> => _inner.MaxOrDefaultBy(keySel);
//     public TItem? MinOrDefaultBy<TKey>(Func<TItem, TKey> keySel) where TKey : IComparable<TKey> => _inner.MinOrDefaultBy(keySel);
//     public TItem First()
//     {
//         if (!TryGetFirst(out var first)) ThrowHelper.ThrowInvalidOperationException();
//         return first!;
//     }
//     public TItem Last()
//     {
//         if (!TryGetLast(out var last)) ThrowHelper.ThrowInvalidOperationException();
//         return last!;
//     }
//     public bool TryGetLast(out TItem? item)
//     {
//         var none = _inner.Any();
//         if (none)
//         {
//             item = default;
//             return false;
//         }
//
//         item= _inner.Max(_comparer);
//         return true;
//     }
//     
//     public bool TryGetFirst(out TItem? item)
//     {
//         var none = _inner.Any();
//         if (none)
//         {
//             item = default;
//             return false;
//         }
//         item= _inner.Min(_comparer);
//         return true;
//     }
//
//     public TItem? FirstOrDefault()
//     {
//         TryGetFirst(out var first);
//         return first;
//     }
//     
//     public TItem? LastOrDefault()
//     {
//         TryGetLast(out var last);
//         return last;
//     }
// }