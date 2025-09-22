using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public sealed class BBoxView : IOpenGLControl
{
    public bool Show { get; set; } = true;
    public required ICamera Camera { get; set; }
    public Color4  Color { get; set; }=Color4.White;

    public float LineWidth
    {
        get;
        set => field = float.Max(value, 1f);
    } = 1;
    private VertexArrayObject? _vao;
    private BufferObject? _vbo;
    private BufferObject? _ibo;
    private ShaderProgram? _shaderProgram;
    private static readonly int[] Indices=[
        0,1,1,2,2,3,3,0, //floor
        0,4,1,5,2,6,3,7, //bars
        4,5,5,6,6,7,7,4  //roof
    ];
    
    
    public AABB<Vector3<float>> BBox
    {
        get;
        set
        {
            _newBbox = field != value;
            field = value;
        }
    }

    private bool _newBbox = true;


    public bool GLInitialized { get; private set; }
    public void Init()
    {
        GLInitialized = true;
        _shaderProgram=ShaderProgram.FromFiles("Shaders/solid_color/solid_color");
        InitStorage();
    }

    private void InitStorage()
    {
        _vao=new VertexArrayObject();
        _vao.Bind();
        _ibo = new BufferObject(BufferTarget.ElementArrayBuffer);
        _ibo.BindAnd().BufferData(Indices,BufferUsageHint.StaticDraw);
        
        OpenGLHelper.LogGlError(nameof(BBoxView));
    }

    public void Update(float aspectRatio)
    {
        if (_newBbox||_vbo is null) UploadBox();
        UpdateShader(aspectRatio);
    }

    private void UpdateShader(float aspectRatio)
    {
        var (model, view, projection) = Camera.CreateRenderMatrices(aspectRatio);
        var objectColor = Color;
        const float depthOffset = 0.000001f;
        _shaderProgram!.BindAnd()
            .SetUniform(nameof(model),ref model)
            .SetUniform(nameof(view),ref view)
            .SetUniform(nameof(projection),ref projection)
            .SetUniform(nameof(objectColor),objectColor)
            .SetUniform(nameof(depthOffset),depthOffset)
            .Unbind();
        OpenGLHelper.LogGlError(nameof(BBoxView));
    }

    private void UploadBox()
    {
        _vao!.Bind();
        _newBbox = false;
        _vbo?.Dispose();
        _vbo = new BufferObject(BufferTarget.ArrayBuffer);
        var min = BBox.Min;
        var max=BBox.Max;

        Vector3<float>[] verts = [
            min,
            new(max.X, min.Y, min.Z),
            new(max.X, max.Y, min.Z),
            new(min.X, max.Y, min.Z),
            new(min.X, min.Y, max.Z),
            new(max.X, min.Y, max.Z),
            max,
            new(min.X, max.Y, max.Z)
        ];
        _vbo.BindAnd().BufferData(verts,BufferUsageHint.StaticDraw);
        var posLoc = _shaderProgram!.GetAttribLoc("position");
        GL.VertexAttribPointer(posLoc,3,VertexAttribPointerType.Float,false,Vector3<float>.ByteSize,0);
        GL.EnableVertexAttribArray(posLoc);
        OpenGLHelper.LogGlError(nameof(BBoxView));
    }

    public void Render()
    {
        if(!Show) return;

        _vao!.Bind();
        _ibo!.Bind();
        _shaderProgram!.Bind();
        GL.LineWidth(LineWidth);
        GL.DrawElements(PrimitiveType.Lines,Indices.Length,DrawElementsType.UnsignedInt,0);
        GL.LineWidth(1f);
        _shaderProgram!.Unbind();
        _vao!.Unbind();

        OpenGLHelper.LogGlError(nameof(BBoxView));
    }
    
    
    public void Dispose()
    {
        _shaderProgram?.Unbind();
        _shaderProgram?.Dispose();
        _vao?.Unbind();
        _ibo?.Unbind();
        _ibo?.Dispose();
        _vbo?.Dispose();
        _vao?.Dispose();
    }
}