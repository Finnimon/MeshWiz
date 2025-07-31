using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class HarshMeshView : IOpenGLControl
{
    public bool Show { get; set; } = true;
    public required ICamera Camera { get; set; }
    public RenderMode RenderModeFlags { get; set; } = RenderMode.Solid;
    public Color4 WireFrameColor { get; set; } = Color4.Black;
    public Color4 SolidColor { get; set; } = Color4.Gray;
    public Color4 LightColor { get; set; } = Color4.White;

    private Vector3<float> LightPosition
    {
        get => Vector3<float>.IsNaN(field) ? AboveHead : field;
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
    // private BufferObject? _ibo;
    // private BufferObject? _normalsSsbo;
    // private BufferObject? _normIdxVbo;
    private ShaderProgram? _solidColorShader;
    private ShaderProgram? _blinnPhongShader;


    public IMesh3<float> Mesh
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
        OpenGLHelper.LogGlError(nameof(HarshMeshView), nameof(UpdateShaders));
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
            GL.DrawArrays(PrimitiveType.Triangles, 0, _uploadedCount);
            _blinnPhongShader.Unbind();
            GL.Disable(EnableCap.CullFace);
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


        _vao.Unbind();
        OpenGLHelper.LogGlError(nameof(HarshMeshView), nameof(Render));
    }

    public float LineWidth
    {
        get;
        set => field = float.Max(value, 1f);
    } = 1f;


    public void Dispose()
    {
        GLInitialized = false;
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
        var mesh = Mesh;
        var interleaved=new Vector3<float>[Mesh.Count*6];
        for (var i = 0; i < mesh.Count; i++)
        {
            var tri = mesh[i];
            var n = tri.Normal;
            interleaved[i*6 + 0] = tri.A;
            interleaved[i*6 + 1] = n;
            interleaved[i*6 + 2] = tri.B;
            interleaved[i*6 + 3] = n;
            interleaved[i*6 + 4] = tri.C;
            interleaved[i*6 + 5] = n;
        }
        
        _vbo = new BufferObject(BufferTarget.ArrayBuffer);
        _vbo.BindAnd().BufferData(interleaved, BufferUsageHint.StaticDraw);
        var positionLoc = _blinnPhongShader!.GetAttribLoc("position");
        GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, Vector3<float>.ByteSize*2, 0);
        GL.EnableVertexAttribArray(positionLoc);
        var normLoc= _blinnPhongShader!.GetAttribLoc("normal");
        GL.VertexAttribPointer(normLoc, 3, VertexAttribPointerType.Float, false, Vector3<float>.ByteSize*2, Vector3<float>.ByteSize);
        GL.EnableVertexAttribArray(normLoc);
        _uploadedCount=Mesh.Count*3;
        
        
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
}