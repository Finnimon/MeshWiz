using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.OpenTK;


public interface ICamera
{
    public Vector3<float> UnitUp { get; set; }
    public float FovRad { get; set; }
    public void MoveRight(float signedMovementScale);
    public void LookRight(float signedMovementScale);
    public void LookUp(float signedMovementScale);
    public void MoveForwards(float signedMovementScale);
    public void MoveUp(float signedMovementScalar);
    public Vector3<float> Position { get; }
    public Vector3<float> LookAt { get; set; }
    public (Matrix4 model, Matrix4 view, Matrix4 projection) CreateRenderMatrices(float aspect);

    public (Matrix4 model, Matrix4 view, Matrix4 projection) CreateRenderMatrices(Vector2 bounds)
    => CreateRenderMatrices(bounds.X / bounds.Y);
}