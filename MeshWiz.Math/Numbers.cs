using System.Numerics;

namespace MeshWiz.Math;

public static class Numbers<TNum>
where TNum: INumberBase<TNum>
{
    public static readonly TNum Two = TNum.CreateTruncating(2);
    public static readonly TNum Half=TNum.CreateTruncating(0.5f);
    public static readonly TNum Three=TNum.CreateTruncating(3);
    public static readonly TNum RootTwo = TNum.CreateTruncating(float.Sqrt(2f));
}