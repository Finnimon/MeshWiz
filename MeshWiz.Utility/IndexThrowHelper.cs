using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility;

[StackTraceHidden]
public static class IndexThrowHelper
{
    [DoesNotReturn,MethodImpl(MethodImplOptions.NoInlining)]
    public static  void Throw() => throw new IndexOutOfRangeException();

    [DoesNotReturn,MethodImpl(MethodImplOptions.NoInlining)]
    public static T Throw<T>() => throw new IndexOutOfRangeException();


    [DoesNotReturn,MethodImpl(MethodImplOptions.NoInlining)]
    public static T Throw<T>(int index, int count)
    {
        Throw(index, count);
        return default!;
    }

    [DoesNotReturn,MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw(int index,int count) => Throw($"Index {index} must be less than {count}");

    [DoesNotReturn,MethodImpl(MethodImplOptions.NoInlining)]
    public static T Throw<T>(string msg)
    {
        Throw(msg);
        return default!;
    }
    
    [DoesNotReturn,MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw(string msg) => throw new IndexOutOfRangeException(msg);
    
}