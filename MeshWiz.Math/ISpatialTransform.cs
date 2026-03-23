using System.Diagnostics.Contracts;

namespace MeshWiz.Math;

public interface ISpatialTransform<TVec>
{
    [Pure]
    TVec TransformPoint(TVec p);

    [Pure]
    TVec TransformDirection(TVec v);

    bool IsAffine { get; }
}