using System.Text;
using MeshWiz.Math;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.IO.Stl;

public sealed class FastBinaryStlWriter : IMeshWriter<float>
{
    private FastBinaryStlWriter() { }
    private static string HeaderString => "binary stl".PadRight(80);
    private static readonly byte[] AttribByteCount = [0,0];
    public static void Write(Mesh3<float> mesh, Stream stream, bool leaveOpen = false)
    {
        try
        {
            stream.Write(Encoding.ASCII.GetBytes(HeaderString), 0, HeaderString.Length);
            var facets = mesh.TessellatedSurface;
            var facetCount = BitConverter.GetBytes((uint)facets.LongLength);
            stream.Write(facetCount, 0, facetCount.Length);
            
            for (var i = 0; i < facets.Length; i++)
            {
                ref var facet = ref facets[i];
                var n = facet.Normal;
                stream.Write(n.ToByteSpan());
                stream.Write(facet.ToByteSpan());
                stream.Write(AttribByteCount);
            }
        }
        finally
        {
            if (!leaveOpen) stream.Dispose();
        }
    }
}