using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    public ref struct Buffer<T>
    {
        internal readonly Freelist? _allocator;
        internal readonly nuint[] _src;
        public readonly Span<T> Span;
        internal readonly int _wordStart;
        internal readonly int _wordCount;
        internal bool _alive;
        public readonly bool Alive => _alive;
        public readonly int WordCount => _wordCount;    

        public readonly bool TryGetSpan(out Span<T> span)
        {
            span = Span;
            return _alive;
        }
        
        internal Buffer(int wordStart, Span<T> span, Freelist? allocator, nuint[] src, int wordCount)
        {
            _alive = true;
            _wordStart = wordStart;
            Span = span;
            _allocator = allocator;
            _src = src;
            _wordCount = wordCount;
        }

        internal Buffer(bool alive, int wordStart, Span<T> span, Freelist? allocator, nuint[] src,  int wordCount)
        {
            _alive = alive;
            _wordStart = wordStart; 
            Span = span;
            _allocator = allocator;
            _src = src;
            _wordCount = wordCount;
        }
        
        public void Dispose()
        {
            if (!_alive) return;
            _alive = false;
            _allocator?.Release(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining),System.Diagnostics.Contracts.Pure]
        internal static Buffer<T> FromWordBuf(Buffer<nuint> underlying, int length)
        {
            var reinterpret = MemoryMarshal.CreateSpan(ref Unsafe.As<nuint,T>(ref MemoryMarshal.GetReference(underlying.Span)),length);
            return new Buffer<T>(
                underlying._wordStart,
                reinterpret,
                underlying._allocator,
                underlying._src,
                underlying._wordCount
            );
        }
    }
}