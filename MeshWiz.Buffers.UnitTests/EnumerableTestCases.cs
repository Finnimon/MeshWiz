using NUnit.Framework;

namespace MeshWiz.Buffers.UnitTests;

internal static class EnumerableTestCases
{
    private static readonly int[] CaseLengths = [0, 1, 2, 4, 8, 16, 1000, 1000_000, 10_000_000/*, Array.MaxLength-1000*/];
    private static IEnumerable<int> IntData(int n) => Enumerable.Range(0, n);
    private static IEnumerable<string> StringData(int n) => IntData(n).Select(i => new string([(char)(n%128)]));
    private static IEnumerable<long> LongData(int n) => IntData(n).Select<int,long>(l => l);
    private static IEnumerable<Int128> LongLongData(int n) => IntData(n).Select<int,Int128>(l => l);
    public static IEnumerable<TestCaseData> Cases() => CaseLengths.SelectMany(TestCasesByCount);

    private static IEnumerable<TestCaseData> TestCasesByCount(int n)
    {
        yield return new TestCaseData(StringData(n)) { TypeArgs = [typeof(string)], TestName = $"{nameof(String)} - ({n:D12})"};
        yield return new TestCaseData(LongLongData(n)) { TypeArgs = [typeof(Int128)], TestName = $"{nameof(Int128)} - ({n:D12})"};
        yield return new TestCaseData(LongData(n)) { TypeArgs = [typeof(long)], TestName = $"{nameof(Int64)} - ({n:D12})"};
        yield return new TestCaseData(IntData(n)){ TypeArgs = [typeof(int)],TestName = $"{nameof(Int32)} - ({n:D12})"};
    }
}