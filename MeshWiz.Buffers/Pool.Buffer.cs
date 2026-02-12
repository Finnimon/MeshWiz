using System.Buffers;
using System.Runtime.CompilerServices;

namespace MeshWiz.Buffers;

public static partial class Pool
{
    public readonly ref struct Buffer<T>
    {
        private readonly nuint[] _words;
        public readonly Span<T> Span;
        
        public Buffer()
        {
            _words = [];
            Span = [];
        }
        internal Buffer(nuint[] words)
        {
            _words = words;
            Span = Utilities.UnsafeCast<T>(words);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_words.Length == 0) return;
            ArrayPool<nuint>.Shared.Return(_words, clearArray: false);
        }
    }
}