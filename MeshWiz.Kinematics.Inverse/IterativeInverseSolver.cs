using MeshWiz.Math;

namespace MeshWiz.Kinematics.Inverse;

public sealed class IterativeInverseSolver:IInverseSolver
{
    /// <inheritdoc />
    public bool TrySolve(KinematicChainState chain, Vec3<double> targetChainEndPoint, double epsilon = 1E-05)
    {
        var poses = chain.AbsoluteConnectors;
        if (poses[^1].Origin.IsApprox(targetChainEndPoint, epsilon)) return true;
        Span<double> axisValues=stackalloc double[poses.Length];
        chain.State.CopyTo(axisValues);
        
        
        
        throw new NotImplementedException();
    }
}