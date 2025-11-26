using System.Diagnostics.Contracts;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Arc3<TNum>(Circle3<TNum> underlying, TNum startAngle, TNum endAngle)
    : IFlat<TNum>, IContiguousDiscreteCurve<Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Circle3<TNum> Underlying = underlying;
    public readonly TNum StartAngle = startAngle, EndAngle = endAngle;
    public TNum AngleRange => TNum.Abs(EndAngle - StartAngle);
    public TNum SignedAngleRange => EndAngle - StartAngle;

    public TNum WindingDirection => (EndAngle - StartAngle).EpsilonTruncatingSign() switch
    {
        -1 => TNum.NegativeOne,
        0 => TNum.Zero,
        1 => TNum.One,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<TNum>()
    };

    /// <inheritdoc />
    public TNum Length => AngleRange / Numbers<TNum>.TwoPi * Underlying.Length;

    /// <inheritdoc />
    public Plane3<TNum> Plane => Underlying.Plane;

    /// <inheritdoc />
    public Vector3<TNum> Normal => Underlying.Normal;

    public Vector3<TNum> CircumCenter => Underlying.Centroid;

    /// <inheritdoc />
    public Vector3<TNum> Start => Underlying.TraverseByAngle(StartAngle);

    /// <inheritdoc />
    public Vector3<TNum> End => Underlying.TraverseByAngle(EndAngle);

    /// <inheritdoc />
    public bool IsClosed
    {
        get
        {
            var wrappedRange = AngleRange.Wrap(TNum.Zero, Numbers<TNum>.TwoPi);
            return wrappedRange.IsApprox(TNum.Zero) || wrappedRange.IsApprox(Numbers<TNum>.TwoPi);
        }
    }


    /// <inheritdoc />
    public Vector3<TNum> TraverseOnCurve(TNum t)
        => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));


    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline() => ToPolyline(new PolylineTessellationParameter<TNum>
        { MaxAngularDeviation = Numbers<TNum>.Eps2 });

    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var maxAngleStep = TNum.Abs(tessellationParameter.MaxAngularDeviation);
        var angleRange = EndAngle - StartAngle;
        var absAngleRange = TNum.Abs(angleRange);
        var stepCount = int.CreateSaturating(TNum.Round(absAngleRange / maxAngleStep, MidpointRounding.AwayFromZero)) +
                        1;
        var pts = new Vector3<TNum>[stepCount];
        var angleStep = angleRange / TNum.CreateTruncating(stepCount - 1);
        Console.WriteLine($"angleStep: {angleStep} maxAngleStep: {maxAngleStep}");
        var curAngle = StartAngle;

        var (u, v) = Plane.Basis; // assumed normalized orthonormal basis


        for (var i = 0; i < stepCount; i++)
        {
            var cos = TNum.Cos(curAngle);
            var sin = TNum.Sin(curAngle);
            pts[i] = Underlying.Centroid + u * (cos * Underlying.Radius) + v * (sin * Underlying.Radius);
            curAngle += angleStep;
        }

        Console.WriteLine($"End:{EndAngle} actualEnd{curAngle - angleStep}");
        return new Polyline<Vector3<TNum>, TNum>(pts);
    }

    /// <inheritdoc />
    public Vector3<TNum> Traverse(TNum t)
    {
        var pos = GetAngleAtNormalPos(t);
        return Underlying.TraverseByAngle(pos);
    }

    [Pure]
    public TNum GetAngleAtNormalPos(TNum distance)
    {
        distance *= AngleRange;
        var pos = StartAngle + distance;
        return pos;
    }

    /// <inheritdoc />
    public Vector3<TNum> GetTangent(TNum at)
        => GetTangentAtAngle(GetAngleAtNormalPos(at));

    public Vector3<TNum> GetTangentAtAngle(TNum angle)
        => Underlying.GetTangentAtAngle(angle) * WindingDirection;

    /// <inheritdoc />
    public Vector3<TNum> EntryDirection => GetTangentAtAngle(StartAngle);

    /// <inheritdoc />
    public Vector3<TNum> ExitDirection => GetTangentAtAngle(EndAngle);
}