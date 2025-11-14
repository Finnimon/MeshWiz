using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility;

[Pure]
public static class Bool
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Once Once() => new();
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Alternator Alternator(bool initialValue) => new(initialValue);
}