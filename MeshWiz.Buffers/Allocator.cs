namespace MeshWiz.Buffers;

internal static class Allocator
{
    public const int InitialSharedCapacity = 1<<18;
    public const int InitialLocalCapacity = 1<<12;
}