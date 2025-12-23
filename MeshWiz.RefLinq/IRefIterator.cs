using System.Diagnostics.CodeAnalysis;
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
    WhereIterator<TSelf, TItem> Where(Func<TItem, bool> predicate);
    SelectIterator<TSelf, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector);
    RangeIterator<TSelf, TItem> Take(Range r);
    RangeIterator<TSelf, TItem> Take(int num);
    RangeIterator<TSelf, TItem> Skip(int num);


    TItem[] ToArray();
    List<TItem> ToList();
    HashSet<TItem> ToHashSet();
    HashSet<TItem> ToHashSet(IEqualityComparer<TItem>? comp);

    TItem First(Func<TItem, bool> predicate) => Where(predicate).First();
    TItem? FirstOrDefault(Func<TItem, bool> predicate) => Where(predicate).FirstOrDefault();

    TItem Last(Func<TItem, bool> predicate) => Where(predicate).Last();
    TItem? LastOrDefault(Func<TItem, bool> predicate) => Where(predicate).LastOrDefault();

    bool Any()
    {
        var copy = this;
        return copy.MoveNext();
    }

    bool Any(Func<TItem, bool> predicate) => Where(predicate).MoveNext();
    bool All(Func<TItem,bool> predicate)=>!Any(x=>!predicate(x));
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
        Func<TItem, TInner> flattener) where TInner : IRefIterator<TInner,TOut>, allows ref struct ;

    SelectManyIterator<TSelf, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, TOut[]> flattener);

    SelectManyIterator<TSelf, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, List<TOut>> flattener);

    SelectManyIterator<TSelf, AdapterIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, IEnumerable<TOut>> flattener);

    /// <summary>
    /// Try to take a range by constraining the source Span.
    /// Do not actually iterate
    /// </summary>
    bool TryTakeRange(Range r,[NotNullWhen(returnValue:true),AllowNull,MaybeNullWhen(returnValue:false)] out TSelf? result);

    DistinctIterator<TSelf, TItem> Distinct();
    DistinctIterator<TSelf, TItem> Distinct(IEqualityComparer<TItem>? comp);
    DistinctIterator<TSelf, TItem> DistinctBy<T>(Func<TItem, T> keySelector) where T : notnull;

    ConcatIterator<TSelf, TOther, TItem> Concat<TOther>(TOther other)
        where TOther : IRefIterator<TOther, TItem>, allows ref struct;

    ConcatIterator<TSelf, ItemIterator<TItem>, TItem> Append(TItem append);
    ConcatIterator<ItemIterator<TItem>, TSelf, TItem> Prepend(TItem prepend);
    static abstract TSelf Empty();
}