using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MeshWiz.Utility;

[StackTraceHidden]
public static class IndexThrowHelper
{
    [DoesNotReturn]
    public static  void Throw() => throw new IndexOutOfRangeException();

    [DoesNotReturn]
    public static T Throw<T>() => throw new IndexOutOfRangeException();


    [DoesNotReturn]
    public static T Throw<T>(int index, int count)
    {
        Throw(index, count);
        return default!;
    }

    [DoesNotReturn]
    public static void Throw(int index,int count) => Throw($"Index {index} must be less than {count}");

    [DoesNotReturn]
    public static T Throw<T>(string msg)
    {
        Throw(msg);
        return default!;
    }
    
    [DoesNotReturn]
    public static void Throw(string msg) => throw new IndexOutOfRangeException(msg);
    
}