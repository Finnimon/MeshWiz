using System.Runtime.CompilerServices;
using MeshWiz.Math;
using MeshWiz.Utility.Extensions;
using OpenTK.Mathematics;

namespace MeshWiz.OpenGL;

public class OrbitCamera(float fovRad, Vec3<float> orbitAround, float distance, float azimuthRad, float pitchRad) : ICamera
{
    public float FovRad { get; set; } = fovRad;
    public Vec3<float> LookAt { get; set; } = orbitAround;

    public float Distance { get; set; } = distance;

    public Vec3<float> UnitUp
    {
        get;
        set => field = value.Normalized();
    }

    public Vec3<float> Position =>GetPosition();

    public float AzimuthRad
    {
        get;
        set => field = value.Wrap(-float.Pi,float.Pi);
    } = azimuthRad; // around UnitUp

    public float PitchRad { get; set; } = pitchRad;     // around CameraRight

    private Vec3<float> GetPosition()
    {
        var up = UnitUp;
        var reference = float.Abs(up.Dot(Vec3<float>.UnitY)) < 0.99f
                ? Vec3<float>.UnitY
                : Vec3<float>.UnitX;;
        var forward = up.Cross(reference).Normalized();

        var local =
            reference   * (Distance * float.Cos(PitchRad) * float.Cos(AzimuthRad)) +
            forward * (Distance * float.Cos(PitchRad) * float.Sin(AzimuthRad)) +
            up      * (Distance * float.Sin(PitchRad));

        return LookAt + local;
    }

    public Vec3<float> UnitRight()
    {
        var up=UnitUp;
        var dir= (LookAt - Position).Normalized();
        var reference = dir;
        if (dir.IsParallelTo(up)) throw new InvalidOperationException("Rot camera front by azimuth");
        return up.Cross(reference).Normalized();
    }


    public void MoveRight(float signedMovementScale) => AzimuthRad += signedMovementScale;
    public void LookRight(float signedMovementScale) => LookAt+=Distance*UnitRight()*signedMovementScale;

    public void LookUp(float signedMovementScale)
        => LookAt+=Distance*UnitUp*signedMovementScale;

    public void MoveUp(float signedMovementScalar) => PitchRad = MathHelper.Clamp(PitchRad + signedMovementScalar, -float.Pi / 2f + 0.01f, float.Pi / 2f - 0.01f);

    public void MoveForwards(float signedMovementScale) => Distance = float.Max(Distance - signedMovementScale, 0.01f);


    public (Mat4x4<float> view, Mat4x4<float> projection) CreateRenderMatrices(float aspect)
    {
        var view = Mat4x4<float>.CreateViewAt(GetPosition(), LookAt, UnitUp);
        var projection= Mat4x4<float>.CreatePerspectiveFov(FovRad, aspect, 0.001f,float.Max(1e5f,Distance * 1000));
        return (view, projection);
    }

    
    public static OrbitCamera Default() => new(float.Pi / 4, Vec3<float>.Zero, 1, 0, 0);

}