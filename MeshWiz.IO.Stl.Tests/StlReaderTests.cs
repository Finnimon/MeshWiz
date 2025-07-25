namespace MeshWiz.IO.Stl.Tests;

public class StlReaderTests
{
    [TestCase("Assets/cube-ascii.stl"), TestCase("Assets/cube-binary.stl"), TestCase("Assets/big-ascii.stl"),
     TestCase("Assets/big-binary.stl")]
    public void FastStlReaderTest(string filename)
        => IMeshReader<float>.ReadFile<FastStlReader>(filename);

    [TestCase("Assets/cube-ascii.stl"), TestCase("Assets/cube-binary.stl"), TestCase("Assets/big-ascii.stl"),
     TestCase("Assets/big-binary.stl")]
    public void SafeStlReaderTest(string filename)
        => IMeshReader<float>.ReadFile<SafeStlReader<float>>(filename);

    [TestCase("Assets/cube-ascii.stl"), TestCase("Assets/cube-binary.stl"), TestCase("Assets/big-ascii.stl"),
     TestCase("Assets/big-binary.stl")]
    public void AssertEquivalenceTest(string filename)
    {
        var safeMesh=IMeshReader<float>.ReadFile<SafeStlReader<float>>(filename);
        var fastMesh=IMeshReader<float>.ReadFile<FastStlReader>(filename);
        Assert.That(fastMesh.TessellatedSurface, Is.EqualTo(safeMesh.TessellatedSurface));
    }
}