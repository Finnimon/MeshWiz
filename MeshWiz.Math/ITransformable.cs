namespace MeshWiz.Math;

public interface ITransformable<out TSelf, TVec>
    where TSelf : ITransformable<TSelf,TVec>
{
    TSelf TransformedBy<TTransform>(TTransform transform)
        where TTransform : ISpatialTransform<TVec>;
}