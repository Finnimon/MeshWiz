using MeshWiz.Math;
using MeshWiz.UpToDate;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class HarshMeshView : IOpenGLControl
{
    public bool Show { get; set; } = true;
    public required ICamera Camera { get; set; }
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

    // private BufferObject? _ibo;
    // private BufferObject? _normalsSsbo;
    // private BufferObject? _normIdxVbo;
    private ShaderProgram? _solidColorShader;
    private ShaderProgram? _blinnPhongShader;


    public IMesh<float> Mesh
    {
        get;
        set
        {
            if (field == value) return;
            OutOfDate();
            _newMesh = true;
            field = value;
        }
    } = IndexedMesh<float>.Empty;


    public void Init()
    {
        OutOfDate();

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
        _solidColorShader!.ConsumeOutOfDate();
        _blinnPhongShader!.ConsumeOutOfDate();
        _solidColorShader!.BindAnd()
            .SetUniform(nameof(model), ref model)
            .SetUniform(nameof(view), ref view)
            .SetUniform(nameof(projection), ref projection)
            .SetUniform(colorUniformName, WireFrameColor)
            .Unbind();

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
            .Unbind();
        if (!_blinnPhongShader!.ConsumeOutOfDate() || !_solidColorShader!.ConsumeOutOfDate())
            this.OutOfDate();
        OpenGLHelper.LogGlError(nameof(HarshMeshView));
    }

    public void Render()
    {
        if (!GLInitialized || Mesh.Count == 0 || !Show)
            return;
        ConsumeOutOfDate();
        _vao!.Bind();
        var renderSolid = (RenderModeFlags & RenderMode.Solid) == RenderMode.Solid;
        if (renderSolid)
        {
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
        }

        if ((RenderModeFlags & RenderMode.Wireframe) == RenderMode.Wireframe)
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            _solidColorShader!.Bind();
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-0.5f, -0.5f);
            GL.LineWidth(LineWidth);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _uploadedCount);
            GL.LineWidth(1f);
            GL.PolygonOffset(0f, 0f);
            GL.Disable(EnableCap.PolygonOffsetLine);
            _solidColorShader.Unbind();
        }

        if (renderSolid)
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
            _blinnPhongShader!.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, _uploadedCount);
            _blinnPhongShader.Unbind();
        }

        if (renderSolid) GL.Disable(EnableCap.CullFace);
        _vao.Unbind();
        OpenGLHelper.LogGlError(nameof(HarshMeshView));
    }

    private readonly FloatingPointScopedProperty<float> _lineWidth;

    public float LineWidth
    {
        get => _lineWidth;
        set => _lineWidth.Value = float.Max(value, 1f);
    }


    public void Dispose()
    {
        GLInitialized = false;
        OutOfDate();
        _vao?.Unbind();
        _vbo?.Unbind();
        _vbo?.Dispose();
        // _ibo?.Unbind();
        // _ibo?.Dispose();
        // _normalsSsbo?.Unbind();
        // _normalsSsbo?.Dispose();
        // _normIdxVbo?.Unbind();
        // _normIdxVbo?.Dispose();
        _vao?.Dispose();
        _blinnPhongShader?.Unbind();
        _blinnPhongShader?.Dispose();
        _solidColorShader?.Unbind();
        _solidColorShader?.Dispose();
    }


    private void UploadMesh()
    {
        _vao!.Bind();
        _vbo?.Unbind();
        _vbo?.Dispose();
        // _ibo?.Unbind();
        // _ibo?.Dispose();
        // _normalsSsbo?.Unbind();
        // _normalsSsbo?.Dispose();
        // _normIdxVbo?.Unbind();
        // _normIdxVbo?.Dispose();

        _newMesh = false;
        this.OutOfDate();
        var mesh = Mesh;
        var interleaved = new Vector3<float>[Mesh.Count * 6];
        for (var i = 0; i < mesh.Count; i++)
        {
            var tri = mesh[i];
            var n = tri.Normal;
            interleaved[i * 6 + 0] = tri.A;
            interleaved[i * 6 + 1] = n;
            interleaved[i * 6 + 2] = tri.B;
            interleaved[i * 6 + 3] = n;
            interleaved[i * 6 + 4] = tri.C;
            interleaved[i * 6 + 5] = n;
        }

        _vbo = new BufferObject(BufferTarget.ArrayBuffer);
        _vbo.BindAnd().BufferData(interleaved, BufferUsageHint.StaticDraw);
        var positionLoc = _blinnPhongShader!.GetAttribLoc("position");
        GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, Vector3<float>.ByteSize * 2, 0);
        GL.EnableVertexAttribArray(positionLoc);
        var normLoc = _blinnPhongShader!.GetAttribLoc("normal");
        GL.VertexAttribPointer(normLoc, 3, VertexAttribPointerType.Float, false, Vector3<float>.ByteSize * 2,
            Vector3<float>.ByteSize);
        GL.EnableVertexAttribArray(normLoc);
        _uploadedCount = Mesh.Count * 3;


        _vao.Unbind();

        // var normals = new Vector3<float>[mesh.Count * 3];
        // for (var i = 0; i < mesh.Count; i++) normals[i] = mesh[i].Normal;
        // var normalIndices = new uint[mesh.Count * 3];
        // for (uint i = 0; i < mesh.Count; i++)
        //     for (uint j = 0; j < 3; j++)
        //         normalIndices[i+j] = i;
        // _normIdxVbo = new BufferObject(BufferTarget.ArrayBuffer);
        // _normIdxVbo.Bind();
        // _normIdxVbo.BufferData(normalIndices, BufferUsageHint.StaticDraw);
        // int normIdxLoc = _blinnPhongShader!.GetAttribLoc("normalIndex"); // =1
        // GL.VertexAttribIPointer(normIdxLoc, 1, VertexAttribIntegerType.UnsignedInt, sizeof(uint), 0);
        // GL.VertexAttribDivisor(normIdxLoc, 0);
        // GL.EnableVertexAttribArray(normIdxLoc);
        //
        // _uploadedCount = mesh.Count * 3;
        //
        // _vbo = new BufferObject(BufferTarget.ArrayBuffer);
        // _vbo.BindAnd()
        //     .BufferData(mesh.Vertices, BufferUsageHint.StaticDraw);
        //
        // int positionLoc = _blinnPhongShader!.GetAttribLoc("position");
        //
        // GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, Vector3<float>.ByteSize, 0);
        // GL.EnableVertexAttribArray(positionLoc);


        // _ibo = new BufferObject(BufferTarget.ElementArrayBuffer);
        // _ibo.BindAnd()
        //     .BufferData(mesh.Indices, BufferUsageHint.StaticDraw);
        // var errorCode = OpenGLHelper.LogGlError(nameof(HarshMeshView), nameof(UploadMesh));
        //
        // _normalsSsbo = new BufferObject(BufferTarget.ShaderStorageBuffer);
        // _normalsSsbo.BindAnd().BufferData(normals, BufferUsageHint.StaticDraw);
        // var ssboBindingIndex = _blinnPhongShader.BindAnd().GetResourceIndex("TriangleNormals");
        // GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _normalsSsbo.Handle);
        // _vao.Unbind();
    }

    private bool _upToDate = false;

    public HarshMeshView()
    {
        _renderModeFlags = this.Property((int)RenderMode.Solid);
        _wireframeColor = this.Property(Color4.Black);
        _solidColor = this.Property(Color4.Gray);
        _lightColor = this.Property(Color4.White);
        _lightPosition = this.Property(Vector3<float>.NaN);
        _solidAmbientStrength = this.FloatingPointProperty(0.15f);
        _solidSpecularStrength = this.FloatingPointProperty(0.75f);
        _solidShininessStrength = this.FloatingPointProperty(32f);
        _lineWidth = this.FloatingPointProperty(1f);
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