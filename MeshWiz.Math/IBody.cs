using System.Numerics;

namespace MeshWiz.Math;

public interface IBody<TNum> : ISurface3<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TNum Volume { get; }
}