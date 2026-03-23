using MeshWiz.Math;

namespace MeshWiz.Kinematics.Inverse;

public interface IInverseSolver
{
    bool TrySolve(KinematicChainState chain, Vec3<double> targetChainEndPoint, double epsilon = 1e-5);
}