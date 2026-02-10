using System.Buffers;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace MeshWiz.Buffers;

public static partial class Pool
{
    // private const int Chunks=31;
    // [ThreadStatic] private static IntPtr[]?[]? _arrays;
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // private static nint[]?[] GetArrays() => _arrays ??= new nint[31][];

    [MustUseReturnValue, MustDisposeResource, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buffer<T> Rent<T>(int minimumLength)
    {
        var minWordLen = Utilities.GetWordCount<T>(minimumLength);
        return new Buffer<T>(ArrayPool<nuint>.Shared.Rent(minWordLen));
    }
}