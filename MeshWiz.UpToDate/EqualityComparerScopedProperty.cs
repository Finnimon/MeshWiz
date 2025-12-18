namespace MeshWiz.UpToDate;

public sealed class EqualityComparerScopedProperty<T>(IUpToDate scope, T initialValue, IEqualityComparer<T> comparer)
    : IScopedProperty<T>
{
    public readonly IEqualityComparer<T> EqualityComparer = comparer;
    public IUpToDate Scope { get; } = scope;

    public T Value
    {
        get;
        set
        {
            if (EqualityComparer.Equals(value, field)) return;
            field = value;
            Scope.OutOfDate();
        }
    } = initialValue;
}