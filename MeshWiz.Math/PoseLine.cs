using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible",Justification = "Pose Copy Cost")]
public readonly struct PoseLine<TPose, TVector, TNum>(TPose start, TPose end) : ILine<TVector, TNum>,IDiscretePoseCurve<TPose,TVector,TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TPose : IPose<TPose, TVector, TNum>
{
    private readonly TPose _start = start, _end = end;
    public TPose StartPose => _start;
    public TPose EndPose => _end;
    /// <inheritdoc />
    public TNum Length => _start.DistanceTo(_end);

    /// <inheritdoc />
    public TVector AxisVector => _end.Position - _start.Position;

    /// <inheritdoc />
    public TVector Direction => TVector.Normalize(_end.Position - _start.Position);

    public TPose GetPose(TNum t) => TPose.Lerp(_start, _end, t);

    /// <inheritdoc />
    public bool IsClosed => false;

    /// <inheritdoc />
    TVector ICurve<TVector, TNum>.Traverse(TNum t)
        => TVector.Lerp(_start.Position, _end.Position, t);

    /// <inheritdoc />
    TVector IDiscreteCurve<TVector, TNum>.Start => _start.Position;


    /// <inheritdoc />
    TVector IDiscreteCurve<TVector, TNum>.End => _end.Position;

    /// <inheritdoc />
    TVector IDiscreteCurve<TVector, TNum>.TraverseOnCurve(TNum t)
        => TVector.Lerp(_start.Position, _end.Position, TNum.Clamp(t, TNum.Zero, TNum.One));

    /// <inheritdoc />
    Polyline<TVector, TNum> IDiscreteCurve<TVector, TNum>.ToPolyline()
        => new([_start.Position, _end.Position]);

    /// <inheritdoc />
    Polyline<TVector, TNum> IDiscreteCurve<TVector, TNum>.ToPolyline(
        PolylineTessellationParameter<TNum> tessellationParameter)
        => new([_start.Position, _end.Position]);

    /// <inheritdoc />
    TVector ILine<TVector, TNum>.MidPoint => TVector.Lerp(_start.Position, _end.Position, Numbers<TNum>.Half);

    public TPose MidPoint => GetPose(Numbers<TNum>.Half);

    public static implicit operator Line<TVector, TNum>(in PoseLine<TPose, TVector, TNum> source)
        => new(source._start.Position, source._end.Position);

    /// <inheritdoc />
    public TVector GetTangent(TNum at) => this.GetPose(at).Front;

    /// <inheritdoc />
    public TVector EntryDirection => _start.Front;

    /// <inheritdoc />
    public TVector ExitDirection =>_end.Front;

    /// <inheritdoc />
    public PosePolyline<TPose, TVector, TNum> ToPosePolyline()
        => new(_start, _end);

    /// <inheritdoc />
    public PosePolyline<TPose, TVector, TNum> ToPosePolyline(PolylineTessellationParameter<TNum> tessellationParameter)
        => new(_start, _end);

    public static PoseLine<Pose3<TNum>, Vector3<TNum>, TNum> FromLine(
        Line<Vector3<TNum>, TNum> line,
        Vector3<TNum> up)
        => FromLine(line.Start, line.End, up);
    
    public static PoseLine<Pose3<TNum>, Vector3<TNum>, TNum> FromLine(
        Vector3<TNum> a,
        Vector3<TNum> b,
        Vector3<TNum> normal)
    {
        var pose1 = Pose3<TNum>.CreateFromOrientation(a,b-a, normal)
            .Value;
        var pose2 = new Pose3<TNum>(pose1.Rotation, b);
        return new PoseLine<Pose3<TNum>, Vector3<TNum>, TNum>(pose1, pose2);
    }
}