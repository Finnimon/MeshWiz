using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    
    TItem First(Func<TItem,bool> predicate)=>this.Where(predicate).First();
    TItem? FirstOrDefault(Func<TItem,bool> predicate)=>this.Where(predicate).FirstOrDefault();

    TItem Last(Func<TItem,bool> predicate)=>this.Where(predicate).Last();
    TItem? LastOrDefault(Func<TItem,bool> predicate)=>this.Where(predicate).LastOrDefault();

    bool Any()
    {
        var copy = this;
        return copy.MoveNext();
    }
    bool Any(Func<TItem,bool> predicate)=>Where(predicate).MoveNext();
    int MaxPossibleCount();

    OfTypeIterator<TSelf, TItem, TOther> OfType<TOther>();


}