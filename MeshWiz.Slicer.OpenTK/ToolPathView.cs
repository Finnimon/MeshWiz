using System.Runtime.CompilerServices;
using MeshWiz.OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MeshWiz.Slicer.OpenTK;

public sealed class ToolPathView 
{
    /// <inheritdoc />
    public bool Show { get; set; }

    public required ICamera Camera { get; set; }

    public float LineWidth
    {
        get;
        set => field = float.Max(value, 1f);
    }

    /// <inheritdoc />
    public bool GLInitialized { get; private set; }

    private bool _newGeometry;
    private VertexArrayObject? _vao;
    private BufferObject? _vbo;
    private ShaderProgram? _shader;

    public Color4 PerimeterColor { get; set; } = Color4.White;
    public Color4 InfillColor { get; set; } = Color4.White;
    public Range LayersToShow { get; set; } = Range.All;

    public SlicedLayer<float>[] Layers
    {
        get;
        set
        {
            field = value;
            _newGeometry = true;
        }
    } = [];


    /// <inheritdoc />
    public void Init()
    {
        _vao = new VertexArrayObject();
        _shader = ShaderProgram.FromFiles("./Shaders/Slicer/tcp");
        OpenGLHelper.LogGlError(nameof(ToolPathView));
        GLInitialized = true;
    }

    /// <inheritdoc />
    public void Update(float aspectRatio)
    {
        if (_newGeometry) UploadLayers();
        UpdateShader(aspectRatio);
    }

    private void UpdateShader(float aspect)
    {
        var (model, view, projection) = Camera.CreateRenderMatrices(aspect);
        const float depthOffset = 0.000001f;
        _shader!.BindAnd()
            .SetUniform(nameof(model), ref model)
            .SetUniform(nameof(view), ref view)
            .SetUniform(nameof(projection), ref projection)
            .SetUniform("infillColor", InfillColor)
            .SetUniform("perimeterColor", PerimeterColor)
            .SetUniform(nameof(depthOffset), depthOffset)
            .Unbind();
        OpenGLHelper.LogGlError(nameof(LineView));
    }

    /// <inheritdoc />
    public void Render()
    {
        if (!Show) return;
        _shader!.Bind();
        _vao!.Bind();
        var customLineWidth = System.Math.Abs(LineWidth - 1) > 0.0001;
        if (customLineWidth) GL.LineWidth(LineWidth);
        DrawTargetLayers();
        if (customLineWidth) GL.LineWidth(1); // Clean state
        _vao!.Unbind();
        _shader!.Unbind();
        OpenGLHelper.LogGlError(nameof(LineView));
    }

    private void DrawTargetLayers()
    {
        List<int> first = [];
        List<int> count = [];
        //should cache and only get range of global first count arrays
        foreach (var layerIndices in _layerRanges.AsSpan(LayersToShow))
        {
            first.AddRange(layerIndices.first);
            count.AddRange(layerIndices.count);
        }

        var firstArray = first.ToArray();
        var countArray = count.ToArray();
        _vao!.Bind();
        _shader!.Bind();
        _vbo!.Bind();
        GL.MultiDrawArrays(PrimitiveType.LineStrip, firstArray, countArray, firstArray.Length);
        _shader!.Unbind();
        _vbo!.Unbind();
        _vao!.Unbind();
    }

    private (int[] first, int[] count)[] _layerRanges = [];

    private void UploadLayers()
    {
        List<ToolCenterPoint<float>> flatVertices = [];
        List<(int[] first, int[] count)> layerRanges = [];
        var offset = 0;
        foreach (var layer in Layers)
        {
            List<int> first = [];
            List<int> count = [];
            foreach (var polyline in layer.Perimeter.Concat(layer.Infill))
            {
                flatVertices.AddRange(polyline);
                first.Add(offset);
                var pCount = polyline.Length;
                count.Add(pCount);
                offset += pCount;
            }
            layerRanges.Add((first.ToArray(), count.ToArray()));
        }
        _vao!.Bind();
        _vbo?.Dispose();
        
        OpenGLHelper.ThrowOnGlError(nameof(ToolPathView));
        _vbo = new BufferObject(BufferTarget.ArrayBuffer);
        OpenGLHelper.ThrowOnGlError(nameof(ToolPathView));
        _vbo.BindAnd().BufferData(flatVertices.ToArray(), BufferUsageHint.StaticDraw);
        OpenGLHelper.ThrowOnGlError("vbo bind");
        var posLoc = _shader!
            .BindAnd()
            .GetAttribLoc("position");
        Console.WriteLine(_shader.Handle);
        var stride = Unsafe.SizeOf<ToolCenterPoint<float>>();
        Console.WriteLine($"Posloc {posLoc}");
        OpenGLHelper.ThrowOnGlError("shader attrib");
        GL.VertexAttribPointer(posLoc, 3, VertexAttribPointerType.Float, false,
            stride, 0);
        GL.EnableVertexAttribArray(posLoc);
        var normLoc=_shader.GetAttribLoc("normal");
        OpenGLHelper.ThrowOnGlError(nameof(ToolPathView));
        GL.VertexAttribPointer(normLoc, 3, VertexAttribPointerType.Float, false,
            stride, sizeof(float) * 3);
        GL.EnableVertexAttribArray(normLoc);

        var optionLoc = _shader.GetAttribLoc("tcpOptions");
        OpenGLHelper.ThrowOnGlError(nameof(ToolPathView));
        GL.VertexAttribPointer(optionLoc, 3, VertexAttribPointerType.UnsignedInt, false,
            stride, sizeof(float)*6);
        GL.EnableVertexAttribArray(optionLoc);
        OpenGLHelper.ThrowOnGlError(nameof(ToolPathView));
        _layerRanges = layerRanges.ToArray();
        _newGeometry = false;
        _vbo!.Unbind();
        _shader!.Unbind();
        _vao!.Unbind();
        OpenGLHelper.ThrowOnGlError(nameof(ToolPathView));
    }


    /// <inheritdoc />
    public void Dispose()
    {
        _vao?.Bind();
        _vbo?.Dispose();
        _shader?.Dispose();
        _vao?.Dispose();
    }
}