global using OpenTK.Graphics.OpenGL;
using System.Collections.Specialized;
using System.Numerics;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using MeshWiz.Math;

namespace MeshWiz.UI.Avalonia;

public sealed class OpenGLParent : OpenGlControlBase
{
    [Content] public AvaloniaList<IOpenGLControl> Children { get; }
    private double RenderScale => _renderScale ??= (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    private double? _renderScale;
    private AvaloniaGLContext? _context;


    public OpenGLParent()
    {
        Children = [];
    }


    protected override void OnOpenGlInit(GlInterface gl)
    {
        _context = new AvaloniaGLContext(gl);
        GL.LoadBindings(_context);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        var visible = UpdateViewport();
        if (visible) RenderChildren();
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Default);
    }

    private bool UpdateViewport()
    {
        var trueBound = new Vector2<double>(Bounds.Width, Bounds.Height) * RenderScale;
        GL.Viewport(0, 0, (int)trueBound.X, (int)trueBound.Y);
        return trueBound.Length >= 1;
    }

    private void RenderChildren()
    {
        foreach (var child in Children)
        {
            if (!child.IsInitialized)
            {
                child.Init();
                child.IsInitialized = true;
            }

            child.Update();
            child.Render();
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        Children.Where(child => child.IsInitialized)
            .ToList()
            .ForEach(child => child.Dispose());
    }
}