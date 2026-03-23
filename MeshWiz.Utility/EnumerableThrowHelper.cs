using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility;

[StackTraceHidden]
public static class EnumerableThrowHelper
{
    /// <exception cref="InvalidOperationException"></exception>
    [StackTraceHidden]
    [DoesNotReturn,MethodImpl(MethodImplOptions.NoInlining)]
    public static void NoElements()=>throw new InvalidOperationException("Sequence contains no elements");
    [StackTraceHidden]
    [DoesNotReturn,MethodImpl(MethodImplOptions.NoInlining)]
    public static T NoElements<T>()=>throw new InvalidOperationException("Sequence contains no elements");

}