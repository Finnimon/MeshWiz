using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IVector3<TSelf, TNum> : IVector<TSelf, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TSelf : unmanaged, IVector3<TSelf, TNum>
{
}