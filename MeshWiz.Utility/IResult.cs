using System.Numerics;

namespace MeshWiz.Utility;

public interface IResult<TSelf, TInfo> : IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>
    where TSelf : struct, IResult<TSelf, TInfo>

{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    TInfo Info { get; }
    static abstract TSelf Failure(TInfo info);
    static abstract TSelf Success();
}