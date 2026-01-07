namespace MeshWiz.Math.Tests;

public class TriangleCalculationTests
{
    [Test]
    public void TestTriangleArea()
    {
        var a = Vec3<float>.Create(0, 0, 0);
        var b = Vec3<float>.Create(1, 0, 0);
        var c = Vec3<float>.Create(0, 1, 0);
        var triangle = new Triangle<Vec3<float>, float>(a,b,c);
        Assert.That(triangle.SurfaceArea,Is.EqualTo(0.5f).Within(0.001));
    }
}