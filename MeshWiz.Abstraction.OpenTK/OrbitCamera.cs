using System.Runtime.CompilerServices;
using MeshWiz.Math;
using MeshWiz.Utility.Extensions;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class OrbitCamera(float fovRad, Vector3<float> orbitAround, float distance, float azimuthRad, float pitchRad) : ICamera
{
    public float FovRad { get; set; } = fovRad;
    public Vector3<float> LookAt { get; set; } = orbitAround;

    public float Distance { get; set; } = distance;

    public Vector3<float> UnitUp
    {
        get;
        set => field = value.Normalized();
    }

    public Vector3<float> Position =>GetPosition();

    public float AzimuthRad
    {
        get;
        set => field = value.Wrap(-float.Pi,float.Pi);
    } = azimuthRad; // around UnitUp

    public float PitchRad { get; set; } = pitchRad;     // around CameraRight

    private Vector3<float> GetPosition()
    {
        var up = UnitUp;
        var reference = float.Abs(up.Dot(Vector3<float>.UnitY)) < 0.99f
                ? Vector3<float>.UnitY
                : Vector3<float>.UnitX;;
        var forward = up.Cross(reference).Normalized();

        var local =
            reference   * (Distance * float.Cos(PitchRad) * float.Cos(AzimuthRad)) +
            forward * (Distance * float.Cos(PitchRad) * float.Sin(AzimuthRad)) +
            up      * (Distance * float.Sin(PitchRad));

        return LookAt + local;
    }

    public Vector3<float> UnitRight()
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


    public (Matrix4 model, Matrix4 view, Matrix4 projection) CreateRenderMatrices(float aspect)
    {
        var model = Matrix4.Identity;
        var view = Matrix4x4<float>.CreateViewAt(GetPosition(), LookAt, UnitUp);
        var projection = Matrix4.CreatePerspectiveFieldOfView(FovRad, aspect, 0.001f, float.Max(100000,Distance * 1000));
        return (model,Unsafe.As<Matrix4x4<float>,Matrix4>(ref view), projection);
    }

    public static ICamera Default()
        =>    new OrbitCamera(float.Pi / 4, Vector3<float>.Zero, 1, 0, 0);

}