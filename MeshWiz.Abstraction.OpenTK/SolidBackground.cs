using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class SolidBackground : IOpenGLControl
{
    public Color4 Color{get;set;}
    public void Dispose() { }

    public bool GLInitialized => true;
    public void Init() { }
    public void Update(float _) => GL.ClearColor(Color);
    public void Render()=>GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
}