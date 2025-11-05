using System.Numerics;

namespace MeshWiz.Utility;

public interface IResult<TSelf, out TInfo> : IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>
    where TSelf : IResult<TSelf, TInfo>

{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    TInfo Info { get; }
}