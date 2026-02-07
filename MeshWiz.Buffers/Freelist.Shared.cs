using System.Diagnostics.CodeAnalysis;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    [field: ThreadStatic, AllowNull, MaybeNull]
    public static Freelist Shared => field ??= new Freelist(Allocator.InitialSharedCapacity, false);
    // public sealed partial class Shared
    // {
    //     [field: ThreadStatic, AllowNull, MaybeNull]
    //     private static Freelist Local => field ??= new Freelist(Allocator.InitialLocalCapacity, false);
    //
    //
    //     [MustDisposeResource, MustUseReturnValue, MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     [SuppressMessage("ReSharper", "NotDisposedResource")]
    //     public static Freelist.Buffer<T> Rent<T>(int length)
    //     {
    //         // ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));
    //
    //         // if (length == 0) return new Buffer<T>(EmptyBuffer<T>());
    //         var local = Local;
    //             return local.Rent<T>(length);
    //         
    //     }
    // }
}