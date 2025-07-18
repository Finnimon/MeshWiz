using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public struct BoundedVolume<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly BBox3<TNum> Bounds;
    public readonly int Start, Length;
    public int FirstChild, SecondChild;
    public bool IsLeaf => FirstChild == -1 && SecondChild == -1;
    public bool IsParent=>FirstChild!=-1 && SecondChild!=-1;
    private BoundedVolume(BBox3<TNum> bounds, int start, int length,int  firstChild, int secondChild)
    {
        Bounds = bounds;
        Start = start;
        Length = length;
        FirstChild = firstChild;
        SecondChild = secondChild;
    }
    
    public BoundedVolume(BBox3<TNum> bounds, int start, int length, uint firstChild, uint secondChild)
        : this(bounds, start, length, (int) firstChild,(int) secondChild) { }
    
    public BoundedVolume(BBox3<TNum> bounds, int start, int length) 
        : this(bounds, start, length, -1, -1) 
    { }

    public TNum Cost=>Bounds.Size.SquaredLength*TNum.CreateTruncating(Length);
    public int End => Start + Length;
    
    public static TNum NodeCost(BBox3<TNum> bounds, int triCount)
        =>bounds.Size.SquaredLength*TNum.CreateTruncating(triCount);

    public static TNum NodeCost(Vector3<TNum> boundsSize, int triCount)
        =>boundsSize.SquaredLength*TNum.CreateTruncating(triCount);

    public static implicit operator Range(in BoundedVolume<TNum> bV) => new(bV.Start, bV.End);
}