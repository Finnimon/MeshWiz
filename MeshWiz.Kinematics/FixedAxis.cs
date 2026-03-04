using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Math;

namespace MeshWiz.Kinematics;

public sealed record FixedAxis(Pose3<double> Connector) : IAxis
{
    /// <inheritdoc />
    public AxisType AxisType => AxisType.Fixed;

    /// <inheritdoc />
    public AABB<double> Range => AABB.From(0.0);

    /// <inheritdoc />
    public bool IsWrapping => true;

    /// <inheritdoc />
    bool IAxis.CanReach(double target)
        => target.Equals(0.0);

    /// <inheritdoc />
    bool IAxis.TryReach(double target, out Pose3<double> transformedConnector)
    {
        if(!target.Equals(0.0))
        {
            Unsafe.SkipInit(out transformedConnector);
            return false;
        }

        transformedConnector = Connector;
        return true;
    }

    /// <inheritdoc />
    Pose3<double> IAxis.Reach(double target) =>
        target is 0.0 ? Connector : ThrowHelper.ThrowNotSupportedException<Pose3<double>>();

    /// <inheritdoc />
    Pose3<double> IAxis.ForceReach(double target) => Connector;

    /// <inheritdoc />
    public bool Equals(IAxis? other) => other is FixedAxis f && this.Equals(f);

    /// <inheritdoc />
    public bool Equals(FixedAxis? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || Connector.Equals(other.Connector);
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Connector, AxisType);
}