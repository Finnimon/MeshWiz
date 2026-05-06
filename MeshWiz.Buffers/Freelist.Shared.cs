using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    public static class Shared
    {
        [field: ThreadStatic, AllowNull, MaybeNull]
        private static Freelist SharedFreeList => field ??= new Freelist(Allocator.InitialSharedCapacity, false);

        [MustDisposeResource, MustUseReturnValue, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Buffer<T> Rent<T>(int minimumLength) => SharedFreeList.Rent<T>(minimumLength);
    }
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