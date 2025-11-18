using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;
using CommunityToolkit.Diagnostics;
using MeshWiz.Math;

namespace MeshWiz.IO.Stl;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SafeStlReader<TNum> : IMeshReader<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private SafeStlReader() { }

    public static IMesh<TNum> Read(Stream stream, bool leaveOpen = false)
    {
        try
        {
            return ReadInternal(stream);
        }
        finally
        {
            if (!leaveOpen) stream.Dispose();
        }
    }

    private static Mesh<TNum> ReadInternal(Stream stream)
    {
        var solid = new byte[5];
        stream.ReadExactly(solid);
        var isAscii = Encoding.ASCII.GetString(solid).Equals(nameof(solid), StringComparison.OrdinalIgnoreCase);
        stream.Seek(-solid.Length, SeekOrigin.Current);
        return isAscii ? ReadAscii(stream) : ReadBinary(stream);
    }

    private const int Stride = 50;
    private const int TriangleOffset = 12;
    private const int HeaderLength = 80;
    private const int TotalHeaderLength = HeaderLength + sizeof(uint);

    private static Mesh<TNum> ReadBinary(Stream stream)
    {
        var remainingLength = stream.Length - stream.Position;
        if (remainingLength < TotalHeaderLength)
            ThrowHelper.ThrowInvalidDataException(
                $"Stream does not provide minimum length of {TotalHeaderLength} bytes for the binary stl header but was {remainingLength}.");
        stream.Seek(HeaderLength, SeekOrigin.Current);
        using BinaryReader binary = new(stream, Encoding.ASCII, true);

        var triangleCount = binary.ReadUInt32();
        var expectedRemainingLength = triangleCount * Stride;
        remainingLength -= TotalHeaderLength;
        if (remainingLength < expectedRemainingLength)
            ThrowHelper.ThrowInvalidDataException(
                $"Expected stream length {expectedRemainingLength} but was {remainingLength}");
        var facets = new Triangle3<TNum>[triangleCount];
        var facetFloats = new TNum[3, 3];
        for (uint facet = 0; facet < triangleCount; facet++)
        {
            stream.Seek(12, SeekOrigin.Current);

            for (int vertex = 0; vertex < 3; vertex++)
            for (var floatIndex = 0; floatIndex < 3; floatIndex++)
                facetFloats[vertex, floatIndex] = TNum.CreateTruncating(binary.ReadSingle());

            facets[facet] = new Triangle3<TNum>(
                new Vector3<TNum>(facetFloats[0, 0], facetFloats[0, 1], facetFloats[0, 2]),
                new Vector3<TNum>(facetFloats[1, 0], facetFloats[1, 1], facetFloats[1, 2]),
                new Vector3<TNum>(facetFloats[2, 0], facetFloats[2, 1], facetFloats[2, 2])
            );

            stream.Seek(2, SeekOrigin.Current);
        }

        return new Mesh<TNum>(facets);
    }

    private static Mesh<TNum> ReadAscii(Stream stream)
        => ReadAsciiInternal(stream);

    internal static Mesh<TNum> ReadAsciiInternal(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var headerLine = reader.ReadLine()?.Trim().Split(' ') ?? [];
        var name = headerLine.Length >= 2 ? headerLine[1] : "";
        var header = headerLine.Length < 3 ? "" : string.Join(' ', headerLine[2..headerLine.Length]);
        var facetBlocks = ReadLineBlocks(reader, new string[7]);
        var facets = facetBlocks.Select(ExtractAsciiFacet);
        return new Mesh<TNum>([..facets]);
    }
    private static IEnumerable<string[]> ReadLineBlocks(StreamReader reader, string[] buffer)
    {
        var pos = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if(line.StartsWith("endsolid",StringComparison.OrdinalIgnoreCase))
                yield break;
            if(line.IsWhiteSpace()) continue;
            buffer[pos] = line;
            pos++;
            var blockComplete = pos >= buffer.Length;
            if (!blockComplete) continue;
            pos = 0;
            yield return buffer;
        }
    }
    
    
    private static Triangle3<TNum> ExtractAsciiFacet(string[] asciiFacet)
    {
        var a = ExtractAsciiVertex(asciiFacet[2]);
        var b = ExtractAsciiVertex(asciiFacet[3]);
        var c = ExtractAsciiVertex(asciiFacet[4]);
        return  new Triangle3<TNum>(a, b, c);
    }

    private static Vector3<TNum> ExtractAsciiVertex(string asciiVertex)
    {
        var split = asciiVertex.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var x = TNum.Parse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture);
        var y = TNum.Parse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture);
        var z = TNum.Parse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture);
        return new Vector3<TNum>(x, y, z);
    }

}