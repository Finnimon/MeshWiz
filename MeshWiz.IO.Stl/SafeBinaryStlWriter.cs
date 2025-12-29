using System.Numerics;
using System.Text;
using MeshWiz.Math;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.IO.Stl;

public sealed class SafeBinaryStlWriter<TNum> 
    : IMeshWriter<TNum> 
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    private SafeBinaryStlWriter() { }
    private static string HeaderString => "binary stl".PadRight(80);
    private static readonly byte[] AttribByteCount = [0,0];
  

    public static void Write(IMesh<TNum> mesh, Stream stream, bool leaveOpen = false)
    {
        try
        {
            stream.Write(Encoding.ASCII.GetBytes(HeaderString), 0, HeaderString.Length);
            var facetCount = BitConverter.GetBytes((uint)mesh.Count);
            stream.Write(facetCount, 0, facetCount.Length);
            for (var i = 0; i < mesh.Count; i++)
            {
                var facet = mesh[i];
                var n = FromOtherNumType(facet.Normal);
                stream.Write(n.ToByteSpan());
                var vec = FromOtherNumType(facet.A);
                stream.Write(vec.ToByteSpan());
                vec = FromOtherNumType(facet.B);
                stream.Write(vec.ToByteSpan());
                vec = FromOtherNumType(facet.C);
                stream.Write(vec.ToByteSpan());
                stream.Write(AttribByteCount);
            }
        }
        finally
        {
            if(!leaveOpen) stream.Dispose();
        }
    }

    private static Vec3<float> FromOtherNumType(in Vec3<TNum> vec)
        =>new(float.CreateTruncating(vec.X), float.CreateTruncating(vec.Y), float.CreateTruncating(vec.Z));
}