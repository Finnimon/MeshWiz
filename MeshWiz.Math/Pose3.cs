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
    public readonly Vec3<TNum> Origin; //quaternion first for better layout ie 32*4+32*3
    public Vec3<TNum> Position => Origin;
    public Vec3<TNum> X => Rotation.UnitX();
    public Vec3<TNum> Y => Rotation.UnitY();
    public Vec3<TNum> Z => Rotation.UnitZ();
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
            var mat = Rotation.AsMat3x3();
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
        var mat = Mat3x3<TNum>.Create(x, y, z);
        var rot = Quaternion<TNum>.CreateUnsafe(mat);
        return new Pose3<TNum>(rot, origin);
    }

    ///<summary></summary>
    /// <param name="origin"></param>
    /// <param name="front">Y</param>
    /// <param name="up">Z</param>
    [Pure]
    public static Pose3<TNum> CreateUnsafe(Vec3<TNum> origin, Vec3<TNum> front, Vec3<TNum> up)
    {
        var y = front.Normalized();
        var z = up;
        z -= y.Dot(z) * y;
        z = z.Normalized();
        var x = Vec3<TNum>.Cross(y, z).Normalized();
        var mat = Mat3x3<TNum>.Create(x, y, z);
        var rot = Quaternion<TNum>.CreateUnsafe(mat);
        return new Pose3<TNum>(rot, origin);
    }

    [Pure]
    public static Pose3<TNum> CreateUnsafe(Vec3<TNum> origin, Vec3<TNum> x, Vec3<TNum> y, Vec3<TNum> z)
    {
        var mat = Mat3x3<TNum>.Create(x, y, z);
        var rot = Quaternion<TNum>.CreateUnsafe(mat).Normalized();
        return new Pose3<TNum>(rot, origin);
    }

    public PoseLine<Pose3<TNum>, Vec3<TNum>, TNum> LineTo(Pose3<TNum> other) => new(this, other);


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
    public Vec3<TNum> Transform(Vec3<TNum> pt)
    {
        var translated = pt - Origin;
        return Rotation.Rotate(translated) + Origin;
    }

    public Vec3<TNum> TransformDir(Vec3<TNum> dir) => Rotation.Rotate(dir);


    public Pose3<TOtherNum> To<TOtherNum>()
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum>
    {
        if (typeof(TOtherNum) == typeof(TNum))
            return (Pose3<TOtherNum>)(object)this;
        return new Pose3<TOtherNum>(Rotation.To<TOtherNum>(), Origin.To<TOtherNum>());
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Pose3<TNum> other && this == other;

    /// <inheritdoc />
    public bool Equals(Pose3<TNum> other) => this == other;

    /// <inheritdoc />
    public static bool operator ==(Pose3<TNum> left, Pose3<TNum> right) =>
        left.Origin == right.Origin && left.Rotation == right.Rotation;

    /// <inheritdoc />
    public static bool operator !=(Pose3<TNum> left, Pose3<TNum> right)
        => !(left == right);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Rotation, Origin);

    public static Ray3<TNum> FrontRay(Pose3<TNum> arg) => new(arg.Origin, arg.Front);
    public static Ray3<TNum> UpRay(Pose3<TNum> arg) => new(arg.Origin, arg.Up);

    public PosedPlane<TNum> ToPosedPlane() => PosedPlane<TNum>.Create(this);

    public Plane<TNum> Xy() => Plane<TNum>.CreateUnsafe(Rotation.UnitZ(), Origin);

    public Mat4x4<TNum> AsMat4x4()
    {
        var rot = Rotation.AsMat3x3();
        return Mat4x4<TNum>.Create(
            Vec4<TNum>.Create(rot.X, Origin.X),
            Vec4<TNum>.Create(rot.Y, Origin.Y),
            Vec4<TNum>.Create(rot.Z, Origin.Z),
            Vec4<TNum>.UnitW
        );
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3<TNum> RotateAbout(Ray3<TNum> about, Angle<TNum> target)
    {
        var rot = Quaternion<TNum>.CreateFromAxisAngle(about, target);
        Pose3<TNum> p = new(rot, about.Origin);
        return p * this;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pose3<TNum> TransformBy(Mat3x3<TNum> mat)
    {
        var origin = mat * Origin;
        var rot = Rotation.AsMat3x3();
        return CreateUnsafe(origin, mat * rot.X, mat * rot.Y, mat * rot.Z);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3<TNum> TranslateBy(Vec3<TNum> direction) => new(Rotation, Origin + direction);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3<TNum> ToWorld(Pose3<TNum> local) => this * local;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pose3<TNum> operator *(Pose3<TNum> a, Pose3<TNum> b)
    {
        var rot = a.Rotation * b.Rotation;
        rot = rot.Normalized();
        var origin = a.TransformPoint(b.Origin);
        return new Pose3<TNum>(rot, origin);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Pose3<TNum> pose, TNum eps = default)
    {
        if (eps == default) eps = Numbers<TNum>.ZeroEpsilon;
        return Origin.IsApprox(pose.Origin, eps) && Quaternion<TNum>.AsVec4(Rotation).IsApprox(pose.Rotation);
    }

    /// <inheritdoc />
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> TransformPoint(Vec3<TNum> p) => Rotation.Rotate(p) + Origin;

    /// <inheritdoc />
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> TransformDirection(Vec3<TNum> v) => Rotation.Rotate(v);

    /// <inheritdoc />
    bool ISpatialTransform<Vec3<TNum>>.IsAffine => true;
}