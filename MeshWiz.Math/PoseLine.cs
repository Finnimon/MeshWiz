using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible",Justification = "Pose Copy Cost")]
public readonly struct PoseLine<TPose, TVec, TNum>(TPose start, TPose end) : ILine<TVec, TNum>,IDiscretePoseCurve<TPose,TVec,TNum>,
    IEquatable<PoseLine<TPose,TVec,TNum>>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TPose : IPose<TPose, TVec, TNum>
{
    private readonly TPose _start = start, _end = end;
    public TPose StartPose => _start;
    public TPose EndPose => _end;
    /// <inheritdoc />
    public TNum Length => _start.DistanceTo(_end);

    /// <inheritdoc />
    public TVec AxisVector => _end.Position - _start.Position;

    /// <inheritdoc />
    public TVec Direction => TVec.Normalize(_end.Position - _start.Position);

    public TPose GetPose(TNum t) => TPose.Lerp(_start, _end, t);

    /// <inheritdoc />
    public bool IsClosed => false;

    /// <inheritdoc />
    public TVec Traverse(TNum t)
        => TVec.Lerp(_start.Position, _end.Position, t);

    /// <inheritdoc />
    public TVec Start => _start.Position;


    /// <inheritdoc />
    public TVec End => _end.Position;

    /// <inheritdoc />
    public TVec TraverseOnCurve(TNum t)
        => TVec.Lerp(_start.Position, _end.Position, TNum.Clamp(t, TNum.Zero, TNum.One));

    /// <inheritdoc />
    public Polyline<TVec, TNum> ToPolyline()
        => new([_start.Position, _end.Position]);

    /// <inheritdoc />
    public Polyline<TVec, TNum> ToPolyline(
        PolylineTessellationParameter<TNum> tessellationParameter)
        => new([_start.Position, _end.Position]);

    /// <inheritdoc />
    TVec ILine<TVec, TNum>.MidPoint => TVec.Lerp(_start.Position, _end.Position, Numbers<TNum>.Half);

    public TPose MidPoint => GetPose(Numbers<TNum>.Half);

    public static implicit operator Line<TVec, TNum>(in PoseLine<TPose, TVec, TNum> source)
        => new(source._start.Position, source._end.Position);

    /// <inheritdoc />
    public TVec GetTangent(TNum t) => this.GetPose(t).Front;

    /// <inheritdoc />
    public TVec EntryDirection => _start.Front;

    /// <inheritdoc />
    public TVec ExitDirection =>_end.Front;

    /// <inheritdoc />
    public PosePolyline<TPose, TVec, TNum> ToPosePolyline()
        => new(_start, _end);

    /// <inheritdoc />
    public PosePolyline<TPose, TVec, TNum> ToPosePolyline(PolylineTessellationParameter<TNum> tessellationParameter)
        => new(_start, _end);

    public static PoseLine<Pose3<TNum>, Vec3<TNum>, TNum> FromLine(
        Line<Vec3<TNum>, TNum> line,
        Vec3<TNum> up)
        => FromLine(line.Start, line.End, up);
    
    public static PoseLine<Pose3<TNum>, Vec3<TNum>, TNum> FromLine(
        Vec3<TNum> a,
        Vec3<TNum> b,
        Vec3<TNum> normal)
    {
        var pose1 = Pose3<TNum>.CreateFromOrientation(a,b-a, normal)
            .Value;
        var pose2 = new Pose3<TNum>(pose1.Rotation, b);
        return new PoseLine<Pose3<TNum>, Vec3<TNum>, TNum>(pose1, pose2);
    }

    /// <inheritdoc />
    public bool Equals(PoseLine<TPose, TVec, TNum> other) => _start == other._start && _end == other._end;

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is PoseLine<TPose, TVec, TNum> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_start, _end);
    }

    public static bool operator ==(PoseLine<TPose, TVec, TNum> left, PoseLine<TPose, TVec, TNum> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PoseLine<TPose, TVec, TNum> left, PoseLine<TPose, TVec, TNum> right)
    {
        return !left.Equals(right);
    }

    public PoseLine<TPose, TVec, TNum> Section(TNum p0, TNum p1)
        => new(GetPose(p0), GetPose(p1));
}