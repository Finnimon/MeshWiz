using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    [DebuggerDisplay("Alive = {Alive}, Size = {Span.Length}")]
    public ref struct Buffer<T>
    {
        internal readonly Freelist _allocator;
        internal readonly object _src;
        public readonly Span<T> Span;
        internal readonly int _wordStart;
        internal readonly int _wordCount;
        internal bool _alive;
        public readonly bool Alive => _alive;

        public readonly bool TryGetAliveSpan(out Span<T> span)
        {
            span = Span;
            return _alive;
        }
        
        internal Buffer(int wordStart, Span<T> span, Freelist allocator, Array src, int wordCount)
        {
            _alive = true;
            _wordStart = wordStart;
            Span = span;
            _allocator = allocator;
            _src = src;
            _wordCount = wordCount;
        }

        internal Buffer(bool alive, int wordStart, Span<T> span, Freelist allocator, Array src,  int wordCount)
        {
            _alive = alive;
            _wordStart = wordStart; 
            Span = span;
            _allocator = allocator;
            _src = src;
            _wordCount = wordCount;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!_alive) return;
            _alive = false;
            if(_wordCount!=0) _allocator.Release(this);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining),System.Diagnostics.Contracts.Pure]
        internal static Buffer<T> FromWordBuf(Buffer<UInt128> underlying)
        {
            var reinterpret = Utilities.UnsafeCast<T>(underlying.Span);
            var buf=Unsafe.As<Buffer<UInt128>,Buffer<T>>(ref underlying);
            Unsafe.AsRef(in buf.Span)=reinterpret;
            return buf;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining),System.Diagnostics.Contracts.Pure]
        internal static Buffer<T> FromObjectBuf(Buffer<object> underlying) => Unsafe.As<Buffer<object>,Buffer<T>>(ref underlying);
    }
}