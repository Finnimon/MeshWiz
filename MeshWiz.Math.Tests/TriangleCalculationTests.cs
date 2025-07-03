namespace MeshWiz.Math.Tests;

public class TriangleCalculationTests
{
    [Test]
    public void TestTriangleArea()
    {
        Vector3<float> a = new(0, 0, 0);
        Vector3<float> b = new(1, 0, 0);
        Vector3<float> c = new(0, 1, 0);
        var triangle = new Triangle<Vector3<float>, float>(a,b,c);
        Assert.That(triangle.SurfaceArea,Is.EqualTo(0.5f).Within(0.001));
    }
}