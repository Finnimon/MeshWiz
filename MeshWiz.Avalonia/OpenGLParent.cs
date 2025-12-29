using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using MeshWiz.Math;
using MeshWiz.OpenTK;
using MeshWiz.Utility.Extensions;
using OpenTK.Graphics.OpenGL;

namespace MeshWiz.Avalonia;

public sealed class OpenGLParent : OpenGlControlBase, IDisposable
{
    [Content] public AvaloniaList<IOpenGLControl> Children { get; }
    private double RenderScale => _renderScale ??= (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    private double? _renderScale;
    private AvaloniaGLContext? _context;
    private bool _disposed = false;
    public float Fps { get; private set; }

    public OpenGLParent()
    {
        Children = [];
        Children.CollectionChanged += ChildrenChangeHandler;
    }

    private void ChildrenChangeHandler(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Move)
            return;
        e.OldItems?.OfType<IDisposable>().ForEach(dispo => dispo.Dispose());
    }


    protected override void OnOpenGlInit(GlInterface gl)
    {
        _context = new AvaloniaGLContext(gl);
        GL.LoadBindings(_context);
        // Console.WriteLine("GL Version: " + GL.GetString(StringName.Version));
        // Console.WriteLine("GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
        // Console.WriteLine("GL Renderer: " + GL.GetString(StringName.Renderer));
        // Console.WriteLine("GL Vendor: " + GL.GetString(StringName.Vendor));
    }

    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private const int TickerMax = 30;
    private int _ticker = TickerMax;
    private long _millis = 0;

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        UpdateFps();
        GL.LoadBindings(_context);
        var (visible, aspectRatio) = UpdateViewport();
        if (visible) RenderChildren(aspectRatio);
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Default);
    }

    private void UpdateFps()
    {
        _millis += _sw.ElapsedMilliseconds;
        _sw.Restart();
        _ticker--;

        if (_ticker >= 0) return;
        Fps = ((float)TickerMax) / _millis * 1000;
        _millis = 0;
        _ticker = TickerMax;
    }

    private (bool visible, float aspectratio) UpdateViewport()
    {
        var trueBound = new Vec2<double>(Bounds.Width, Bounds.Height) * RenderScale;
        GL.Viewport(0, 0, (int)trueBound.X, (int)trueBound.Y);
        return (trueBound.Length >= 1, (float)(trueBound.X / trueBound.Y));
    }

    /// <summary>
    /// meant for ensuring framebuffer is filled
    /// </summary>
    private int _renderSinceUpToDate = 0;
    private void RenderChildren(float aspectRatio)
    {
        GL.LoadBindings(_context!);
        var upToDate = true;
        foreach (var child in Children)
        {
            if (!child.GLInitialized) child.Init();
            child.Update(aspectRatio);
            var isChild= child.ConsumeOutOfDate();
            if(isChild)
                continue;
            upToDate = false;
        }
        
        if (upToDate&&_renderSinceUpToDate>1)
            return;
        _renderSinceUpToDate = upToDate ? _renderSinceUpToDate + 1 : 0;
        
        GL.Enable(EnableCap.DepthTest);
        Children.ForEach(child => child.Render());
        GL.Disable(EnableCap.DepthTest);
    }


    protected override void OnOpenGlDeinit(GlInterface gl) => Dispose();

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) => Dispose();

    public void Dispose()
    {
        foreach (var openGLControl in Children) openGLControl.Dispose();
        if (_disposed) return;
        _disposed = true;
        _context?.AvaloniaInterface.Finish();
    }
}