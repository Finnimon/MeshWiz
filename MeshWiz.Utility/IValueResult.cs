using CommunityToolkit.Diagnostics;

namespace MeshWiz.Utility;

public interface IValueResult<TSelf, TInfo, TValue> : IResult<TSelf, TInfo>
    where TSelf : struct,IValueResult<TSelf, TInfo,TValue>
{
    TValue Value { get; }
    bool HasValue { get; }
    
    static abstract TSelf Success(TValue value);
    new static abstract TSelf Failure(TInfo info);

    /// <inheritdoc />
    static TSelf IResult<TSelf, TInfo>.Success()
        => ThrowHelper.ThrowNotSupportedException<TSelf>();
}