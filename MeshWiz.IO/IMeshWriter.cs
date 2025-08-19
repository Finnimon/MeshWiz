using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.IO;

public interface IMeshWriter<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    static abstract void Write(IMesh<TNum> mesh, Stream stream, bool leaveOpen = false);

}