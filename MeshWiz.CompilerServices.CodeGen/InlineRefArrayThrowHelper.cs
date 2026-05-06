using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MeshWiz.CompilerServices.CodeGen;

public static class InlineRefArrayThrowHelper
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static void Index() => throw new IndexOutOfRangeException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static T NotSupported<T>() => throw new NotSupportedException();
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static void NotSupported() => throw new NotSupportedException();
}