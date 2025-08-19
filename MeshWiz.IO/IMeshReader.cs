using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.IO;

public interface IMeshReader<TNum>
where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    static abstract IMesh<TNum> Read(Stream stream, bool leaveOpen = false);
}