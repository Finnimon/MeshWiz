using System.Diagnostics.CodeAnalysis;

namespace MeshWiz.Utility;

public static class Equality
{
    private sealed record ByEqComparer<T, TKey>(Func<T, TKey> KeySel) : IEqualityComparer<T>
        where TKey : notnull
    {
        /// <inheritdoc />
        public bool Equals(T? x, T? y)
        {
            var xNull = x is null;
            var yNull = y is null;

            return xNull && yNull 
                   || !yNull && KeySel.Invoke(x!).Equals(KeySel.Invoke(y!));
        }

        /// <inheritdoc />
        public int GetHashCode([DisallowNull] T obj) => KeySel(obj).GetHashCode();
    }

    private sealed record ByComparer<T, TKey>(Func<T, TKey> KeySel) : IComparer<T>
        where TKey : IComparable<TKey>
    {
        public int Compare(T? x, T? y)
        {
            if (x == null)
                return y != null ? -1 : 0;
            if (y == null)
                return 1;
            return KeySel(x).CompareTo(KeySel(y));
        }
    }

    public static IEqualityComparer<T> By<T, TKey>(Func<T, TKey> keySelector) where TKey : notnull 
        => new ByEqComparer<T, TKey>(keySelector);

    public static IComparer<T> CompareBy<T, TKey>(Func<T, TKey> keySelector)
    where TKey: IComparable<TKey> =>
        new ByComparer<T, TKey>(keySelector);
}