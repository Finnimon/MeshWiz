using Avalonia.Media;
using MeshWiz.Abstraction.OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.Avalonia;

public class AvaloniaGLBackground : IOpenGLControl
{
    public Color Color{get;set;}

    private Color4 GLColor
        =>new (Color.R, Color.G, Color.B, Color.A);

    public void Dispose() { }
    public bool GLInitialized => true;
    public void Init() { }
    public void Update(float _) => GL.ClearColor(GLColor);
    public void Render()=>GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
}