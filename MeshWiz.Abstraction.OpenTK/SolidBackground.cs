using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class SolidBackground : IOpenGLControl
{
    public bool Show { get; set; } = true;
    public Color4 Color{get;set;}
    public void Dispose() { }

    public bool GLInitialized => true;
    public void Init() { }
    public void Update(float _) {}
    public void Render()
    {
        if(!Show) return;
        GL.ClearColor(Color);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }
}