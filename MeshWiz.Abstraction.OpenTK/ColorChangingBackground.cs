using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class ColorChangingBackground : IOpenGLControl
{
    public int RotationMillis { get; set; }
    public Color4 From{get;set;}
    public Color4 To{get;set;}
    
    private Color4 CurrentColor{get;}
    public void Dispose() { }

    public bool GLInitialized { get; set; }
    public void Init() { }
    public void Update(float _) => GL.ClearColor(CurrentColor);
    public void Render()=>GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);

    private void GetColor()
    {
        var from = From.ToVec4();
        var to = To.ToVec4();
    }
}