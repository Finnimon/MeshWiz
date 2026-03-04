using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Math;

namespace MeshWiz.Kinematics;

public sealed class TranslationalAxis(Vec3<double> direction, AABB<double> range, Pose3<double> connector)
    : IAxis
{

    /// <inheritdoc />
    public Pose3<double> Connector { get; } = connector;

    /// <inheritdoc />
    public bool IsWrapping => false;

    /// <inheritdoc />
    public AxisType AxisType => AxisType.Translational;
    public AABB<double> Range { get; } = range;
    public Vec3<double> Direction { get; } = direction.Normalized();


    /// <inheritdoc />
    public bool CanReach(double target) => Range.Contains(target);

    /// <inheritdoc />
    public bool TryReach(double target, out Pose3<double> transformedConnector)
    {
        if (!CanReach(target))
        {
            Unsafe.SkipInit(out transformedConnector);
            return false;
        }

        transformedConnector = ForceReach(target);
        return true;
    }

    /// <inheritdoc />
    public Pose3<double> ForceReach(double target) => Connector.TranslateBy(Direction * target);

    /// <inheritdoc />
    public Pose3<double> Reach(double target)
    {
        if(!CanReach(target)) ThrowHelper.ThrowArgumentOutOfRangeException();
        return ForceReach(target);
    }

    /// <inheritdoc />
    public bool Equals(IAxis? other) => Equals(other as TranslationalAxis);

    public bool Equals(TranslationalAxis? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || Connector.Equals(other.Connector);
    }
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Connector, AxisType, Range);

}