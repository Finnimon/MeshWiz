using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.IO;

public static class MeshIO
{
    public static void WriteFile<TMeshWriter,TNum>(IMesh<TNum> mesh, string filepath)
        where TMeshWriter : IMeshWriter<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => TMeshWriter.Write(mesh, File.OpenWrite(filepath), leaveOpen: false);
    public static IMesh<TNum> ReadFile<TMeshReader,TNum>(string filepath)
    where TMeshReader : IMeshReader<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    =>TMeshReader.Read(File.OpenRead(filepath), leaveOpen: false);
}