using MeshWiz.Utility.Extensions;

namespace MeshWiz.Utility.Tests.StructExtTest;

public class UnsafeAsTests
{
    
    [TestCase(10),TestCase(100),TestCase(-1)]
    public void TestOnArray(int number)
    {
        var bytes= BitConverter.GetBytes(number);
        var convertedBack = StructExt.UnsafeAs<byte, int>(bytes[0]);
        Assert.That(convertedBack, Is.EqualTo(number));
    }
    
    [TestCase(10),TestCase(100),TestCase(-1)]
    public void TestOnArrayWithOffset(int number)
    {
        var bytes= BitConverter.GetBytes(number);
        bytes=[0,0,..bytes,0];
        var convertedBack = StructExt.UnsafeAs<byte, int>(bytes[2]);
        Assert.That(convertedBack, Is.EqualTo(number));
    }
}