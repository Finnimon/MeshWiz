using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MeshWiz.Math;

namespace MeshWiz.Kinematics;

public interface IAxis : IEquatable<IAxis>
{
    [System.Diagnostics.Contracts.Pure]
    public AxisType AxisType { get; } 
    /// <summary>
    /// The Pose for the next node to be attaches
    /// </summary>
    public Pose3<double> Connector { get; }
    /// <summary>
    /// start and end of <see cref="Range"/> are equivalent, and movement between both can be done continuously
    /// </summary>
    public bool IsWrapping { get; }
    /// <summary>
    /// The available movement range used by <see cref="CanReach"/>
    /// </summary>
    [System.Diagnostics.Contracts.Pure]
    public AABB<double> Range { get; }
    [System.Diagnostics.Contracts.Pure]
    public bool CanReach(double target);
    /// <param name="target">Target axis value</param>
    /// <param name="transformedConnector">resulting <see cref="Connector"/></param>
    /// <returns>whether <paramref name="target"/> could be reached</returns>
    [System.Diagnostics.Contracts.Pure]
    public bool TryReach(double target, out Pose3<double> transformedConnector);
    /// <param name="target">Target axis value</param>
    /// <returns>resulting <see cref="Connector"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">when <see cref="CanReach"/> for <paramref name="target"/> is false</exception>
    [MustUseReturnValue]
    public Pose3<double> Reach(double target);

    /// <param name="target">Target axis value</param>
    /// <returns>theoretical <see cref="Connector"/> at <paramref name="target"/></returns>
    /// <remarks>this does not validate inputs</remarks>
    [System.Diagnostics.Contracts.Pure]
    public Pose3<double> ForceReach(double target);
}

public enum KinematicNodeType
{
    Splitter,
    Axis,
}
public interface IKinematicNode
{
    public IReadOnlyList<Pose3<double>> Connectors { get; }
    public KinematicNodeType NodeType { get; }
}
