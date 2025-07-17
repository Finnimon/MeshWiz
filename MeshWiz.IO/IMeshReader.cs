using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.IO;

public interface IMeshReader<TNum>
where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    static abstract Mesh3<TNum> Read(Stream stream, bool leaveOpen = false);
    static Mesh3<TNum> ReadFile<TMeshReader>(string filepath)
    where TMeshReader : IMeshReader<TNum>
    =>TMeshReader.Read(File.OpenRead(filepath), leaveOpen: false);
}