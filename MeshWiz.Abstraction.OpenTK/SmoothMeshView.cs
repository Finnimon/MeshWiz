using MeshWiz.Math;
using MeshWiz.UpToDate;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class SmoothMeshView : IOpenGLControl
{
    private readonly EquatableScopedProperty<bool> _show;

    public bool Show
    {
        get => _show;
        set => _show.Value = value;
    }

    public required ICamera Camera { get; set; }

    // public RenderMode RenderModeFlags { get; set; } = RenderMode.Solid;
    // public Color4 WireFrameColor { get; set; } = Color4.Black;
    // public Color4 SolidColor { get; set; } = Color4.Gray;
    // public Color4 LightColor { get; set; } = Color4.White;
    //
    private readonly FloatingPointScopedProperty<float> _lineWidth;

    public float LineWidth
    {
        get => _lineWidth;
        set => _lineWidth.Value = float.Max(value, 1f);
    }

    private readonly EquatableScopedProperty<int> _renderModeFlags;

    public RenderMode RenderModeFlags
    {
        get => (RenderMode)_renderModeFlags.Value;
        set => _renderModeFlags.Value = (int)value;
    }

    private readonly EquatableScopedProperty<Color4> _wireframeColor;

    public Color4 WireFrameColor
    {
        get => _wireframeColor;
        set => _wireframeColor.Value = value;
    }

    private readonly EquatableScopedProperty<Color4> _solidColor;

    public Color4 SolidColor
    {
        get => _solidColor;
        set => _solidColor.Value = value;
    }

    private readonly EquatableScopedProperty<Color4> _lightColor;

    public Color4 LightColor
    {
        get => _lightColor;
        set => _lightColor.Value = value;
    }

    private readonly EquatableScopedProperty<Vector3<float>> _lightPosition;

    private Vector3<float> LightPosition
    {
        get => Vector3<float>.IsNaN(_lightPosition) ? AboveHead : _lightPosition;
        set => _lightPosition.Value = value;
    }

    private Vector3<float> AboveHead => Camera.UnitUp * 2 + Camera.Position;
    private readonly FloatingPointScopedProperty<float> _solidAmbientStrength;

    public float SolidAmbientStrength
    {
        get => _solidAmbientStrength;
        set => _solidAmbientStrength.Value = float.Clamp(value, 0f, 1f);
    }

    private readonly FloatingPointScopedProperty<float> _depthOffset;

    public float DepthOffset
    {
        get => _depthOffset;
        set => _depthOffset.Value = value;
    }

    private readonly FloatingPointScopedProperty<float> _solidSpecularStrength;

    public float SolidSpecularStrength
    {
        get => _solidSpecularStrength;
        set => _solidSpecularStrength.Value = float.Clamp(value, 0f, 1f);
    }

    private readonly FloatingPointScopedProperty<float> _solidShininessStrength;

    public float SolidShininessStrength
    {
        get => _solidShininessStrength;
        set => _solidShininessStrength.Value = value;
    }

    public bool GLInitialized { get; private set; }
    private bool _newMesh;
    private int _uploadedCount;
    private VertexArrayObject? _vao;
    private BufferObject? _vbo;
    private BufferObject? _ibo;
    private ShaderProgram? _solidColorShader;
    private ShaderProgram? _blinnPhongShader;


    public IIndexedMesh<float> Mesh
    {
        get;
        set
        {
            if (field == value) return;
            this.OutOfDate();
            _newMesh = true;
            field = value;
        }
    } = IndexedMesh<float>.Empty;


    public void Init()
    {
        _vao = new VertexArrayObject();
        _blinnPhongShader = ShaderProgram.FromFiles("Shaders/blinn_phong/blinn_phong");
        _solidColorShader = ShaderProgram.FromFiles("Shaders/solid_color/solid_color");
        GLInitialized = true;
    }

    public void Update(float aspectRatio)
    {
        if (_newMesh) UploadMesh();
        UpdateShaders(aspectRatio);
    }

    private void UpdateShaders(float aspectRatio)
    {
        var (model, view, projection) = Camera.CreateRenderMatrices(aspectRatio);
        const string colorUniformName = "objectColor";
        var camDistance = Camera.Position.DistanceTo(Camera.LookAt);
        var depthOffset = DepthOffset / (camDistance * camDistance);
        _solidColorShader!.ConsumeOutOfDate();
        _blinnPhongShader!.ConsumeOutOfDate();
        _solidColorShader!.BindAnd()
            .SetUniform(nameof(model), ref model)
            .SetUniform(nameof(view), ref view)
            .SetUniform(nameof(projection), ref projection)
            .SetUniform(nameof(depthOffset), depthOffset)
            .SetUniform(colorUniformName, WireFrameColor)
            .Unbind();
        OpenGLHelper.LogGlError(nameof(SmoothMeshView));
        
        _blinnPhongShader!.BindAnd()
            .SetUniform(nameof(model), ref model)
            .SetUniform(nameof(view), ref view)
            .SetUniform(nameof(projection), ref projection)
            .SetUniform(colorUniformName, SolidColor)
            .SetUniform("viewPos", Camera.Position.ToOpenTK())
            .SetUniform("lightColor", LightColor.ToVec4().XYZ.ToOpenTK())
            .SetUniform("ambientStrength", SolidAmbientStrength)
            .SetUniform("specularStrength", SolidSpecularStrength)
            .SetUniform("shininess", SolidShininessStrength)
            .SetUniform("lightPos", LightPosition.ToOpenTK())
            .SetUniform(nameof(depthOffset), depthOffset)
            .Unbind();
        OpenGLHelper.LogGlError(nameof(SmoothMeshView));
        if (!_blinnPhongShader.ConsumeOutOfDate() || !_solidColorShader.ConsumeOutOfDate())
            this.OutOfDate();
    }

    public void Render()
    {
        if (!GLInitialized || Mesh.Count == 0 || !Show)
            return;
        ConsumeOutOfDate();
        _vao!.Bind();

        if ((RenderModeFlags & RenderMode.Solid) == RenderMode.Solid)
        {
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
            _blinnPhongShader!.Bind();
            GL.DrawElements(PrimitiveType.Triangles, _uploadedCount, DrawElementsType.UnsignedInt, 0);
            _blinnPhongShader.Unbind();
            GL.Disable(EnableCap.CullFace);
        }

        if ((RenderModeFlags & RenderMode.Wireframe) == RenderMode.Wireframe)
        {
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            _solidColorShader!.Bind();
            GL.PolygonOffset(-1f, -1f);
            GL.LineWidth(LineWidth);
            GL.DrawElements(PrimitiveType.Triangles, _uploadedCount, DrawElementsType.UnsignedInt, 0);
            GL.LineWidth(1f);
            GL.Disable(EnableCap.PolygonOffsetLine);
            _solidColorShader.Unbind();
        }


        _vao.Unbind();
        OpenGLHelper.LogGlError(nameof(SmoothMeshView));
    }


    public void Dispose()
    {
        OutOfDate();
        GLInitialized = false;
        _vao?.Unbind();
        _vbo?.Unbind();
        _vbo?.Dispose();
        _ibo?.Unbind();
        _ibo?.Dispose();
        _vao?.Dispose();
        _blinnPhongShader?.Unbind();
        _blinnPhongShader?.Dispose();
        _solidColorShader?.Unbind();
        _solidColorShader?.Dispose();
        _vao = null;
        _vbo = null;
        _ibo = null;
        _blinnPhongShader = null;
        _solidColorShader = null;
    }


    private void UploadMesh()
    {
        OutOfDate();
        _vao!.Bind();
        _vbo?.Unbind();
        _vbo?.Dispose();
        _ibo?.Unbind();
        _ibo?.Dispose();

        var mesh = Mesh;
        var normals = GetInterleavedMesh(mesh);
        _newMesh = false;
        // Interleave positions and normals
        var vertexData = new float[mesh.Vertices.Length * 6];
        for (var i = 0; i < mesh.Vertices.Length; i++)
        {
            var v = mesh.Vertices[i];
            var n = normals[i];
            var baseIdx = i * 6;
            vertexData[baseIdx + 0] = v.X;
            vertexData[baseIdx + 1] = v.Y;
            vertexData[baseIdx + 2] = v.Z;
            vertexData[baseIdx + 3] = n.X;
            vertexData[baseIdx + 4] = n.Y;
            vertexData[baseIdx + 5] = n.Z;
        }


        _uploadedCount = mesh.Indices.Length * 3;

        _vbo = new BufferObject(BufferTarget.ArrayBuffer);
        _vbo.BindAnd()
            .BufferData(vertexData, BufferUsageHint.StaticDraw);

        var stride = 2 * Vector3<float>.ByteSize;
        var positionLoc = _blinnPhongShader!.GetAttribLoc("position");
        var normalLoc = _blinnPhongShader.GetAttribLoc("normal");

        GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(positionLoc);

        GL.VertexAttribPointer(normalLoc, 3, VertexAttribPointerType.Float, false, stride, sizeof(float) * 3);
        GL.EnableVertexAttribArray(normalLoc);

        _ibo = new BufferObject(BufferTarget.ElementArrayBuffer);
        _ibo.BindAnd()
            .BufferData(mesh.Indices, BufferUsageHint.StaticDraw);

        _vao.Unbind();
        OpenGLHelper.LogGlError(nameof(SmoothMeshView));
    }

    Vector3<float>[] GetInterleavedMesh(IIndexedMesh<float> mesh)
    {
        var normals = new Vector3<float>[mesh.Vertices.Length];
        var counts = new uint[mesh.Vertices.Length];

        for (var i = 0; i < mesh.Indices.Length; i++)
        {
            var indexer = mesh.Indices[i];
            var normal = indexer.Extract(mesh.Vertices).Normal;

            normals[indexer.A] += normal;
            counts[indexer.A]++;
            normals[indexer.B] += normal;
            counts[indexer.B]++;
            normals[indexer.C] += normal;
            counts[indexer.C]++;
        }

        for (var i = 0; i < normals.Length; i++) normals[i] /= counts[i];

        return normals;
    }

    private bool _upToDate = false;

    public SmoothMeshView()
    {
        _show = this.Property(true);
        _lineWidth = this.FloatingPointProperty(1f);
        _renderModeFlags = this.Property((int)RenderMode.Solid);
        _wireframeColor = this.Property(Color4.Black);
        _solidColor = this.Property(Color4.Gray);
        _lightColor = this.Property(Color4.White);
        _lightPosition = this.Property(Vector3<float>.NaN);
        _solidAmbientStrength = this.FloatingPointProperty(0.15f);
        _depthOffset = this.FloatingPointProperty(0.0005f);
        _solidSpecularStrength = this.FloatingPointProperty(0.75f);
        _solidShininessStrength = this.FloatingPointProperty(32f);
    }

    /// <inheritdoc />
    public void OutOfDate()
        => _upToDate = false;

    /// <inheritdoc />
    public bool ConsumeOutOfDate()
    {
        var copy = _upToDate;
        _upToDate = true;
        return copy;
    }
}