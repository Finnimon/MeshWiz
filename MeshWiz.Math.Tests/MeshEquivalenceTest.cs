namespace MeshWiz.Math.Tests;

public class MeshEquivalenceTest
{

    [Test]
    public void EquivalenceOfIndexedMeshTest()
    {
        var sphere = new Sphere<float>(Vector3<float>.Zero, 1);
        var tessellations = Sphere<float>.GenerateTessellation(sphere,32,64);
        Console.WriteLine(tessellations.Length);
        var mesh=new Mesh3<float>(tessellations);
        var indexed=new IndexedMesh3<float>(tessellations);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(indexed, Is.EqualTo(tessellations));
            Assert.That(mesh, Is.EqualTo(tessellations));
            Assert.That(indexed, Is.EqualTo(tessellations));
            Assert.That(mesh.Indexed(), Is.EqualTo(mesh));
        }
    }
}