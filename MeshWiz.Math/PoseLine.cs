using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible",Justification = "Pose Copy Cost")]
public readonly struct PoseLine<TPose, TVector, TNum>(TPose start, TPose end) : ILine<TVector, TNum>,IDiscretePoseCurve<TPose,TVector,TNum>,
    IEquatable<PoseLine<TPose,TVector,TNum>>
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
    public TVector Traverse(TNum t)
        => TVector.Lerp(_start.Position, _end.Position, t);

    /// <inheritdoc />
    public TVector Start => _start.Position;


    /// <inheritdoc />
    public TVector End => _end.Position;

    /// <inheritdoc />
    public TVector TraverseOnCurve(TNum t)
        => TVector.Lerp(_start.Position, _end.Position, TNum.Clamp(t, TNum.Zero, TNum.One));

    /// <inheritdoc />
    public Polyline<TVector, TNum> ToPolyline()
        => new([_start.Position, _end.Position]);

    /// <inheritdoc />
    public Polyline<TVector, TNum> ToPolyline(
        PolylineTessellationParameter<TNum> tessellationParameter)
        => new([_start.Position, _end.Position]);

    /// <inheritdoc />
    TVector ILine<TVector, TNum>.MidPoint => TVector.Lerp(_start.Position, _end.Position, Numbers<TNum>.Half);

    public TPose MidPoint => GetPose(Numbers<TNum>.Half);

    public static implicit operator Line<TVector, TNum>(in PoseLine<TPose, TVector, TNum> source)
        => new(source._start.Position, source._end.Position);

    /// <inheritdoc />
    public TVector GetTangent(TNum t) => this.GetPose(t).Front;

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

    /// <inheritdoc />
    public bool Equals(PoseLine<TPose, TVector, TNum> other) => _start == other._start && _end == other._end;

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is PoseLine<TPose, TVector, TNum> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_start, _end);
    }

    public static bool operator ==(PoseLine<TPose, TVector, TNum> left, PoseLine<TPose, TVector, TNum> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PoseLine<TPose, TVector, TNum> left, PoseLine<TPose, TVector, TNum> right)
    {
        return !left.Equals(right);
    }
}