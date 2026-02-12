using System.Text;

namespace MeshWiz.Buffers.UnitTests;

internal static class AllocationHelper
{
    public static string CreateRandomString(int l)
    {
        Random r = new();
        StringBuilder b = new(l);
        for (var i = 0; i < l; i++) b.Append((r.Next() % 10).ToString());
        return b.ToString();
    }
}