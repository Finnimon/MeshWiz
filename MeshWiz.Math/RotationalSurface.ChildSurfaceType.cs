namespace MeshWiz.Math;

public sealed partial record RotationalSurface<TNum>
{
    public enum ChildSurfaceType
    {
        /// <summary>
        /// Is not usable because of 0 len
        /// </summary>
        Dead=0,
        Cylinder,
        Cone,
        ConeSection,
        Circle,
        CircleSection,
    }
}