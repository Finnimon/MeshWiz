using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IVec3<TSelf, TNum> : IVec<TSelf, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TSelf : unmanaged, IVec3<TSelf, TNum>
{
}