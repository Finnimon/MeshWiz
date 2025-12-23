using System.Diagnostics.CodeAnalysis;

namespace MeshWiz.Utility;

public static class Equality
{
    private sealed record ByComparer<T, TKey>(Func<T, TKey> KeySel) : IEqualityComparer<T>
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

    public static IEqualityComparer<T> By<T, TKey>(Func<T, TKey> keySelector) where TKey : notnull 
        => new ByComparer<T, TKey>(keySelector);
}