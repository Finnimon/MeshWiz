namespace MeshWiz.UpToDate;

public sealed class ReferenceScopedProperty<T>(IUpToDate scope, T initialValue) : IScopedProperty<T>
    where T : class
{
    public IUpToDate Scope { get; } = scope;

    public T Value
    {
        get;
        set
        {
            if(ReferenceEquals(value, field)) return;
            field = value;
            Scope.OutOfDate();
        }
    } = initialValue;

}