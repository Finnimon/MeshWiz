namespace MeshWiz.IO.Stl.Tests;

public class StlWriterTests
{
    [TestCase("Assets/cube-ascii.stl")]
    [TestCase("Assets/cube-binary.stl")]
    public void TestReadWriteFast(string originalFile)
    {
        var mesh=IMeshReader<float>.ReadFile<FastStlReader>(originalFile);
        using var memStream = new MemoryStream();
        FastBinaryStlWriter.Write(mesh, memStream,leaveOpen:true);
        memStream.Seek(0, SeekOrigin.Begin);
        var rereadMesh=FastStlReader.Read(memStream,leaveOpen:false);
        Assert.That(rereadMesh.TessellatedSurface, Is.EqualTo(mesh.TessellatedSurface));
    }
    
    
    [TestCase("Assets/cube-ascii.stl")]
    [TestCase("Assets/cube-binary.stl")]
    public void TestReadWriteSafe(string originalFile)
    {
        var mesh=IMeshReader<double>.ReadFile<SafeStlReader<double>>(originalFile);
        using var memStream = new MemoryStream();
        SafeBinaryStlWriter<double>.Write(mesh, memStream,leaveOpen:true);
        memStream.Seek(0, SeekOrigin.Begin);
        var rereadMesh=SafeStlReader<double>.Read(memStream,leaveOpen:false);
        Assert.That(rereadMesh.TessellatedSurface, Is.EqualTo(mesh.TessellatedSurface));
    }
}