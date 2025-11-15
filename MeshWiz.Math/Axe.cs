using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct AxisSystem3<TNum> where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> Position, Front, Up;
    public Vector3<TNum> Origin => Position;
    public Vector3<TNum> X => Front;
    public Vector3<TNum> Y => Up.Cross(Front);
    public Vector3<TNum> Z => Up;

    public static AxisSystem3<TNum> World { get; } = new AxisSystem3<TNum>();


    private AxisSystem3(Vector3<TNum> position, Vector3<TNum> front, Vector3<TNum> up)
    {
        Position = position;
        Front = front;
        Up = up;
    }

    public AxisSystem3() : this(Vector3<TNum>.Zero, Vector3<TNum>.UnitX, Vector3<TNum>.UnitZ) { }

    [Pure]
    public static Result<Arithmetics, AxisSystem3<TNum>> CreateAffine(Vector3<TNum> position, Vector3<TNum> front,
        Vector3<TNum> up)
    {
        var finite = Vector3<TNum>.IsFinite(position) && Vector3<TNum>.IsFinite(front) && Vector3<TNum>.IsFinite(up);
        if (!finite)
            return Result<Arithmetics, AxisSystem3<TNum>>.Failure(Arithmetics.NonFiniteArguments);
        var sheering = !front.IsPerpendicularTo(up);
        if (sheering)
            return Result<Arithmetics, AxisSystem3<TNum>>.Failure(Arithmetics.SheeringDisallowed);
        AxisSystem3<TNum> created = new(position, front.Normalized, up.Normalized);
        return Result<Arithmetics, AxisSystem3<TNum>>.Success(created);
    }

    [Pure]
    public static Result<Arithmetics, AxisSystem3<TNum>> Create(Vector3<TNum> position, Vector3<TNum> front,
        Vector3<TNum> up)
    {
        if(!Vector3<TNum>.IsFinite(position))
            return Result<Arithmetics, AxisSystem3<TNum>>.Failure(Arithmetics.NonFiniteArguments);
        up = up.Normalized;
        front = front.Normalized;
        if (!up.IsNormalized || !front.IsNormalized)
            return Result<Arithmetics, AxisSystem3<TNum>>.Failure(Arithmetics.NormalizationImpossible);
        AxisSystem3<TNum> created = new(position, front, up);
        return Result<Arithmetics, AxisSystem3<TNum>>.Success(created);
    }

    [Pure]
    public static AxisSystem3<TNum> Lerp(in AxisSystem3<TNum> a, in AxisSystem3<TNum> b, TNum t)
    {
        var nestedResult = Vector3<Vector3<TNum>>.Lerp(AsNested(in a), AsNested(in b), Vector3<TNum>.FromValue(t));
        return Create(nestedResult.X, nestedResult.Y, nestedResult.Z);
    }

    private static Vector3<Vector3<TNum>> AsNested(in AxisSystem3<TNum> sys)
        => Unsafe.As<AxisSystem3<TNum>, Vector3<Vector3<TNum>>>(ref Unsafe.AsRef(in sys));
}