using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.Collections;

public ref struct SpanStack<T>
{
    private readonly Span<T> _span;
    private int _count;
    public readonly int Count => _count;
    public readonly int Capacity => _span.Length;

    public SpanStack(Span<T> span) => _span = span;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SpanStack<T>(Span<T> span) => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPop([AllowNull, MaybeNull] out T result)
    {
        Unsafe.SkipInit(out result);
        if (_count == 0) return false;
        result = _span[--_count];
        return true;
    }

    public T this[int index]
    {
        get
        {
            if (_count <= (uint)index) IndexThrowHelper.Throw();
            return _span[index];
        }
        set
        {
            if (_count <= (uint)index) IndexThrowHelper.Throw();
            _span[index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(T value) => _span[_count++] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop()
    {
        if (_count == 0) ThrowHelper.ThrowInvalidOperationException();
        return _span[--_count];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        var count = _count;
        _count = 0;
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>()) return;
        _span.Slice(0, count).Clear();
    }
}