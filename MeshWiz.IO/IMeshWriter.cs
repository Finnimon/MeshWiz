using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.IO;

public interface IMeshWriter<TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    static abstract void Write(IMesh3<TNum> mesh, Stream stream, bool leaveOpen = false);

    static void WriteFile<TMeshWriter>(IMesh3<TNum> mesh, string filepath)
        where TMeshWriter : IMeshWriter<TNum>
        => TMeshWriter.Write(mesh, File.OpenWrite(filepath), leaveOpen: false);
}