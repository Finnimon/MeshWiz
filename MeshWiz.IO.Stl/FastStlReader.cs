using System.Diagnostics.CodeAnalysis;
using System.Text;
using MeshWiz.Math;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.IO.Stl;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FastStlReader : IMeshReader<float>
{
    private FastStlReader() { }

    public static Mesh3<float> Read(Stream stream, bool leaveOpen = false)
    {
        try
        {
            return ReadInternal(stream);
        }
        finally 
        {
            if(!leaveOpen) stream.Dispose();
        }
    }

    private static Mesh3<float> ReadInternal(Stream stream)
    {
        var solid=new  byte[5];
        stream.ReadExactly(solid);
        var isAscii = Encoding.ASCII.GetString(solid).Equals(nameof(solid), StringComparison.OrdinalIgnoreCase);
        stream.Seek(-solid.Length, SeekOrigin.Current);
        return isAscii ? ReadAscii(stream) : ReadBinary(stream);
    }

    private const int Stride = 50;
    private const int TriangleOffset = 12;
    private const int HeaderLength = 80;
    private const int TotalHeaderLength = HeaderLength + sizeof(uint);
    private static Mesh3<float> ReadBinary(Stream stream)
    {
        var remainingLength= stream.Length-stream.Position;
        if (remainingLength < TotalHeaderLength) 
            throw new InvalidDataException($"Stream does not provide minimum length of {TotalHeaderLength} bytes for the binary stl header but was {remainingLength}.");
        stream.Seek(HeaderLength, SeekOrigin.Current);
        var countBuffer=new byte[sizeof(uint)];
        var readCount = stream.Read(countBuffer);
        if(readCount!=sizeof(uint)) 
            throw new InvalidDataException("Could not read facet count.");
        var triangleCount = StructExt.UnsafeAs<byte, uint>(in countBuffer[0]);
        var expectedRemainingLength = triangleCount * Stride;
        remainingLength -= TotalHeaderLength;
        if(remainingLength<expectedRemainingLength) 
            throw new InvalidDataException($"Expected stream length {expectedRemainingLength} but was {remainingLength}");
        var facets=new Triangle3<float>[triangleCount];
        var buffer=new byte[Stride];
        for (uint i = 0; i < triangleCount; i++)
        {
            stream.ReadExactly(buffer);
            facets[i]=StructExt.UnsafeAs<byte,Triangle3<float>>(in buffer[TriangleOffset]);
        }
        return new Mesh3<float>(facets);
    }

    private static Mesh3<float> ReadAscii(Stream stream)
        => SafeStlReader<float>.ReadAsciiInternal(stream);
}