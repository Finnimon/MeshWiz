using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public readonly struct ToolCenterPoint<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec3<TNum> Position;
    public readonly Vec3<TNum> Normal;
    public readonly TcpOptions Options;
    
    public ToolCenterPoint(Vec3<TNum> position, Vec3<TNum> normal, TcpOptions options)
    {
        Position = position;
        Normal = normal;
        Options = options;
    }
    public TcpOptions MovementMode => Options & TcpOptions.MovementModeMask;

    public Vec3<TNum> EulerAngles(Vec3<TNum> previousPosition)
        => throw new NotImplementedException();
}