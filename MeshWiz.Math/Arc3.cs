using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Arc3<TNum>(Circle3<TNum> underlying, TNum startAngle, TNum endAngle)
    : IFlat<TNum>, IDiscreteCurve<Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Circle3<TNum> Underlying = underlying;
    public readonly TNum StartAngle = startAngle, EndAngle = endAngle;
    public TNum AngleRange => TNum.Abs(EndAngle - StartAngle);

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
            return wrappedRange.IsApprox(TNum.Zero)||wrappedRange.IsApprox(Numbers<TNum>.TwoPi);
        }
    }


    /// <inheritdoc />
    public Vector3<TNum> TraverseOnCurve(TNum distance)
        => Traverse(TNum.Clamp(distance, TNum.Zero, TNum.One));


    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline() => ToPolyline(new PolylineTessellationParameter<TNum>
        { MaxAngularDeviation = Numbers<TNum>.Eps2 });

    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var maxAngleStep = TNum.Abs(tessellationParameter.MaxAngularDeviation);
        var angleRange = EndAngle - StartAngle;
        var absAngleRange = TNum.Abs(angleRange);
        var stepCount = int.CreateSaturating(TNum.Round(absAngleRange / maxAngleStep, MidpointRounding.AwayFromZero))+1;
        var pts = new Vector3<TNum>[stepCount];
        var angleStep = angleRange / TNum.CreateTruncating(stepCount-1);
        Console.WriteLine($"angleStep: {angleStep} maxAngleStep: {maxAngleStep}");
        var curAngle = StartAngle;
        for (var i = 0; i < stepCount; i++)
        {
            pts[i] = Underlying.TraverseByAngle(curAngle);
            curAngle += angleStep;
        }

        Console.WriteLine($"End:{EndAngle} actualEnd{curAngle-angleStep}");
        return new Polyline<Vector3<TNum>, TNum>(pts);
    }

    /// <inheritdoc />
    public Vector3<TNum> Traverse(TNum distance)
    {
        distance *= AngleRange;
        var pos = StartAngle + distance;
        return Underlying.TraverseByAngle(pos);
    }
}