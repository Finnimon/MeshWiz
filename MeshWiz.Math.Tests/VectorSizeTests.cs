using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
namespace MeshWiz.Math.Tests;

[UsedImplicitly]
public class VectorSizeTests
{

    [Test]
    public void TestVector3Half()
        => VectorSizeAssert<Vector3<Half>, Half>();
    [Test]
    public void TestVector3Single()
        => VectorSizeAssert<Vector3<float>, float>();
    [Test]
    public void TestVector3Double()
        => VectorSizeAssert<Vector3<double>, double>();
    [Test]
    public void TestVector3NFloat()
        => VectorSizeAssert<Vector3<NFloat>,NFloat>();
    
    [Test]
    public void TestVector2Half()
        => VectorSizeAssert<Vector2<Half>, Half>();
    [Test]
    public void TestVector2Single()
        => VectorSizeAssert<Vector2<float>, float>();
    [Test]
    public void TestVector2Double()
        => VectorSizeAssert<Vector2<double>, double>();
    [Test]
    public void TestVector2NFloat()
        => VectorSizeAssert<Vector2<NFloat>,NFloat>();
    
    
    [Test]
    public void TestVector4Half()
        => VectorSizeAssert<Vector4<Half>, Half>();
    [Test]
    public void TestVector4Single()
        => VectorSizeAssert<Vector4<float>, float>();
    [Test]
    public void TestVector4Double()
        => VectorSizeAssert<Vector4<double>, double>();
    [Test]
    public void TestVector4NFloat()
        => VectorSizeAssert<Vector4<NFloat>,NFloat>();

    private unsafe void VectorSizeAssert<TVector,TNum>() 
        where TVector : unmanaged, IFloatingVector<TVector,TNum>
        where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
    {
        var expected = (int)(sizeof(TNum) * TVector.Dimensions);
        var reported = TVector.ByteSize;
        var actual = sizeof(TVector);
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected), "Actual size is not correct.");
            Assert.That(reported, Is.EqualTo(expected), "Reported size is not correct.");
        });
    }
}