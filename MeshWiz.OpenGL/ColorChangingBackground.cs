using System.Diagnostics;
using MeshWiz.Math;
using MeshWiz.UpToDate;
using OpenTK.Mathematics;

namespace MeshWiz.OpenTK;

public class ColorChangingBackground : IOpenGLControl
{
    public bool Show { get; set; } = true;
    public int RotationMillis { get; set; } = 10000;
    public Color4 From { get; set; }
    public Color4 To { get; set; }
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly EquatableScopedProperty<Color4> _currentColor;
    public ColorChangingBackground() => _currentColor = this.Property<Color4>();

    public Color4 CurrentColor
    {
        get => _currentColor.Value;
        private set => _currentColor.Value = value;
    }

    private Color4 CalculateNextColor()
    {
        var millis = _stopwatch.ElapsedMilliseconds;
        var factor = millis * 2 / (float)RotationMillis;
        var col = Vec4<float>.CosineLerp(From.ToVec4(), To.ToVec4(), factor);
        return col.ToColor4();
    }

    public void Dispose() { }

    public bool GLInitialized => true;
    public void Init() { }

    public void Update(float _) => _currentColor.Value = CalculateNextColor();

    public void Render()
    {
        if (!Show) return;
        ConsumeOutOfDate();
        GL.ClearColor(CurrentColor);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    private bool _upToDate=false;
    /// <inheritdoc />
    public void OutOfDate() => _upToDate = true;

    /// <inheritdoc />
    public bool ConsumeOutOfDate()
    {
        var copy = _upToDate;
        _upToDate = true;
        return copy;
    }
}