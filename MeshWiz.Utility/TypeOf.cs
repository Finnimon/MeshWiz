namespace MeshWiz.Utility;

public static class TypeOf<T>
{
    public static readonly bool Unmanaged = 
        Func.Try<Type[], Type>(typeof(TypeOfHelpers.Unmanaged<>).MakeGenericType, [typeof(T)]).IsSuccess;
}

internal static class TypeOfHelpers
{
    public static class Unmanaged<T>
    where T : unmanaged;
}