using System.Numerics;

namespace MeshWiz.Math;

public interface IBounded<TNum> where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    AABB<TNum> BBox { get; }
}