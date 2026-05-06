using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Buffers;

public static partial class Pool
{
    public readonly ref struct Buffer<T>
    {
        private readonly Array _words;
        public readonly Span<T> Span;
        
        public Buffer()
        {
            _words = Array.Empty<T>();
            Span = [];
        }
        internal Buffer(UInt128[] words)
        {
            _words = words;
            Span = Utilities.UnsafeCast<T>(words);
        }

        internal Buffer(object[] references)
        {
            _words = references;
            Span = Utilities.UnsafeCast<T>(references);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_words.Length == 0) return;
            if (typeof(T).IsValueType)
                ArrayPool<UInt128>.Shared.Return((UInt128[])_words, clearArray: false);
            else
                ArrayPool<object>.Shared.Return((object[])_words, true);
        }
    }
}