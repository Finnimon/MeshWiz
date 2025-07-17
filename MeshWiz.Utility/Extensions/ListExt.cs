using System.Collections;

namespace MeshWiz.Utility.Extensions;

public static class ListExt
{
    public static IReadOnlyList<T> SliceChecked<T>(this IReadOnlyList<T> source, int start, int length)
    {
        ValidateSlice(start, length, source.Count);
        return new ReadOnlyListSliceView<T>(source,start,length);
    }
    // public static IReadOnlyList<T> SliceChecked<T>(this IList<T> source, int start, int length)
    // {
    //     ValidateSlice(start, length, source.Count);
    //     return new ListSliceView<T>(source,start,length);
    // }
    
    // public static IReadOnlyList<T> SliceTruncating<T>(this IList<T> source, int start, int length)
    // {
    //     if (source.Count==0) return Array.Empty<T>();
    //     
    //     start= int.Clamp(start, 0, source.Count-1);
    //     length=int.Clamp(length,0,source.Count-start);
    //     return new ListSliceView<T>(source,start,length);
    // }
    
    public static IReadOnlyList<T> SliceTruncating<T>(this IReadOnlyList<T> source, int start, int length)
    {
        if (source.Count==0) return Array.Empty<T>();
        
        start= int.Clamp(start, 0, source.Count-1);
        length=int.Clamp(length,0,source.Count-start);
        return new ReadOnlyListSliceView<T>(source,start,length);
    }
    

    private static void ValidateSlice(int start, int sliceLength, int sourceCount)
    {
        if (sliceLength < 0) 
            throw new ArgumentOutOfRangeException(
                nameof(sliceLength), 
                sliceLength, 
                $"Slicelength must be >=0");
        if(start<0||start>=sourceCount) 
            throw new ArgumentOutOfRangeException(
                nameof(start),
                start,
                $"Slice start must be >=0 and <sourceCount={sourceCount}");
        var lastValidIndex = start + sliceLength-1;
        if (lastValidIndex >= sourceCount)
            throw new ArgumentOutOfRangeException(
                nameof(sourceCount),
                sourceCount,
                $"Source count must be at least {lastValidIndex+1} to support start={start} and slice length={sliceLength}");
    }
    private sealed record ListSliceView<T>(IList<T> Source, int Start, int Count) : IReadOnlyList<T>
    {
        private int LastIndex => Count - 1;
        private int LastSourceIndex => Start + Count - 1;

        public IEnumerator<T> GetEnumerator()
        {
            var lastIndex = LastSourceIndex;
            for (int i = Start; i < LastSourceIndex; i++)
                yield return Source[i];
        }


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T this[int index]
            => index.InsideInclusiveRange(0, LastIndex)
                ? Source[index + Start]
                : throw new IndexOutOfRangeException();
    }
    
    private sealed record ReadOnlyListSliceView<T>(IReadOnlyList<T> Source, int Start, int Count) : IReadOnlyList<T>
    {
        private int LastIndex => Count - 1;
        private int LastSourceIndex => Start + Count - 1;

        public IEnumerator<T> GetEnumerator()
        {
            var lastIndex = LastSourceIndex;
            for (int i = Start; i < LastSourceIndex; i++)
                yield return Source[i];
        }


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T this[int index]
            => index.InsideInclusiveRange(0, LastIndex)
                ? Source[index + Start]
                : throw new IndexOutOfRangeException();
    }
}