using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using JetBrains.Annotations;
using MeshWiz.Math;

namespace MeshWiz.Kinematics;

public sealed record RotationalAxis(AABB<double> Range, Ray3<double> About, Pose3<double> Connector, bool IsWrapping) 
    : IAxis
{
    [System.Diagnostics.Contracts.Pure]
    public AxisType AxisType=>AxisType.Rotational;

    /// <inheritdoc />
    public bool CanReach(double target) => CanReach(Angle<double>.FromRadians(target));

    /// <inheritdoc />
    public bool TryReach(double target, out Pose3<double> transformedConnector)
        => TryReach(Angle<double>.FromRadians(target), out transformedConnector);

    /// <inheritdoc />
    public Pose3<double> Reach(double target)
        => Reach(Angle<double>.FromRadians(target));

    [System.Diagnostics.Contracts.Pure]
    public bool CanReach(Angle<double> angle) => IsWrapping || Range.Contains(angle);

    [System.Diagnostics.Contracts.Pure]
    public bool TryReach(Angle<double> angle, out Pose3<double> transformedConnector)
    {
        if (!CanReach(angle))
        {
            Unsafe.SkipInit(out transformedConnector);
            return false;
        }
        transformedConnector = ForceReach(angle);
        return true;
    }

    /// <inheritdoc cref="Reach(double)"/>
    [MustUseReturnValue]
    public Pose3<double> Reach(Angle<double> target)
    {
        if(!CanReach(target)) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(target));
        return ForceReach(target);
    }
    
    /// <inheritdoc cref="ForceReach(double)"/>
    [System.Diagnostics.Contracts.Pure]
    public Pose3<double> ForceReach(Angle<double> target) => Connector.RotateAbout(About, target);

    ///<inheritdoc />
    [System.Diagnostics.Contracts.Pure] 
    public Pose3<double> ForceReach(double target) => Connector.RotateAbout(About, target);

    
    /// <inheritdoc />
    public bool Equals(IAxis? other) => other is RotationalAxis f && this.Equals(f);

    public bool Equals(RotationalAxis? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || Connector.Equals(other.Connector);
    }
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Connector, AxisType, Range);

    public static RotationalAxis CreateWrapping(Ray3<double> about, Pose3<double> connector)
        => new(AABB.From(-double.Pi, double.Pi), about, connector, true);
    public static RotationalAxis CreateNonWrapping(AABB<double> radiansRange, Ray3<double> about, Pose3<double> connector)
    =>new(radiansRange, about, connector, false);
}