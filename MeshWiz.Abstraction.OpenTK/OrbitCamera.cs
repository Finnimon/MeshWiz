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
        set => field = value.Normalized;
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
        var right = UnitRight();
        var forward = up.Cross(right).Normalized;

        var local =
            right   * (Distance * float.Cos(PitchRad) * float.Cos(AzimuthRad)) +
            forward * (Distance * float.Cos(PitchRad) * float.Sin(AzimuthRad)) +
            up      * (Distance * float.Sin(PitchRad));

        return LookAt + local;
    }

    public Vector3<float> UnitRight()
    {
        var up=UnitUp;
        var reference = float.Abs(up*Vector3<float>.UnitY) < 0.99f
            ? Vector3<float>.UnitY
            : Vector3<float>.UnitX;
        return reference.Cross(up).Normalized;
    }


    public void MoveToSides(float signedMovementScale) => AzimuthRad += signedMovementScale;
    public void LookRight(float signedMovementScale) => LookAt+=UnitRight()*signedMovementScale;

    public void LookUp(float signedMovementScale)
        => LookAt+=UnitUp*signedMovementScale;

    public void MoveUp(float signedMovementScalar) => PitchRad = MathHelper.Clamp(PitchRad + signedMovementScalar, -float.Pi / 2f + 0.01f, float.Pi / 2f - 0.01f);

    public void MoveForwards(float signedMovementScale) => Distance = float.Max(Distance - signedMovementScale, 0.01f);


    public (Matrix4 model, Matrix4 view, Matrix4 projection) CreateRenderMatrices(float aspect)
    {
        var model = Matrix4.Identity;
        var view = Matrix4.LookAt(GetPosition().ToOpenTK(), LookAt.ToOpenTK(), UnitUp.ToOpenTK());
        var projection = Matrix4.CreatePerspectiveFieldOfView(FovRad, aspect, 0.001f, Distance*4);
        return (model, view, projection);
    }

    public static ICamera Default()
        =>    new OrbitCamera(float.Pi / 4, Vector3<float>.Zero, 1, 0, 0);

}