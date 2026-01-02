using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public struct BoundedVolume<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly AABB<Vec3<TNum>> Bounds;
    private int _first;
    private int _second;

    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
    public int Start => _first;

    public int Length => -_second;
    public int FirstChild => _first;
    public int SecondChild => _second;
    public bool IsLeaf => _second <= 0;
    public bool IsParent => _second > 0;

    private BoundedVolume(AABB<Vec3<TNum>> bounds, int first, int second)
    {
        Bounds = bounds;
        _first = first;
        _second = second;
    }

    public static BoundedVolume<TNum> MakeLeaf(AABB<Vec3<TNum>> bounds, int start, int length)
        => new(bounds, start, -length);

    public static BoundedVolume<TNum> MakeParent(AABB<Vec3<TNum>> bounds, int left, int right)
        => new(bounds, left, -right);

    public void RegisterChildren(int firstChild, int secondChild)
    {
        _first = firstChild;
        _second = secondChild;
    }


    public TNum Cost => Bounds.Size.SquaredLength * TNum.CreateTruncating(Length);
    public int End => Start + Length;
    public Range LeafRange => Start..End;

    public static TNum NodeCost(AABB<Vec3<TNum>> bounds, int triCount)
        => bounds.Size.SquaredLength * TNum.CreateTruncating(triCount);

    public static TNum NodeCost(Vec3<TNum> boundsSize, int triCount)
        => boundsSize.SquaredLength * TNum.CreateTruncating(triCount);


    public static implicit operator Range(in BoundedVolume<TNum> bV) => new(bV.Start, bV.End);
}