using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace MeshWiz.Buffers.UnitTests;

[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
public class ArrayBuilderToList
{
    [TestCaseSource(typeof(EnumerableTestCases), nameof(EnumerableTestCases.Cases))]
    [Obsolete]
    public void Buffered<T>(IEnumerable<T> data)
    where T: unmanaged
    {
        var res = ArrayBuilder.Buffered<T>.ToList(data);
        Assert.That(res,Is.EquivalentTo(data));
    }
    [TestCaseSource(typeof(EnumerableTestCases), nameof(EnumerableTestCases.Cases))]
    public void Segmented<T>(IEnumerable<T> data)
    {
        var res = ArrayBuilder.Segmented<T>.ToList(data);
        Assert.That(res,Is.EquivalentTo(data));
    }
    
    [TestCaseSource(typeof(EnumerableTestCases), nameof(EnumerableTestCases.Cases))]
    [Obsolete]
    public void Pooled<T>(IEnumerable<T> data)
    where T: unmanaged
    {
        var res = ArrayBuilder.Pooled<T>.ToArray(data);
        Assert.That(res,Is.EquivalentTo(data));
    }
    
    [TestCaseSource(typeof(EnumerableTestCases), nameof(EnumerableTestCases.Cases))]
    public void Enumerable<T>(IEnumerable<T> data)
    {
        var res = System.Linq.Enumerable.ToArray(data);
        Assert.That(res,Is.EquivalentTo(data));
    }
}
