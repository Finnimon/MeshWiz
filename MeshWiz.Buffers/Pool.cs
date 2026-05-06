using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using JetBrains.Annotations;

namespace MeshWiz.Buffers;

public static partial class Pool
{
    [MustUseReturnValue, MustDisposeResource, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buffer<T> Rent<T>(int minimumLength)
    {
        switch (minimumLength)
        {
            case 0:
                return new Buffer<T>();
            case < 0:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(minimumLength));
                break;
        }
        return typeof(T).IsValueType
            ? new Buffer<T>(ArrayPool<UInt128>.Shared.Rent(Utilities.GetWordCount<T>(minimumLength)))
            : new Buffer<T>(ArrayPool<object>.Shared.Rent(minimumLength));
    }
}