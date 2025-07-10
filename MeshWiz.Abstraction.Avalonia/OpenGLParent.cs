using System.Diagnostics;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using MeshWiz.Abstraction.OpenTK;
using MeshWiz.Math;
using OpenTK.Graphics.OpenGL;

namespace MeshWiz.Abstraction.Avalonia;

public sealed class OpenGLParent : OpenGlControlBase, IDisposable
{
    [Content] public AvaloniaList<IOpenGLControl> Children { get; }
    private double RenderScale => _renderScale ??= (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    private double? _renderScale;
    private AvaloniaGLContext? _context;
    public float Fps { get; private set; }

    public OpenGLParent()
    {
        Children = [];
    }


    protected override void OnOpenGlInit(GlInterface gl)
    {
        _context = new AvaloniaGLContext(gl);
        GL.LoadBindings(_context);
    }
    private readonly Stopwatch _sw=Stopwatch.StartNew();
    private const int TickerMax = 30;
    private int _ticker = TickerMax;
    private long _millis = 0;
    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        UpdateFps();
        var (visible, aspectRatio) = UpdateViewport();
        if (visible) RenderChildren(aspectRatio);
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Default);
    }

    private void UpdateFps()
    {
        _millis+= _sw.ElapsedMilliseconds;
        _sw.Restart();
        
        if(_ticker==0)
        {
            Fps = ((float)TickerMax) / _millis * 1000;
            _millis = 0;
            _ticker = TickerMax;
        }
        _ticker--;
    }

    private (bool visible, float aspectratio) UpdateViewport()
    {
        var trueBound = new Vector2<double>(Bounds.Width, Bounds.Height) * RenderScale;
        GL.Viewport(0, 0, (int)trueBound.X, (int)trueBound.Y);
        return (trueBound.Length >= 1,(float)(trueBound.X / trueBound.Y));
    }

    private void RenderChildren(float aspectRatio)
    {
        int childIdx = 0;
        foreach (var child in Children)
        {
            if (!child.GLInitialized)
            {
                child.Init();
            }
            child.Update(aspectRatio);
            child.Render();
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        Dispose();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        foreach (var openGLControl in Children) openGLControl.Dispose();
    }
}