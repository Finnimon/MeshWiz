using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Helix<TNum> : IDiscretePoseCurve<Pose3<TNum>,Vec3<TNum>, TNum>,
    IEquatable<Helix<TNum>> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Cylinder<TNum> Cylinder;
    public readonly Line<Vec2<TNum>, TNum> Line;
    public TNum Length => Line.Length;
    public Vec3<TNum> Start => Project(in Cylinder, Line.Start);

    /// <inheritdoc />
    public Pose3<TNum> EndPose => GetPose(TNum.Zero);

    /// <inheritdoc />
    public Pose3<TNum> StartPose => GetPose(TNum.Zero);

    public Vec3<TNum> End => Project(in Cylinder, Line.End);
    public Vec3<TNum> EntryDirection => GetTangent(TNum.Zero);
    public Vec3<TNum> ExitDirection => GetTangent(TNum.One);


    public Helix(Cylinder<TNum> cylinder, Line<Vec2<TNum>, TNum> line)
    {
        Cylinder = cylinder;
        Line = line;
    }


    [Pure]
    public static Vec3<TNum> Project(in Cylinder<TNum> cylinder, Vec2<TNum> p)
    {
        var baseCircle = cylinder.Base;
        var xPos = p.X / baseCircle.Circumference;
        var atBase = baseCircle.Traverse(xPos);
        var shift = cylinder.Axis.Direction * p.Y;
        return atBase + shift;
    }

    [Pure]
    public static Vec2<TNum> Project(in Cylinder<TNum> cylinder, Vec3<TNum> p)
    {
        var axisDir = cylinder.Axis.Direction;
        var closest = cylinder.Axis.ClosestPoint(p);
        var y = closest.DistanceTo(cylinder.Axis.Start);
        var sign = axisDir.Dot(p - cylinder.Axis.Start);
        y = TNum.CopySign(y, sign);

        var axisToP = p - closest;
        var (u, v) = cylinder.Base.Basis;

        // Compute angle using atan2 for numerical stability
        var xComponent = u.Dot(axisToP);
        var yComponent = v.Dot(axisToP);
        var angle = TNum.Atan2(yComponent, xComponent);

        // Wrap angle to [0, 2π)
        angle = angle.Wrap(TNum.Zero, Numbers<TNum>.TwoPi);

        var circ = cylinder.Circumference;
        var x = angle / Numbers<TNum>.TwoPi * circ;

        return new Vec2<TNum>(x, y);
    }
    
    [Pure]
    public static Vec2<TNum> ProjectDirection(in Cylinder<TNum> cylinder, Vec3<TNum> p, Vec3<TNum> direction)
    {
        direction = direction.Normalized();
        var axisDir = cylinder.Axis.Direction;
        var closest = cylinder.Axis.ClosestPoint(p);
        var axisToP = (p - closest).Normalized();

        var tangentialDir = axisDir.Cross(axisToP).Normalized();
        
        var dxCircumference = tangentialDir.Dot(direction) * cylinder.Radius; // linear distance along circumference
        var dy = axisDir.Dot(direction); // along cylinder axis
        return new Vec2<TNum>(dxCircumference, dy).Normalized();
    }

    [Pure]
    public static Vec3<TNum> ProjectDirection(in Cylinder<TNum> cylinder, Vec2<TNum> p, Vec2<TNum> direction)
    {
        direction = direction.Normalized();

        var p3 = Project(in cylinder, p);
        var axisDir = cylinder.Axis.Direction;
        var closest = cylinder.Axis.ClosestPoint(p3);
        var axisToP = (p3 - closest).Normalized();

        var tangentialDir = axisDir.Cross(axisToP).Normalized();

        var dx = direction.X;
        var dy = direction.Y;

        var world = tangentialDir * dx + axisDir * dy;
        return world.Normalized();
    }
    [Pure]
    public static Ray3<TNum> ProjectDirectionComplete(in Cylinder<TNum> cylinder, Vec2<TNum> p, Vec2<TNum> direction)
    {
        direction = direction.Normalized();

        var p3 = Project(in cylinder, p);
        var axisDir = cylinder.Axis.Direction;
        var closest = cylinder.Axis.ClosestPoint(p3);
        var axisToP = (p3 - closest).Normalized();

        var tangentialDir = axisDir.Cross(axisToP).Normalized();

        var dx = direction.X;
        var dy = direction.Y;

        var worldDir = tangentialDir * dx + axisDir * dy;
        return new(p3,worldDir);
    }
    
    [Pure]
    private static Vec3<TNum> ProjectDirection(in Cylinder<TNum> cylinder, Vec3<TNum> p3, Vec2<TNum> direction)
    {
        direction = direction.Normalized();
        var axisDir = cylinder.Axis.Direction;
        var closest = cylinder.Axis.ClosestPoint(p3);
        var axisToP = (p3 - closest).Normalized();

        var tangentialDir = axisDir.Cross(axisToP).Normalized();

        var dx = direction.X;
        var dy = direction.Y;

        var world = tangentialDir * dx + axisDir * dy;
        return world.Normalized();
    }


    /// <inheritdoc />
    public Vec3<TNum> TraverseOnCurve(TNum t) => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));

    /// <inheritdoc />

    public Pose3<TNum> GetPose(TNum t)
    {
        var p2 = Line.Traverse(t);
        var pos = Project(in Cylinder, p2);
        var front = ProjectDirection(in Cylinder, pos, Line.AxisVector);
        var cylAxis = Cylinder.Axis;
        var normal = pos - cylAxis.Start;
        var axisDir = cylAxis.Direction;
        normal -= axisDir * Vec3<TNum>.Dot(normal, axisDir);
        return Pose3<TNum>.CreateFromOrientation(pos,front,normal);
    }

    /// <inheritdoc />
    public Vec3<TNum> Traverse(TNum t)
        => Project(in Cylinder, Line.Traverse(t));

    /// <inheritdoc />
    public Polyline<Vec3<TNum>, TNum> ToPolyline() =>
        ToPolyline(new PolylineTessellationParameter<TNum>
            { MaxAngularDeviation = Numbers<TNum>.TwoPi * Numbers<TNum>.Eps3 });

    /// <inheritdoc />
    public PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> ToPosePolyline()
        => ToPosePolyline(new PolylineTessellationParameter<TNum>
            { MaxAngularDeviation = Numbers<TNum>.TwoPi * Numbers<TNum>.Eps3 });
    /// <inheritdoc />
    public PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> ToPosePolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var poses = GetAngularDevPolylineSteps(tessellationParameter).Select(GetPose);
        return new PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>(poses);
    }
    /// <inheritdoc />
    public Polyline<Vec3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var pts = GetAngularDevPolylineSteps(tessellationParameter).Select(Traverse);
        return new Polyline<Vec3<TNum>, TNum>(pts);
    }

    private IEnumerable<TNum> GetAngularDevPolylineSteps(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var angleRange = GetTotalAngleRange();
        if (angleRange.IsApproxZero())
            return [TNum.Zero, TNum.One];
        var (count, countNum, _) = tessellationParameter.GetStepsForAngle(angleRange);
        var stepSize = TNum.One / (countNum);
        return Enumerable.Range(0, count + 1).Select(i => TNum.CreateTruncating(i) * stepSize);

    }

    public TNum GetTotalAngleRange()
    {
        var diff = Line.AxisVector.X;
        var horizontalLength = TNum.Abs(diff);
        var relative = horizontalLength / Cylinder.Circumference;
        return relative * Numbers<TNum>.TwoPi;
    }

    public static Helix<TNum> BetweenPoints(in Cylinder<TNum> surface, Vec3<TNum> p1, Vec3<TNum> p2)
    {
        var start = Project(in surface, surface.ClampToSurface(p1));
        var end = Project(in surface, surface.ClampToSurface(p2));
        var curDist = TNum.Abs(start.X - end.X);
        var leftDist = TNum.Abs(start.X - (end.X - surface.Circumference));
        var rightDist = TNum.Abs(start.X - (end.X + surface.Circumference));
        TNum bestEndX;
        if (curDist <= leftDist && curDist <= rightDist)
            bestEndX = end.X;
        else if (leftDist <= rightDist)
            bestEndX = end.X - surface.Circumference;
        else
            bestEndX = end.X + surface.Circumference;
        end = new Vec2<TNum>(bestEndX, end.Y);
        return new Helix<TNum>(surface, start.LineTo(end));
    }


    /// <param name="surface">the cylinder around which to wrap</param>
    /// <param name="p">entrypoint</param>
    /// <param name="direction">direction at <paramref name="p"/></param>
    /// <returns>Helix that traverses to the next boundary</returns>
    /// <exception cref="ArithmeticException"></exception>
    public static Helix<TNum> FromOrigin(in Cylinder<TNum> surface, Vec3<TNum> p, Vec3<TNum> direction)
    {
        p = surface.ClampToSurface(p);
        direction = direction.Normalized();
        var localOrigin = Project(in surface, p);
        var localDir = ProjectDirection(in surface, p, direction);
        if (localDir.Y.IsApproxZero())
        {
            var localEnd = localOrigin + new Vec2<TNum>(surface.Circumference, TNum.Zero);
            var horizontalLine = localOrigin.LineTo(localEnd);
            return new Helix<TNum>(surface, horizontalLine);
        }

        var solveFor = TNum.IsPositive(localDir.Y) ? surface.Height : TNum.Zero;
        var diff = solveFor - localOrigin.Y;
        var scalar = diff/localDir.Y;
        var end = localOrigin+localDir*scalar;
        var line = localOrigin.LineTo(end);
        return new Helix<TNum>(surface, line);
    }

    [Pure]
    public Vec3<TNum> GetTangent(TNum t)
        => ProjectDirection(in Cylinder, Line.Traverse(t), Line.AxisVector);

    /// <inheritdoc />
    public bool Equals(Helix<TNum> other) => Cylinder.Equals(other.Cylinder) && Line.Equals(other.Line);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Helix<TNum> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Cylinder, Line);

    public static bool operator ==(Helix<TNum> left, Helix<TNum> right) => left.Equals(right);

    public static bool operator !=(Helix<TNum> left, Helix<TNum> right) => !left.Equals(right);

    public Ray3<TNum> GetRay(TNum t) => ProjectDirectionComplete(Cylinder, Line.Traverse(t), Line.Direction);

    public Helix<TNum> Section(TNum start, TNum end)
        => new(Cylinder, Line.Section(start, end));
}