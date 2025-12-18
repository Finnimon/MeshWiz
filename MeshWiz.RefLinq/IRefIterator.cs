using System.Diagnostics.CodeAnalysis;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public interface IRefIterator<TSelf, TItem> : IEnumerator<TItem> where TSelf : IRefIterator<TSelf, TItem>, allows ref struct
{
    TItem First();
    TItem? FirstOrDefault();
    bool TryGetFirst(out TItem? item);
    TItem Last();
    TItem? LastOrDefault();
    bool TryGetLast(out TItem? item);
    int Count();
    bool TryGetNonEnumeratedCount(out int count);
    void CopyTo(Span<TItem> destination);
    TSelf GetEnumerator();
    FilterIterator<TSelf, TItem> Where(Func<TItem, bool> predicate);
    SelectIterator<TSelf,TItem,TOut> Select<TOut>(Func<TItem,TOut> selector);
    RangeIterator<TSelf, TItem> Take(Range r);
    RangeIterator<TSelf, TItem> Take(int num);
    RangeIterator<TSelf,TItem> Skip(int num);
    
    
    TItem[] ToArray();
    List<TItem> ToList();
    HashSet<TItem> ToHashSet();
    HashSet<TItem> ToHashSet(IEqualityComparer<TItem> comp);
    
}