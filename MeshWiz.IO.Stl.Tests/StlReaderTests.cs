namespace MeshWiz.IO.Stl.Tests;

public class StlReaderTests
{
    [TestCase("Assets/cube-ascii.stl"), TestCase("Assets/cube-binary.stl"), TestCase("Assets/big-ascii.stl"),
     TestCase("Assets/big-binary.stl")]
    public void FastStlReaderTest(string filename)
        => MeshIO.ReadFile<FastStlReader,float>(filename);

    [TestCase("Assets/cube-ascii.stl"), TestCase("Assets/cube-binary.stl"), TestCase("Assets/big-ascii.stl"),
     TestCase("Assets/big-binary.stl")]
    public void SafeStlReaderTest(string filename)
        => MeshIO.ReadFile<SafeStlReader<float>,float>(filename);

    [TestCase("Assets/cube-ascii.stl"), TestCase("Assets/cube-binary.stl"), TestCase("Assets/big-ascii.stl"),
     TestCase("Assets/big-binary.stl")]
    public void AssertEquivalenceTest(string filename)
    {
        var safeMesh=MeshIO.ReadFile<SafeStlReader<float>,float>(filename);
        var fastMesh=MeshIO.ReadFile<FastStlReader,float>(filename);
        Assert.That(fastMesh, Is.EqualTo(safeMesh));
    }
}