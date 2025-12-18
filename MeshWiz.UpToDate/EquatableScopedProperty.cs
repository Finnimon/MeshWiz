namespace MeshWiz.UpToDate;

public sealed class EquatableScopedProperty<T>(IUpToDate scope, T initiaValue) : IScopedProperty<T>
    where T : IEquatable<T>
{
    public IUpToDate Scope { get; } = scope;
    public static implicit operator T(EquatableScopedProperty<T> property)=>property.Value;
    public T Value
    {
        get;
        set
        {
            if (AreEqual(value, field)) return;
            field = value;
            Scope.OutOfDate();
        }
    } = initiaValue;

    private static bool AreEqual(T? obj1, T? obj2)
    {
        if (obj1 is null) return obj2 is null;
        return obj1.Equals(obj2);
    }
}