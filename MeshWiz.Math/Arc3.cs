using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[method: JsonConstructor]
public readonly struct Arc3<TNum>(Circle3<TNum> underlying, TNum startAngle, TNum endAngle)
    : IFlat<TNum>, IContiguousDiscreteCurve<Vec3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonInclude]
    public readonly Circle3<TNum> Underlying = underlying;
    [JsonInclude]
    public readonly TNum StartAngle = startAngle, EndAngle = endAngle;
    [JsonIgnore]
    public TNum AngleRange => TNum.Abs(EndAngle - StartAngle);
    [JsonIgnore]
    public TNum SignedAngleRange => EndAngle - StartAngle;

    [JsonIgnore]
    public TNum WindingDirection => (EndAngle - StartAngle).EpsilonTruncatingSign() switch
    {
        -1 => TNum.NegativeOne,
        0 => TNum.Zero,
        1 => TNum.One,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<TNum>()
    };

    /// <inheritdoc />
    [JsonIgnore]
    public TNum Length => AngleRange / Numbers<TNum>.TwoPi * Underlying.Length;

    /// <inheritdoc />
    [JsonIgnore]
    public Plane<TNum> Plane => Underlying.Plane;

    /// <inheritdoc />
    [JsonIgnore]
    public Vec3<TNum> Normal => Underlying.Normal;

    [JsonIgnore]
    public Vec3<TNum> CircumCenter => Underlying.Centroid;

    /// <inheritdoc />
    [JsonIgnore]
    public Vec3<TNum> Start => Underlying.TraverseByAngle(StartAngle);

    /// <inheritdoc />
    [JsonIgnore]
    public Vec3<TNum> End => Underlying.TraverseByAngle(EndAngle);

    /// <inheritdoc />
    [JsonIgnore]
    public bool IsClosed
    {
        get
        {
            var wrappedRange = AngleRange.Wrap(TNum.Zero, Numbers<TNum>.TwoPi);
            return wrappedRange.IsApprox(TNum.Zero) || wrappedRange.IsApprox(Numbers<TNum>.TwoPi);
        }
    }


    /// <inheritdoc />
    public Vec3<TNum> TraverseOnCurve(TNum t)
        => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));


    /// <inheritdoc />
    public Polyline<Vec3<TNum>, TNum> ToPolyline() => ToPolyline(new PolylineTessellationParameter<TNum>
        { MaxAngularDeviation = Numbers<TNum>.Eps2 });

    /// <inheritdoc />
    public Polyline<Vec3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var maxAngleStep = TNum.Abs(tessellationParameter.MaxAngularDeviation);
        var angleRange = EndAngle - StartAngle;
        var absAngleRange = TNum.Abs(angleRange);
        var stepCount = int.CreateSaturating(TNum.Round(absAngleRange / maxAngleStep, MidpointRounding.AwayFromZero)) +
                        1;
        var pts = new Vec3<TNum>[stepCount];
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
        return new Polyline<Vec3<TNum>, TNum>(pts);
    }

    /// <inheritdoc />
    public Vec3<TNum> Traverse(TNum t)
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
    public Vec3<TNum> GetTangent(TNum t)
        => GetTangentAtAngle(GetAngleAtNormalPos(t));

    public Vec3<TNum> GetTangentAtAngle(TNum angle)
        => Underlying.GetTangentAtAngle(angle) * WindingDirection;

    /// <inheritdoc />
    [JsonIgnore]
    public Vec3<TNum> EntryDirection => GetTangentAtAngle(StartAngle);

    /// <inheritdoc />
    [JsonIgnore]
    public Vec3<TNum> ExitDirection => GetTangentAtAngle(EndAngle);
    
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Arc3<TOther> To<TOther>()
        where TOther : unmanaged, IFloatingPointIeee754<TOther>
    =>new(Underlying.To<TOther>(), TOther.CreateTruncating(StartAngle),TOther.CreateTruncating(EndAngle));
}