using System.Runtime.CompilerServices;

namespace MeshWiz.Buffers;

public static partial class ArrayBuilder
{
    /// <summary>The size to use for the first segment that's stack allocated by the caller.</summary>
    /// <remarks>
    /// This value needs to be small enough that we don't need to be overly concerned about really large
    /// value types. It's not unreasonable for a method to say it has 8 locals of a T, and that's effectively
    /// what this is.
    /// </remarks>
    public const int ScratchBufferSize = 8;
    [InlineArray(ScratchBufferSize)]
    public struct Scratch<T>
    {
        private T _data;
    }
}
