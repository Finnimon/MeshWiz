using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Pose3<TNum> : IPose<Pose3<TNum>, Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static Pose3<TNum> Identity => new();
    public readonly Quaternion<TNum> Rotation;
    public readonly Vector3<TNum> Origin;//quaternion first for better layout ie 32*4+32*3
    public Vector3<TNum> Position => Origin;
    public Vector3<TNum> Front => Rotation.UnitY();
    public Vector3<TNum> Up => Rotation.UnitZ();
    public Vector3<TNum> Right => Rotation.UnitX();
    public Vector3<TNum> Back => -Front;
    public Vector3<TNum> Left => -Right;
    public Vector3<TNum> Down => -Up;

    public (Vector3<TNum> Front, Vector3<TNum> Up, Vector3<TNum> Right) Orientation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var mat = Rotation.AsMatrix3x3();
            return (mat.Y, mat.Z, mat.X);
        }
    }


    public Pose3()
    {
        Origin = Vector3<TNum>.Zero;
        Rotation = Quaternion<TNum>.Identity;
    }

    public Pose3(Quaternion<TNum> rotation, Vector3<TNum> origin)
    {
        Rotation = rotation;
        Origin = origin;
    }

    [Pure]
    public static Result<Arithmetics, Pose3<TNum>> CreateFromOrientation(Vector3<TNum> origin, Vector3<TNum> front,
        Vector3<TNum> up)
    {
        if (!Vector3<TNum>.IsFinite(origin))
            return Result<Arithmetics, Pose3<TNum>>.Failure(Arithmetics.NonFiniteArguments);
        var y = front.Normalized();
        var z = up.Normalized();
        var diff = y.Dot(up) * y;
        z -= diff;
        z = z.Normalized();
        var x = Vector3<TNum>.Cross(y, z);
        if (!up.IsNormalized || !front.IsNormalized)
            return Result<Arithmetics, Pose3<TNum>>.Failure(Arithmetics.NormalizationImpossible);
        Matrix3x3<TNum> mat = new(x, y, z);
        var rot = Quaternion<TNum>.CreateUnsafe(in mat);
        return new Pose3<TNum>(rot, origin);
    }


    /// <inheritdoc />
    public TNum DistanceTo(Pose3<TNum> other) => Origin.DistanceTo(other.Origin);

    /// <inheritdoc />
    public TNum SquaredDistanceTo(Pose3<TNum> other)
        => Origin.SquaredDistanceTo(other.Origin);

    /// <inheritdoc />
    public static TNum Distance(Pose3<TNum> a, Pose3<TNum> b) => Vector3<TNum>.Distance(a.Origin, b.Origin);

    /// <inheritdoc />
    public static TNum SquaredDistance(Pose3<TNum> a, Pose3<TNum> b) =>
        Vector3<TNum>.SquaredDistance(a.Origin, b.Origin);


    /// <inheritdoc />
    public static Pose3<TNum> Lerp(Pose3<TNum> a, Pose3<TNum> b, TNum t)
        => new(Quaternion<TNum>.Slerp(a.Rotation, b.Rotation, t), Vector3<TNum>.Lerp(a.Origin, b.Origin, t));

    /// <inheritdoc />
    public static Pose3<TNum> ExactLerp(Pose3<TNum> a, Pose3<TNum> b, TNum exactDistance)
    {
        var distance = a.DistanceTo(b);
        return Lerp(a, b, exactDistance / distance);
    }

    /// <inheritdoc />
    public Vector3<TNum> Transform(Vector3<TNum> src)
    {
        var translated = src - Origin;
        return Rotation.Rotate(translated);
    }
}