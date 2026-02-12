using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace MeshWiz.Buffers.UnitTests;

public class ArrayBuilderToArray
{
    [TestCaseSource(typeof(EnumerableTestCases), nameof(EnumerableTestCases.Cases))]
    public void Buffered<T>(IEnumerable<T> data)
    {
        var res = ArrayBuilder.Buffered<T>.ToArray(data);
        Assert.That(res,Is.EquivalentTo(data));
    }
    [TestCaseSource(typeof(EnumerableTestCases), nameof(EnumerableTestCases.Cases))]
    public void Segmented<T>(IEnumerable<T> data)
    {
        var res = ArrayBuilder.Segmented<T>.ToList(data);
        Assert.That(res,Is.EquivalentTo(data));
    }
}