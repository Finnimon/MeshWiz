using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public interface IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public IContiguousCurve<Vector3<TNum>, TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2);
    public IContiguousCurve<Vector3<TNum>, TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction);
}

public interface IGeodesicProvider<TCurve, TNum> :IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TCurve:IContiguousCurve<Vector3<TNum>, TNum>
{
    public new TCurve GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2);
    public new TCurve GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction);

    /// <inheritdoc />
    IContiguousCurve<Vector3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2) 
        => GetGeodesic(p1, p2);

    /// <inheritdoc />
    IContiguousCurve<Vector3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction) 
        => GetGeodesicFromEntry(entryPoint, direction);
}

public interface IContiguousCurve<TVector, TNum> : ICurve<TVector,TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
{
    public TVector GetTangent(TNum at);
}

public interface IContiguousDiscreteCurve<TVector, TNum> : IContiguousCurve<TVector, TNum>,
    IDiscreteCurve<TVector, TNum> 
    where TVector : unmanaged, IFloatingVector<TVector, TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public TVector EntryDirection { get; }
    public TVector ExitDirection { get; }
}

public readonly struct Helix<TNum> : IContiguousDiscreteCurve<Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Cylinder<TNum> Cylinder;
    public readonly Line<Vector2<TNum>, TNum> Line;
    public TNum Length => Line.Length;
    public Vector3<TNum> Start => Project(in Cylinder, Line.Start);
    public Vector3<TNum> End => Project(in Cylinder, Line.End);
    public Vector3<TNum> EntryDirection =>GetTangent(TNum.Zero);
    public Vector3<TNum> ExitDirection => GetTangent(TNum.One);

    public Helix(Cylinder<TNum> cylinder, Line<Vector2<TNum>, TNum> line)
    {
        Cylinder = cylinder;
        Line = line;
    }


    [Pure]
    public static Vector3<TNum> Project(in Cylinder<TNum> cylinder, Vector2<TNum> p)
    {
        var baseCircle = cylinder.Base;
        var xPos = p.X / baseCircle.Circumference;
        var atBase = baseCircle.Traverse(xPos);
        var shift = cylinder.Axis.NormalDirection * p.Y;
        return atBase + shift;
    }

    [Pure]
    public static Vector2<TNum> Project(in Cylinder<TNum> cylinder, Vector3<TNum> p)
    {
        var axisDir = cylinder.Axis.NormalDirection;
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

        return new Vector2<TNum>(x, y);
    }

    [Pure]
    public static Vector2<TNum> ProjectDirection(in Cylinder<TNum> cylinder, Vector3<TNum> p, Vector3<TNum> direction)
    {
        direction = direction.Normalized;
        var axisDir = cylinder.Axis.NormalDirection;
        var closest = cylinder.Axis.ClosestPoint(p);
        var axisToP = (p - closest).Normalized;

        var tangentialDir = axisDir.Cross(axisToP).Normalized;

        var dxCircumference = tangentialDir.Dot(direction) * cylinder.Radius; // linear distance along circumference
        var dy = axisDir.Dot(direction);                                       // along cylinder axis

        return new Vector2<TNum>(dxCircumference, dy).Normalized;
    }

    [Pure]
    public static Vector3<TNum> ProjectDirection(in Cylinder<TNum> cylinder, Vector2<TNum> p, Vector2<TNum> direction)
    {
        direction = direction.Normalized;

        var p3 = Project(in cylinder, p);
        var axisDir = cylinder.Axis.NormalDirection;
        var closest = cylinder.Axis.ClosestPoint(p3);
        var axisToP = (p3 - closest).Normalized;

        var tangentialDir = axisDir.Cross(axisToP).Normalized;

        var dx = direction.X;
        var dy = direction.Y;

        var world = tangentialDir * dx + axisDir * dy;
        return world.Normalized;
    }


    /// <inheritdoc />
    public Vector3<TNum> TraverseOnCurve(TNum distance) => Traverse(TNum.Clamp(distance, TNum.Zero, TNum.One));

    /// <inheritdoc />
    public Vector3<TNum> Traverse(TNum distance)
        => Project(in Cylinder, Line.Traverse(distance));

    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline() =>
        ToPolyline(new PolylineTessellationParameter<TNum>
            { MaxAngularDeviation = Numbers<TNum>.TwoPi * Numbers<TNum>.Eps2 });

    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var angleRange = GetTotalAngleRange();
        if (angleRange.IsApproxZero()) return new Polyline<Vector3<TNum>, TNum>(Start, End);
        var stepCount = tessellationParameter.GetStepsForAngle(angleRange).countNum;
        var stepSize = TNum.One / stepCount;
        var pts = Enumerable.Sequence(TNum.Zero, TNum.One, stepSize)
            .Select(Traverse)
            .ToArray();
        return new Polyline<Vector3<TNum>, TNum>(pts);
    }

    public TNum GetTotalAngleRange()
    {
        var diff = Line.Direction.X;
        var horizontalLength = TNum.Abs(diff);
        var relative = horizontalLength / Cylinder.Circumference;
        return relative * Numbers<TNum>.TwoPi;
    }

    public static Helix<TNum> BetweenPoints(in Cylinder<TNum> surface, Vector3<TNum> p1, Vector3<TNum> p2)
    {
        var start = Project(in surface, surface.ClampToSurface(p1));
        var end=Project(in surface, surface.ClampToSurface(p2));
        return new Helix<TNum>(surface, start.LineTo(end));
    }
    
    
    /// <param name="surface">the cylinder around which to wrap</param>
    /// <param name="p">entrypoint</param>
    /// <param name="direction">direction at <paramref name="p"/></param>
    /// <returns>Helix that traverses to the next boundary</returns>
    /// <exception cref="ArithmeticException"></exception>
    public static Helix<TNum> FromOrigin(in Cylinder<TNum> surface, Vector3<TNum> p, Vector3<TNum> direction)
    {
        p=surface.ClampToSurface(p);
        direction=direction.Normalized;
        var localOrigin = Project(in surface, p);
        var localDir = ProjectDirection(in surface, p, direction);
        if (localDir.Y.IsApproxZero())
        {
            var localEnd = localOrigin + new Vector2<TNum>(surface.Circumference,TNum.Zero); 
            var horizontalLine = localOrigin.LineTo(localEnd);
            return new Helix<TNum>(surface, horizontalLine);
        }

        var curEndPt = localDir + localOrigin;
        var solveFor = TNum.IsPositive(localDir.Y) ? surface.Height : TNum.Zero;
        var success= Solver.Linear.TrySolve(localOrigin.Y, curEndPt.Y, solveFor, out var scalar);
        if (!success) throw new ArithmeticException();
        var end= Vector2<TNum>.Lerp(localOrigin,curEndPt , scalar);
        var line = localOrigin.LineTo(end);
        return new Helix<TNum>(surface, line);
    }

    [Pure]
    public Vector3<TNum> GetTangent(TNum at)
        => ProjectDirection(in Cylinder, Line.Traverse(at), Line.Direction);
}