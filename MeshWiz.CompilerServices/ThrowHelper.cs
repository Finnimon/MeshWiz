using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MeshWiz.CompilerServices;

#pragma warning disable CS8500
internal static class ThrowHelper
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static void Index() => throw new IndexOutOfRangeException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static T NotSupported<T>() => throw new NotSupportedException();
}