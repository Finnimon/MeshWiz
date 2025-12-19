using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public interface IRefIterator<TSelf, TItem> : IEnumerator<TItem>
    where TSelf : IRefIterator<TSelf, TItem>, allows ref struct
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
    SelectIterator<TSelf, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector);
    RangeIterator<TSelf, TItem> Take(Range r);
    RangeIterator<TSelf, TItem> Take(int num);
    RangeIterator<TSelf, TItem> Skip(int num);


    TItem[] ToArray();
    List<TItem> ToList();
    HashSet<TItem> ToHashSet();
    HashSet<TItem> ToHashSet(IEqualityComparer<TItem> comp);

    TItem First(Func<TItem, bool> predicate) => this.Where(predicate).First();
    TItem? FirstOrDefault(Func<TItem, bool> predicate) => this.Where(predicate).FirstOrDefault();

    TItem Last(Func<TItem, bool> predicate) => this.Where(predicate).Last();
    TItem? LastOrDefault(Func<TItem, bool> predicate) => this.Where(predicate).LastOrDefault();

    bool Any()
    {
        var copy = this;
        return copy.MoveNext();
    }

    bool Any(Func<TItem, bool> predicate) => Where(predicate).MoveNext();
    int EstimateCount();

    OfTypeIterator<TSelf, TItem, TOther> OfType<TOther>();

    TItem Aggregate(Func<TItem, TItem, TItem> aggregator);

    TOther Aggregate<TOther>(Func<TOther, TItem, TOther> aggregator, TOther seed);

    
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<TItem, TKey> keyGen, 
        Func<TItem, TValue> valGen) 
        where TKey : notnull =>
        ToDictionary(keyGen, valGen,null);

    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<TItem, TKey> keyGen,
        Func<TItem, TValue> valGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull;

    public Dictionary<TKey, TItem> ToDictionary<TKey>(
        Func<TItem, TKey> keyGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull
        => ToDictionary(keyGen, x => x, comp);
    public Dictionary<TKey, TItem> ToDictionary<TKey>(
        Func<TItem, TKey> keyGen)
        where TKey : notnull
        => ToDictionary(keyGen, x => x, null);
    
    
    
    
    SelectManyIterator<TSelf, TInner, TItem, TOut> SelectMany<TInner, TOut>(
        Func<TItem, TInner> flattener) where TInner : IEnumerator<TOut>, allows ref struct ;

    SelectManyIterator<TSelf, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, TOut[]> flattener);

    SelectManyIterator<TSelf, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, List<TOut>> flattener);

    SelectManyIterator<TSelf, IEnumerator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, IEnumerable<TOut>> flattener);

}