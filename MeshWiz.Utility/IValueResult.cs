namespace MeshWiz.Utility;

public interface IValueResult<TSelf, out TValue, out TInfo> : IResult<TSelf, TInfo>
    where TSelf : IResult<TSelf, TInfo>
{
    TValue Value { get; }
}