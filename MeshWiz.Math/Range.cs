using System.Numerics;

namespace MeshWiz.Math;

public readonly record struct Range<TNum>(TNum Start, TNum End)
    where TNum:unmanaged,IFloatingPointIeee754<TNum>
{
    public TNum Size=>End - Start;
    public int Direction => TNum.Sign(Size);
    public TNum SizeAbs => TNum.Abs(Size);
    public static implicit operator AABB<TNum>(Range<TNum> r)=>AABB.From(r.Start, r.End);
    public static implicit operator Range<TNum>(AABB<TNum> box)=>new(box.Min, box.Max);
    
}