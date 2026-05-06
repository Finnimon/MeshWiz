using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MeshWiz.Utility;

public static class Equality
{
    public readonly struct ByEqComparer<T, TKey>(Func<T, TKey> keySel) : IEqualityComparer<T>
        where TKey : notnull
    {
        /// <inheritdoc />
        public bool Equals(T? x, T? y)
        {
            var xNull = x is null;
            var yNull = y is null;

            return xNull && yNull
                   || !yNull && keySel(x!).Equals(keySel(y!));
        }

        /// <inheritdoc />
        public int GetHashCode([DisallowNull] T obj) => keySel(obj).GetHashCode();
    }

    public readonly struct ByComparer<T, TKey>(Func<T, TKey> keySel) : IComparer<T>
        where TKey : IComparable<TKey>
    {
        public int Compare(T? x, T? y)
        {
            if (x == null)
                return y != null ? -1 : 0;
            if (y == null)
                return 1;
            return keySel(x).CompareTo(keySel(y));
        }
    }

    public static ByEqComparer<T, TKey> By<T, TKey>(Func<T, TKey> keySelector) where TKey : notnull => new(keySelector);
    
    public static ByEqComparer<T, T> By<T>(Func<T, T> keySelector) where T : notnull => new(keySelector);

    public static ByComparer<T, TKey> CompareBy<T, TKey>(Func<T, TKey> keySelector) 
        where TKey : IComparable<TKey> => new(keySelector);
    
    public static ByComparer<T, T> CompareBy<T>(Func<T, T> keySelector) 
        where T : IComparable<T> => new(keySelector);
}