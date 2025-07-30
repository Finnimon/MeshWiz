using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public readonly struct ToolCenterPoint<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> Position;
    public readonly Vector3<TNum> Normal;
    public readonly TcpOptions Options;
    
    public ToolCenterPoint(Vector3<TNum> position, Vector3<TNum> normal, TcpOptions options)
    {
        Position = position;
        Normal = normal;
        Options = options;
    }
    public TcpOptions MovementMode => Options & TcpOptions.MovementModeMask;

    public Vector3<TNum> EulerAngles(Vector3<TNum> previousPosition)
        => throw new NotImplementedException();
}