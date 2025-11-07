using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace MeshWiz.Utility.Extensions;

public static class ListExt
{
    [Pure]
    public static bool TryGet<T>(this IReadOnlyList<T> list, int index,
        [AllowNull, MaybeNullWhen(returnValue: false)] out T value)
    {
        var possible = list.Count > (uint)index;
        value=possible?list[index]:default;
        return possible;
    }
    public static int BinarySearch<TList, T>(this TList list, Func<T, int> comparer)
    where TList : IReadOnlyList<T>
    {
        if(list.Count == 0) return -1;
        
        var low = 0;
        var high = list.Count - 1;
        while (low<=high)
        {
            var mid = (low + high) / 2;
            var item = list[mid];
            var score = comparer(item);
            if (score == 0) return mid;
            if (score > 0) low = mid;
            else high = mid-1;
        }
        return -1;
    }
    
    public static int BinarySearch<T>(this ReadOnlySpan<T> list, Func<T, int> comparer)
    {
        if(list.Length == 0) return -1;
        var low = 0;
        var high = list.Length - 1;
        while (low<=high)
        {
            var mid = (low + high) / 2;
            var item = list[mid];
            var score = comparer(item);
            if (score == 0) return mid;
            if (score > 0) low = mid;
            else high = mid-1;
        }
        return -1;
    }
}