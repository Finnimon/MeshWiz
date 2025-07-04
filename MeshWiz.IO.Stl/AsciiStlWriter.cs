using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.IO.Stl;

public class AsciiStlWriter<TNum>
    : IMeshWriter<TNum> 
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public static void Write(Mesh3<TNum> mesh, Stream stream, bool leaveOpen = false)
    {
        try
        {
            var headerString = $"solid ascii-stl facet-count={mesh.TessellatedSurface.LongLength}";
            using var writer = new StreamWriter(stream, leaveOpen: leaveOpen);
            var facets = mesh.TessellatedSurface;
            writer.WriteLine(headerString);
            for (var i = 0; i <facets.LongLength; i++) 
                writer.Write(FormatFacetAscii(in facets[i]));
            writer.WriteLine("endsolid ascii-stl");
        }
        finally
        {
            if(!leaveOpen) stream.Dispose();
        }
    }
    
    
    private static string FormatFacetAscii(in Triangle3<TNum> facet)
    {
        var normal = facet.Normal;
        var (a,b,c)=facet;
        return $"""
                 facet normal {normal.X:e6} {normal.Y:e6} {normal.Z:e6}
                  outer loop
                   vertex {a.X:e6} {a.Y:e6} {a.Z:e6}
                   vertex {b.X:e6} {b.Y:e6} {b.Z:e6}
                   vertex {c.X:e6} {c.Y:e6} {c.Z:e6}
                  endloop
                 endfacet
                 
                """;
    }
}