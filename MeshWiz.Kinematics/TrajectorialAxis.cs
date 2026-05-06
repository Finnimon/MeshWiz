using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Math;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Kinematics;

public sealed record TrajectorialAxis<TCurve>(AABB<double> Range, TCurve Trajectory, bool IsWrapping, double DefaultPosition) : IAxis
where TCurve : IPoseCurve<Pose3<double>, Vec3<double>, double>
{
    public Pose3<double> Connector => Trajectory.GetPose(DefaultPosition);

    /// <inheritdoc />
    public AxisType AxisType => AxisType.Trajectory;
    [System.Diagnostics.Contracts.Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanReach(double target) => IsWrapping||Range.Contains(target);
    [System.Diagnostics.Contracts.Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReach(double target, out Pose3<double> transformedConnector)
    {
        if (!CanReach(target))
        {
            Unsafe.SkipInit(out transformedConnector);
            return false;
        }

        if (IsWrapping) target = target.Wrap(Range.Min, Range.Max);
        transformedConnector = Trajectory.GetPose(target);
        return true;
    }

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3<double> Reach(double target)
    {
        if(!TryReach(target,out var res))
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(target));
        return res;
    }

    /// <inheritdoc />
    public Pose3<double> ForceReach(double target) => Trajectory.GetPose(target);

    /// <inheritdoc />
    public bool Equals(IAxis? other) => other is TrajectorialAxis<TCurve> typed && typed == this;

    public static TrajectorialAxis<TCurve> CreateWrapping(AABB<double> range,
        TCurve trajectory,
        double defaultPosition)
    {
        if(!range.Contains(defaultPosition)) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(defaultPosition));
        var canBeWrapping= trajectory.GetPose(range.Min).IsApprox(trajectory.GetPose(range.Max));
        if(!canBeWrapping) ThrowHelper.ThrowArgumentException(nameof(range));
        return new TrajectorialAxis<TCurve>(range, trajectory, true, defaultPosition);
    }
}