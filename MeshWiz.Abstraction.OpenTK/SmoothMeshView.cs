using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class SmoothMeshView : IOpenGLControl
{
    public bool Show { get; set; } = true;
    public required ICamera Camera { get; set; }
    public RenderMode RenderModeFlags { get; set; } = RenderMode.Solid;
    public Color4 WireFrameColor { get; set; } = Color4.Black;
    public Color4 SolidColor { get; set; } = Color4.Gray;
    public Color4 LightColor { get; set; } = Color4.White;
    
    public float LineWidth
    {
        get;
        set => field = float.Max(value, 1f);
    } = 1f;
    private Vector3<float> LightPosition
    {
        get => Vector3<float>.IsNaN(field) ?  AboveHead:field;
        set;
    } = Vector3<float>.NaN;

    private Vector3<float> AboveHead => Camera.UnitUp * 2 + Camera.Position;

    public float SolidAmbientStrength
    {
        get;
        set => field = float.Clamp(value, 0f, 1f);
    } = 0.15f;

    public float SolidSpecularStrength
    {
        get;
        set => field = float.Clamp(value, 0f, 1f);
    } = 0.75f;

    public float SolidShininessStrength { get; set; } = 32;
    public bool GLInitialized { get; private set; }
    private bool _newMesh;
    private int _uploadedCount;
    private VertexArrayObject? _vao;
    private BufferObject? _vbo;
    private BufferObject? _ibo;
    private ShaderProgram? _solidColorShader;
    private ShaderProgram? _blinnPhongShader;


    public IndexedMesh3<float> Mesh
    {
        get;
        set
        {
            if (field == value) return;
            _newMesh = true;
            field = value;
        }
    } = IndexedMesh3<float>.Empty;


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
        if ((RenderModeFlags & RenderMode.Wireframe) == RenderMode.Wireframe)
            _solidColorShader!.BindAnd()
                .SetUniform(nameof(model), ref model)
                .SetUniform(nameof(view), ref view)
                .SetUniform(nameof(projection), ref projection)
                .SetUniform(colorUniformName, WireFrameColor)
                .Unbind();

        if ((RenderModeFlags & RenderMode.Solid) == RenderMode.Solid)
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
        OpenGLHelper.LogGlError(nameof(SmoothMeshView), nameof(UpdateShaders));
    }

    public void Render()
    {
        if (!GLInitialized || Mesh.Count == 0||!Show)
            return;

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
        OpenGLHelper.LogGlError(nameof(SmoothMeshView), nameof(Render));
    }


    public void Dispose()
    {
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
    }


    private void UploadMesh()
    {
        _vao!.Bind();
        _vbo?.Unbind(); _vbo?.Dispose();
        _ibo?.Unbind(); _ibo?.Dispose();

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
    
        
        _uploadedCount = mesh.Indices.Length*3;
    
        _vbo = new BufferObject(BufferTarget.ArrayBuffer);
        _vbo.BindAnd()
            .BufferData(vertexData, BufferUsageHint.StaticDraw);

        var stride = 2*Vector3<float>.ByteSize;
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
        OpenGLHelper.LogGlError(nameof(SmoothMeshView), nameof(UploadMesh));
    }

    Vector3<float>[] GetInterleavedMesh(IndexedMesh3<float> mesh)
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

        for (var i = 0; i < normals.Length; i++) normals[i]/=counts[i];

        return normals;
    }

}