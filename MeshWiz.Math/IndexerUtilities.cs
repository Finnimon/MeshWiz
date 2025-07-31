namespace MeshWiz.Math;


public static class IndexerUtilities
{
    public static int GetIndex<TElement>(TElement vec,
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