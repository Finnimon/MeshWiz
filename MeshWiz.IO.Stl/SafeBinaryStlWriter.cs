using System;
using System.IO;
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
                Vec3<float> n = (facet.Normal);
                stream.Write(n.ToByteSpan());
                Vec3<float> vec = (facet.A);
                stream.Write(vec.ToByteSpan());
                vec = (facet.B);
                stream.Write(vec.ToByteSpan());
                vec = (facet.C);
                stream.Write(vec.ToByteSpan());
                stream.Write(AttribByteCount);
            }
        }
        finally
        {
            if(!leaveOpen) stream.Dispose();
        }
    }

}