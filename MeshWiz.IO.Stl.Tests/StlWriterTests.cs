using System.Numerics;

namespace MeshWiz.IO.Stl.Tests;

public class StlWriterTests
{
    [TestCase("Assets/cube-ascii.stl"), TestCase("Assets/cube-binary.stl")]
    public void TestReadWriteFast(string originalFile)
        => ReadWriteTest<FastStlReader, FastBinaryStlWriter, float>(originalFile);


    [TestCase("Assets/cube-ascii.stl"), TestCase("Assets/cube-binary.stl")]
    public void TestReadWriteSafe(string originalFile)
        => ReadWriteTest<SafeStlReader<float>, SafeBinaryStlWriter<float>, float>(originalFile);

    private static void ReadWriteTest<TMeshReader, TMeshWriter, TNum>(string file)
        where TMeshReader : IMeshReader<TNum>
        where TMeshWriter : IMeshWriter<TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var mesh = MeshIO.ReadFile<TMeshReader, TNum>(file);
        using var memStream = new MemoryStream();
        TMeshWriter.Write(mesh, memStream, leaveOpen: true);
        memStream.Seek(0, SeekOrigin.Begin);
        var rereadMesh = TMeshReader.Read(memStream, leaveOpen: false);
        Assert.That(rereadMesh, Is.EquivalentTo(mesh));
    }
}