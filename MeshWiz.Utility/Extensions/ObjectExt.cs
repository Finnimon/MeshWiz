using System.Runtime.CompilerServices;

namespace MeshWiz.Utility.Extensions;

public static class ObjectExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T DeferredSet<T>(ref this T field, T val)
    where T:struct
    {
        var copy = field;
        field = val;
        return copy;
    }
}