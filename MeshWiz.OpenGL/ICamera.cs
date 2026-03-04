using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.OpenGL;


public interface ICamera
{
    public Vec3<float> UnitUp { get; set; }
    public float FovRad { get; set; }
    public void MoveRight(float signedMovementScale);
    public void LookRight(float signedMovementScale);
    public void LookUp(float signedMovementScale);
    public void MoveForwards(float signedMovementScale);
    public void MoveUp(float signedMovementScalar);
    public Vec3<float> Position { get; }
    public Vec3<float> LookAt { get; set; }
    public ( Mat4x4<float> view, Mat4x4<float> projection) CreateRenderMatrices(float aspect);

}