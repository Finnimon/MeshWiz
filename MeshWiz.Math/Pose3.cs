using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Pose3<TNum> : IPose<Pose3<TNum>, Vec3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{

    public static Pose3<TNum> Identity => new();
    public readonly Quaternion<TNum> Rotation;
    public readonly Vec3<TNum> Origin;//quaternion first for better layout ie 32*4+32*3
    public Vec3<TNum> Position => Origin;
    public Vec3<TNum> Front => Rotation.UnitY();
    public Vec3<TNum> Up => Rotation.UnitZ();
    public Vec3<TNum> Right => Rotation.UnitX();
    public Vec3<TNum> Back => -Front;
    public Vec3<TNum> Left => -Right;
    public Vec3<TNum> Down => -Up;

    public (Vec3<TNum> Front, Vec3<TNum> Up, Vec3<TNum> Right) Orientation
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
        Origin = Vec3<TNum>.Zero;
        Rotation = Quaternion<TNum>.Identity;
    }

    public Pose3(Quaternion<TNum> rotation, Vec3<TNum> origin)
    {
        Rotation = rotation;
        Origin = origin;
    }

    [Pure]
    public static Result<Arithmetics, Pose3<TNum>> CreateFromOrientation(Vec3<TNum> origin, Vec3<TNum> front,
        Vec3<TNum> up)
    {
        if (!Vec3<TNum>.IsFinite(origin))
            return Result<Arithmetics, Pose3<TNum>>.Failure(Arithmetics.NonFiniteArguments);
        var y = front.Normalized();
        var z = up.Normalized();
        var diff = y.Dot(up) * y;
        z -= diff;
        z = z.Normalized();
        var x = Vec3<TNum>.Cross(y, z);
        if (!z.IsNormalized || !y.IsNormalized)
            return Result<Arithmetics, Pose3<TNum>>.Failure(Arithmetics.NormalizationImpossible);
        Matrix3x3<TNum> mat = new(x, y, z);
        var rot = Quaternion<TNum>.CreateUnsafe(in mat);
        return new Pose3<TNum>(rot, origin);
    }

    [Pure]
    public static Pose3<TNum> CreateUnsafe(Vec3<TNum> origin, Vec3<TNum> front, Vec3<TNum> up)
    {
        var y = front.Normalized();
        var z = up;
        z -= y.Dot(z) * y;
        z = z.Normalized();
        var x = Vec3<TNum>.Cross(y, z).Normalized();
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
    public static TNum Distance(Pose3<TNum> a, Pose3<TNum> b) => Vec3<TNum>.Distance(a.Origin, b.Origin);

    /// <inheritdoc />
    public static TNum SquaredDistance(Pose3<TNum> a, Pose3<TNum> b) =>
        Vec3<TNum>.SquaredDistance(a.Origin, b.Origin);


    /// <inheritdoc />
    public static Pose3<TNum> Lerp(Pose3<TNum> a, Pose3<TNum> b, TNum t)
        => new(Quaternion<TNum>.Slerp(a.Rotation, b.Rotation, t), Vec3<TNum>.Lerp(a.Origin, b.Origin, t));

    /// <inheritdoc />
    public static Pose3<TNum> ExactLerp(Pose3<TNum> a, Pose3<TNum> b, TNum exactDistance)
    {
        var distance = a.DistanceTo(b);
        return Lerp(a, b, exactDistance / distance);
    }

    /// <inheritdoc />
    public Vec3<TNum> Transform(Vec3<TNum> src)
    {
        var translated = src - Origin;
        return Rotation.Rotate(translated);
    }

    public Pose3<TOtherNum> To<TOtherNum>() 
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum>
    {
        if (typeof(TOtherNum) == typeof(TNum))
            return (Pose3<TOtherNum>)(object) this;
        return new Pose3<TOtherNum>(Rotation.To<TOtherNum>(), Origin.To<TOtherNum>());
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Pose3<TNum> other && this == other;

    /// <inheritdoc />
    public bool Equals(Pose3<TNum> other) => this == other;

    /// <inheritdoc />
    public static bool operator ==(Pose3<TNum> left, Pose3<TNum> right) => left.Origin==right.Origin&&left.Rotation==right.Rotation;

    /// <inheritdoc />
    public static bool operator !=(Pose3<TNum> left, Pose3<TNum> right)
        => !(left == right);
    
    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Rotation, Origin);
    }

    public static Ray3<TNum> FrontRay(Pose3<TNum> arg) => new(arg.Origin, arg.Front);
    public static Ray3<TNum> UpRay(Pose3<TNum> arg) => new(arg.Origin, arg.Up);
}