namespace MeshWiz.Math.Tests;

public class PlaneAabbIntersect
{
    
    [TestCase(1f,1.0f)]
    [TestCase(1f,0.5f)]
    [TestCase(1f,0.1f)]
    [TestCase(0.2f,1.0f)]
    [TestCase(0.2f,0.5f)]
    [TestCase(0.2f,0.1f)]
    [TestCase(0,1.0f)]
    [TestCase(0,0.5f)]
    [TestCase(0,0.1f)]
    public void TestClampAgainstSignIntersect(float d,float boxSize)
    {
        var box = AABB<Vector3<float>>.Around(Vector3<float>.Zero, Vector3<float>.One * boxSize);
        var plane = new Plane3<float>(new Vector3<float>(0, 0, 1),d);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.That(plane.DoIntersect(box),Is.EqualTo(plane.DoIntersectDistanceSign(box)));
#pragma warning restore CS0618 // Type or member is obsolete
    }
}