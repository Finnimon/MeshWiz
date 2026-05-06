// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Modifications:
// Made sorting weak and mod Keys aswell as namespace moving and naming changes

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Buffers;

public partial class Freelist
{
    // The SortedDictionary class implements a generic sorted list of keys
    // and values. Entries in a sorted list are sorted by their keys and
    // are accessible both by key and by index. The keys of a sorted dictionary
    // can be ordered either according to a specific IComparer
    // implementation given when the sorted dictionary is instantiated, or
    // according to the IComparable implementation provided by the keys
    // themselves. In either case, a sorted dictionary does not allow entries
    // with duplicate or null keys.
    //
    // A sorted list internally maintains two arrays that store the keys and
    // values of the entries. The capacity of a sorted list is the allocated
    // length of these internal arrays. As elements are added to a sorted list, the
    // capacity of the sorted list is automatically increased as required by
    // reallocating the internal arrays.  The capacity is never automatically
    // decreased, but users can call either TrimExcess or
    // Capacity explicitly.
    //
    // The GetKeyList and GetValueList methods of a sorted list
    // provides access to the keys and values of the sorted list in the form of
    // List implementations. The List objects returned by these
    // methods are aliases for the underlying sorted list, so modifications
    // made to those lists are directly reflected in the sorted list, and vice
    // versa.
    //
    // The SortedList class provides a convenient way to create a sorted
    // copy of another dictionary, such as a Hashtable. For example:
    //
    // Hashtable h = new Hashtable();
    // h.Add(...);
    // h.Add(...);
    // ...
    // SortedList s = new SortedList(h);
    //
    // The last line above creates a sorted list that contains a copy of the keys
    // and values stored in the hashtable. In this particular example, the keys
    // will be ordered according to the IComparable interface, which they
    // all must implement. To impose a different ordering, SortedList also
    // has a constructor that allows a specific IComparer implementation to
    // be specified.
    //
    private sealed class WeakSortedList
    {
        private int[] keys; // Do not rename (binary serialization)
        private int[] values; // Do not rename (binary serialization)
        internal int Count; // Do not rename (binary serialization)
        private readonly Comparer<int> comparer; // Do not rename (binary serialization)

        private const int DefaultCapacity = 4;

        // Constructs a new sorted list. The sorted list is initially empty and has
        // a capacity of zero. Upon adding the first element to the sorted list the
        // capacity is increased to DefaultCapacity, and then increased in multiples of two as
        // required. The elements of the sorted list are ordered according to the
        // IComparable interface, which must be implemented by the keys of
        // all entries added to the sorted list.
        public WeakSortedList()
        {
            keys = [];
            values = [];
            Count = 0;
            comparer = Comparer<int>.Default;
        }

        // Constructs a new sorted list. The sorted list is initially empty and has
        // a capacity of zero. Upon adding the first element to the sorted list the
        // capacity is increased to 16, and then increased in multiples of two as
        // required. The elements of the sorted list are ordered according to the
        // IComparable interface, which must be implemented by the keys of
        // all entries added to the sorted list.
        //
        public WeakSortedList(int capacity)
        {
            if (capacity != 0)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(capacity);
                keys = GC.AllocateUninitializedArray<int>(capacity);
                values = GC.AllocateUninitializedArray<int>(capacity);
            }
            else
            {
                keys = [];
                values = [];
            }

            comparer = Comparer<int>.Default;
        }

        // Adds an entry with the given key and value to this sorted list. An
        // ArgumentException is thrown if the key is already present in the sorted list.
        //
        public void Add(int key, int value)
        {
            int i = Array.BinarySearch(keys, 0, Count, key, comparer);
            if (i >= 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(key));

            Insert(~i, key, value);
        }


        // Returns the capacity of this sorted list. The capacity of a sorted list
        // represents the allocated length of the internal arrays used to store the
        // keys and values of the list, and thus also indicates the maximum number
        // of entries the list can contain before a reallocation of the internal
        // arrays is required.
        //
        public int Capacity
        {
            get => keys.Length;
            set
            {
                if (value == keys.Length) return;
                if (value < Count) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value));

