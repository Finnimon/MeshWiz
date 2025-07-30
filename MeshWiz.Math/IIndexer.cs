namespace MeshWiz.Math;

public interface IIndexer<out TExtract,in TSource>
{
    public static abstract int IndexCount { get; }
    public TExtract Extract(IReadOnlyList<TSource> from);
}

public static class IndexerUtilities
{
    private static int GetIndex<TElement>(TElement vec,
        Dictionary<TElement, int> unified,
        List<TElement> elements) 
        where TElement : notnull
    {
        if (unified.TryGetValue(vec, out var index)) return index;
        index = elements.Count;
        unified.Add(vec, index);
        elements.Add(vec);
        return index;
    }
}