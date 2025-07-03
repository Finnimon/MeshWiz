namespace MeshWiz.Math;

public interface IFlat<out TVector>
{
    public TVector Normal { get; }
}