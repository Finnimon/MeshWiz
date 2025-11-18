namespace MeshWiz.Math;

public interface IPosition<TSelf, out TPosition, TNum> : IDistance<TSelf, TNum>
    where TSelf : IPosition<TSelf, TPosition, TNum>
    where TPosition : IPosition<TPosition, TPosition, TNum>
{
    TPosition Position { get; }
}