namespace MeshWiz.Math;

public interface IShape<out TVec>
{
    TVec Centroid { get; }
}