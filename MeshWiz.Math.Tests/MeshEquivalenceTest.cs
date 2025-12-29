namespace MeshWiz.Math.Tests;

public class MeshEquivalenceTest
{

    [Test]
    public void EquivalenceOfIndexedMeshTest()
    {
        var tessellations = Sphere<float>.GenerateTessellation(Vec3<float>.Zero, 1, 32, 64);
        Console.WriteLine(tessellations.Length);
        var mesh=new Mesh<float>(tessellations);
        var indexed=new IndexedMesh<float>(tessellations);
        var meshNormals = mesh.Select(x => x.Normal).ToArray();
        var indexedNormals = indexed.Select(x => x.Normal).ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(indexed, Is.EqualTo(tessellations));
            Assert.That(mesh, Is.EqualTo(tessellations));
            Assert.That(indexed, Is.EqualTo(tessellations));
            Assert.That(mesh.Indexed(), Is.EqualTo(mesh));
            Assert.That(meshNormals,Is.EqualTo(indexedNormals));
        }
        for (var i = 0; i < meshNormals.Length; i++)
        {
            var normal=indexedNormals[i];
            var tri = indexed[i];
            var triCen = tri.Centroid;
            var meshCent = indexed.Centroid;
            if (meshCent.DistanceTo(triCen - normal) > meshCent.DistanceTo(normal + triCen))
                throw new Exception();
        }
    }
}