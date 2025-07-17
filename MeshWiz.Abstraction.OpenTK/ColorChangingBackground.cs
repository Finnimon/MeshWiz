using System.Diagnostics;
using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class ColorChangingBackground : IOpenGLControl
{
    public int RotationMillis { get; set; } = 10000;
    public Color4 From{get;set;}
    public Color4 To{get;set;}
    private readonly Stopwatch _stopwatch=Stopwatch.StartNew();
    private Color4 CurrentColor
    {
        get
        {
            var millis=_stopwatch.ElapsedMilliseconds;
            float factor=millis*2/(float) RotationMillis;
            var col= Vector4<float>.CosineLerp(From.ToVec4(), To.ToVec4(),factor);
            
            return col.ToColor4();
        }
    }

    public void Dispose() { }

    public bool GLInitialized => true;
    public void Init() { }
    public void Update(float _) => GL.ClearColor(CurrentColor);
    public void Render()=>GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);

}