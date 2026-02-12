using NUnit.Framework;

namespace MeshWiz.Buffers.UnitTests;

public static class EnumerableTestCases
{
    private static IEnumerable<int> IntData() => Enumerable.Range(0, 1000);
    private static IEnumerable<string> StringData() => IntData().Select(i => i.ToString());
    private static IEnumerable<long> LongData() => Enumerable.Sequence(0L, 999L, 1L);
    public static IEnumerable<TestCaseData> Cases()
    {
        yield return new TestCaseData(StringData()) { TypeArgs = [typeof(string)], TestName = nameof(String) };
        yield return new TestCaseData(LongData()) { TypeArgs = [typeof(long)], TestName = nameof(Int64)};
        yield return new TestCaseData(IntData()){ TypeArgs = [typeof(int)],TestName = nameof(Int32)};
    }
}