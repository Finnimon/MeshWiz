using System.Runtime.CompilerServices;

namespace MeshWiz.Buffers;

public static partial class ArrayBuilder
{
    [InlineArray(ScratchBufferSize)]
    public struct Scratch<T>
    {
        private T _data;
    }
}
