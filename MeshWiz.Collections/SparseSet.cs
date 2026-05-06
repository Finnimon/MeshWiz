using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Collections;

/// <summary>
/// Only beneficial for either large <typeparamref name="T"/> and or known sparse capacity to prevent resizes.
/// Otherwise prefer <see cref="Dictionary{TKey,TValue}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SparseSet<T> : IReadOnlyList<T>, IReadOnlyDictionary<int, T>, IList<T>
    where T : struct
{
    /// <inheritdoc />
    public bool Remove(T item)
    {
        var index = _dense.AsSpan(0, Count).IndexOf(item);
        if (index == -1) return false;
        RemoveAt(_denseMappers[index]);
        return true;
    }

    public int Count
    {
        get;
        private set
        {
            Debug.Assert(value >= 0);
            field = value;
            if (DenseCapacity > (uint)field)
                return;

            DenseCapacity = int.Max(field, DenseCapacity * 2);
        }
    }

    /// <inheritdoc />
    bool ICollection<T>.IsReadOnly => false;

    public int SparseCapacity
    {
        get => _sparse.Length;
        set
        {
            unchecked
            {
                ++_version;
            }

            if (value == _sparse.Length) return;
            if (_sparse.Length == 0) _sparse = new int[value];
            else Array.Resize(ref _sparse, value);
        }
    }

    public const int DefaultInitialDenseCapacity = 4;
    public const int DefaultInitialSparseCapacity = 128;

    public SparseSet(int sparseCap, int denseCap)
    {
        _sparse = new int[sparseCap];
        _dense = new T[denseCap];
        _denseMappers = new int[denseCap];
    }

    public SparseSet() : this(DefaultInitialSparseCapacity, DefaultInitialDenseCapacity) { }

    public int DenseCapacity
    {
        get => _dense.Length;
        private set
        {
            unchecked
            {
                ++_version;
            }

            if (value == 0)
            {
                _dense = [];
                _denseMappers = [];
            }
            else if (DenseCapacity == 0)
            {
                _dense = new T[value];
                _denseMappers = new int[value];
            }
            else
            {
                Array.Resize(ref _dense, value);
                Array.Resize(ref _denseMappers, value);
            }
        }
    }

    /// <inheritdoc />
    IEnumerable<int> IReadOnlyDictionary<int, T>.Keys => Count == 0 ? [] : _denseMappers.Take(Count);

    /// <inheritdoc />
    IEnumerable<T> IReadOnlyDictionary<int, T>.Values => Count == 0 ? [] : _dense.Take(Count);

    private int[] _sparse;
    public ReadOnlySpan<int> Sparse => _sparse.AsSpan();
    private T[] _dense;
    private int[] _denseMappers;
    private int _version;

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        if (Count == 0) return -1;
        var comp = EqualityComparer<T>.Default;
        for (var i = 0; i < Count; i++)
        {
            if (!comp.Equals(_dense[i], item)) continue;
            return _denseMappers[i];
        }

        return -1;
    }

    /// <summary>meaningless for a sparse set</summary>
    void IList<T>.Insert(int index, T item) => this[index] = item;

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        var dense = _sparse[index];
        ArgumentOutOfRangeException.ThrowIfEqual(dense, 0, nameof(index));
        unchecked
        {
            ++_version;
        }

        Count--;
        if (index == Count)
        {
            _dense[Count] = default!;
            _denseMappers[Count] = 0;
            _sparse[index] = 0;
            return;
        }

        _dense[dense - 1] = _dense[Count];
        _denseMappers[dense - 1] = _denseMappers[Count];
        _sparse[_denseMappers[Count]] = 0;
        _dense[Count] = default!;
    }

    public T this[int index]
    {
        get
        {
            var uIndex = (uint)index;
            if (SparseCapacity <= uIndex) IndexThrowHelper.Throw();
            var denseId = _sparse[index];
            if (denseId == 0) IndexThrowHelper.Throw();
            return _dense[denseId - 1];
        }
        set
        {
            if (index < 0) IndexThrowHelper.Throw();
            if (SparseCapacity <= index)
                SparseCapacity = SparseCapacity * 2 > index ? SparseCapacity * 2 : index.NextPow2();
            ref var knownIndex = ref _sparse[index];
            if (knownIndex == 0) knownIndex = ++Count;
            var realIndex = knownIndex - 1;
            unchecked
            {
                ++_version;
            }

            _denseMappers[realIndex] = index;
            _dense[realIndex] = value;
        }
    }


    public void Add(T item) => this[Count] = item;

    public bool Add(int index, T item)
    {
        if (ContainsKey(index)) return false;
        this[index] = item;
        return true;
    }

    public T GetOrAdd(int index, Func<int, T> orElse)
    {
        if (index < 0) IndexThrowHelper.Throw();
        if (SparseCapacity <= index)
            SparseCapacity = SparseCapacity * 2 > index ? SparseCapacity * 2 : index.NextPow2();
        ref var knownIndex = ref _sparse[index];
        var isSet = knownIndex != 0;
        if (isSet)
            return _dense[knownIndex - 1];
        knownIndex = ++Count;
        var realIndex = knownIndex - 1;
        unchecked
        {
            ++_version;
        }

        _denseMappers[realIndex] = index;
        return _dense[realIndex] = orElse(index);
    }

    public bool ContainsKey(int index) => SparseCapacity > (uint)index && _sparse[index] != 0;

    public bool TryGetValue(int index, out T value)
    {
        value = default;
        if (SparseCapacity <= (uint)index)
            return false;
        var denseIndex = _sparse[index];
        if (denseIndex == 0) return false;
        value = _dense[denseIndex - 1];
        return true;
    }

    public Enumerator GetEnumerator() => new(this);

    public T[] ToArray() => Values.ToArray();
    public T[] ToArrayOrdered() => _sparse.Where(i => i != 0).Select(i => _dense[i - 1]).Take(Count).ToArray();

    public void Clear()
    {
        if (Count == 0) return;
        _dense.AsSpan(0, Count).Clear();
        Array.Clear(_sparse);
        Count = 0;
    }

    public ReadOnlySpan<T> Values =>
        MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(_dense), Count);

    public ReadOnlySpan<int> Keys =>
        MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(_denseMappers), Count);

    /// <inheritdoc />
    public bool Contains(T item) => Count != 0 && Values.Contains(item);

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex) => Values.CopyTo(array.AsSpan(arrayIndex));

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    public IEnumerable<KeyValuePair<int, T>> EnumeratePairs() =>
        Count == 0 ? [] : Enumerable.Range(0, Count).Select(i => new KeyValuePair<int, T>(_denseMappers[i], _dense[i]));

    IEnumerator<KeyValuePair<int, T>> IEnumerable<KeyValuePair<int, T>>.GetEnumerator() =>
        EnumeratePairs().GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly SparseSet<T> _set;
        private readonly int _version;
        private int _pos = -1;

        internal Enumerator(SparseSet<T> set)
        {
            _set = set;
            _version = _set._version;
            Reset();
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_set._version != _version)
                ThrowHelper.ThrowInvalidOperationException();
            return ++_pos < (uint)_set.Count;
        }

        /// <inheritdoc />
        public void Reset() => _pos = -1;

        /// <inheritdoc />
        public T Current
        {
            get
            {
                if (_version != _set._version) ThrowHelper.ThrowInvalidOperationException();
                return _set._dense[_pos];
            }
        }

        /// <inheritdoc />
        object? IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() { }
    }
}