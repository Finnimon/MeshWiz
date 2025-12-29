using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
namespace MeshWiz.Math.Tests;

[UsedImplicitly]
public class VectorSizeTests
{

    [Test]
    public void TestVector3Half()
        => VectorSizeAssert<Vec3<Half>, Half>();
    [Test]
    public void TestVector3Single()
        => VectorSizeAssert<Vec3<float>, float>();
    [Test]
    public void TestVector3Double()
        => VectorSizeAssert<Vec3<double>, double>();
    [Test]
    public void TestVector3NFloat()
        => VectorSizeAssert<Vec3<NFloat>,NFloat>();
    
    [Test]
    public void TestVector2Half()
        => VectorSizeAssert<Vec2<Half>, Half>();
    [Test]
    public void TestVector2Single()
        => VectorSizeAssert<Vec2<float>, float>();
    [Test]
    public void TestVector2Double()
        => VectorSizeAssert<Vec2<double>, double>();
    [Test]
    public void TestVector2NFloat()
        => VectorSizeAssert<Vec2<NFloat>,NFloat>();
    
    
    [Test]
    public void TestVector4Half()
        => VectorSizeAssert<Vec4<Half>, Half>();
    [Test]
    public void TestVector4Single()
        => VectorSizeAssert<Vec4<float>, float>();
    [Test]
    public void TestVector4Double()
        => VectorSizeAssert<Vec4<double>, double>();
    [Test]
    public void TestVector4NFloat()
        => VectorSizeAssert<Vec4<NFloat>,NFloat>();

    private static unsafe void VectorSizeAssert<TVector,TNum>() 
        where TVector : unmanaged, IVec<TVector,TNum>
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