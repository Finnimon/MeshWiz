namespace MeshWiz.Math;

public interface IShape<out TVector>
{
    TVector Centroid { get; }
}