using System.Numerics;

namespace MeshWiz.UpToDate;

public sealed class FloatingPointScopedProperty<T>(IUpToDate scope, T initialValue, T delta)
    where T : IFloatingPoint<T>
{
    public IUpToDate Scope { get; } = scope;
    public readonly T Delta = T.Abs(delta);
    public static implicit operator T(FloatingPointScopedProperty<T> property)=>property.Value;
    public T Value
    {
        get;
        set
        {
            if (AreEqual(value, field)) return;
            field = value;
            Scope.OutOfDate();
        }
    } = initialValue;

    private bool AreEqual(T? obj1, T? obj2)
    {
        if (obj1 is null) return obj2 is null;
        if (obj2 is null) return false;
        return T.Abs(obj1 - obj2) < Delta;
    }
}