                if (value > 0)
                {
                    var newKeys = GC.AllocateUninitializedArray<int>(value);
                    var newValues = GC.AllocateUninitializedArray<int>(value);
                    if (Count > 0)
                    {
                        Keys.CopyTo(newKeys);
                        Values.CopyTo(newValues);
                    }

                    keys = newKeys;
                    values = newValues;
                }
                else
                {
                    keys = [];
                    values = [];
                }
            }
        }

        public unsafe Span<int> Keys => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(keys), Count);

        public unsafe Span<int> Values =>
            MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(values), Count);

        // Removes all entries from this sorted list.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Count = 0;


        // Checks if this sorted list contains an entry with the given key.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(int key)
        {
            return IndexOfKey(key) >= 0;
        }

        // Checks if this sorted list contains an entry with the given value. The
        // values of the entries of the sorted list are compared to the given value
        // using the Object.Equals method. This method performs a linear
        // search and is substantially slower than the Contains
        // method.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsValue(int value) => IndexOfValue(value) >= 0;

        // Ensures that the capacity of this sorted list is at least the given
        // minimum value. The capacity is increased to twice the current capacity
        // or to min, whichever is larger.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureCapacity(int min)
        {
            int newCapacity = keys.Length == 0 ? DefaultCapacity : keys.Length * 2;
            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;
            if (newCapacity < min) newCapacity = min;
            Capacity = newCapacity;
        }

        /// <summary>
        /// Gets the value corresponding to the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the value within the entire <see cref="SortedList{TKey,TValue}"/>.</param>
        /// <returns>The value corresponding to the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified index was out of range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValueAtIndex(int index) => values[index];

        /// <summary>
        /// Updates the value corresponding to the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the value within the entire <see cref="SortedList{TKey,TValue}"/>.</param>
        /// <param name="value">The value with which to replace the entry at the specified index.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified index was out of range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValueAtIndex(int index, int value) => values[index] = value;

        /// <summary>
        /// Gets the key corresponding to the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the key within the entire <see cref="SortedList{TKey,TValue}"/>.</param>
        /// <returns>The key corresponding to the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified index is out of range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetKeyAtIndex(int index) => keys[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<int, int> GetEntryAtIndex(int index) => new(keys[index], values[index]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetKeyAtIndex(int index, int key) => keys[index] = key;

        // Returns the value associated with the given key. If an entry with the
        // given key is not found, the returned value is null.
        public int this[int key]
        {
            get
            {
                int i = IndexOfKey(key);
                if (i >= 0)
                    return values[i];

                return ThrowHelper.ThrowArgumentException<int>(nameof(key));
            }
            set
            {
                var i = Array.BinarySearch(keys, 0, Count, key, comparer);
                if (i >= 0)
                {
                    values[i] = value;
                    return;
                }

                Insert(~i, key, value);
            }
        }

        // Returns the index of the entry with a given key in this sorted list. The
        // key is located through a binary search, and thus the average execution
        // time of this method is proportional to Log2(size), where
        // size is the size of this sorted list. The returned value is -1 if
        // the given key does not occur in this sorted list. Null is an invalid
        // key value.
        public int IndexOfKey(int key)
        {
            if (Count == 1) return keys[0] == key ? 0 : -1;
            var ret = Array.BinarySearch(keys, 0, Count, key, comparer);
            return ret >= 0 ? ret : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfValue(int value)
        {
            if (Count == 1) return values[0] == value ? 0 : -1;
            var ret = Array.BinarySearch(values, 0, Count, value, comparer);
            return ret >= 0 ? ret : -1;
        }

        // Inserts an entry with a given key and value at a given index.
        public void Insert(int index, int key, int value)
        {
            if (Count == keys.Length) EnsureCapacity(Count + 1);
            if (index < Count)
            {
                Array.Copy(keys, index, keys, index + 1, Count - index);
                Array.Copy(values, index, values, index + 1, Count - index);
            }

            keys[index] = key;
            values[index] = value;
        }

        // Removes the entry at the given index. The size of the sorted list is
        // decreased by one.
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
            Count--;
            if (index < Count)
            {
                Array.Copy(keys, index + 1, keys, index, Count - index);
                Array.Copy(values, index + 1, values, index, Count - index);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddAssumeOrdered(int key, int value)
        {
            var idx = Count++;
            if (idx == keys.Length) EnsureCapacity(Count);
            keys[idx] = key;
            values[idx] = value;
        }
    }
}