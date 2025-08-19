using System.Drawing;
using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class IndexedLineView : IOpenGLControl
{
    public ICamera Camera{get;set;}
    public bool Show { get; set; } 
    public float LineWidth
    {
        get;
        set => field = float.Max(value, 1f);
    }

    public Color4 Color{get;set;}

    public Color SysColor
    {
        get => System.Drawing.Color.FromArgb(Color.ToArgb());
        set=> Color = new Color4(value.R,value.G,value.B,value.A);
    }

    public int Argb
    {
        get=>SysColor.ToArgb();
        set=>SysColor=System.Drawing.Color.FromArgb(value);
    }

    private VertexArrayObject? _vao;
    private BufferObject? _vbo;
    private BufferObject? _ibo;
    private ShaderProgram?  _shader; 
    public bool GLInitialized { get; private set; }
    private bool _newLine;
    private int _uploadedVertexCount;
    public IEnumerable<Line<Vector3<float>, float>> Lines
    {
        get;
        set
        {
            field=value;
            _newLine=true;
        }
    }

    

    public IndexedLineView() : this([]) { }

    public IndexedLineView(IEnumerable<Line<Vector3<float>, float>> lines)
    {
        Show= true;
        Lines = lines;
        Camera=OrbitCamera.Default();
        Camera.MoveForwards(-10);
        Color = Color4.White;
        LineWidth = 2;
    }


    public void Init()
    {
        _vao = new VertexArrayObject();
        _shader=ShaderProgram.FromFiles("./Shaders/solid_color/solid_color");
        OpenGLHelper.LogGlError(nameof(LineView),nameof(Init));
        GLInitialized = true;
    }
    public void Update(float aspect)
    {
        if (_newLine) UploadLine();
        UpdateShader(aspect);
    }

    private void UpdateShader(float aspect)
    {
        var (model, view, projection) = Camera.CreateRenderMatrices(aspect);
        var objectColor = Color;
        const float depthOffset = 0.000001f;
        _shader!.BindAnd()
            .SetUniform(nameof(model), ref model)
            .SetUniform(nameof(view), ref view)
            .SetUniform(nameof(projection), ref projection)
            .SetUniform(nameof(objectColor),in objectColor)
            .SetUniform(nameof(depthOffset),depthOffset)
            .Unbind();
        OpenGLHelper.LogGlError(nameof(LineView),nameof(UpdateShader));
    }


    private void UploadLine()
    {
        _newLine = false;
        _vao!.Bind();
        _vbo?.Unbind();
        _vbo?.Dispose();
        _ibo?.Unbind();
        _ibo?.Dispose();
        var (indices,vertices) = Polyline.Indexing.Indicate(Lines);
        _vbo=new BufferObject(BufferTarget.ArrayBuffer);
        _vbo.BindAnd().BufferData(vertices,BufferUsageHint.StaticDraw);
        _ibo=new BufferObject(BufferTarget.ElementArrayBuffer);
        _ibo.BindAnd().BufferData(indices,BufferUsageHint.StaticDraw);
        _uploadedVertexCount=indices.Length*2;
        _shader!.Bind();
        int position;
        position =_shader!.GetAttribLoc(nameof(position));
        GL.VertexAttribPointer(position, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
        GL.EnableVertexAttribArray(position);
        _vbo.Unbind();
        _vao!.Unbind();
        OpenGLHelper.LogGlError(nameof(LineView),nameof(UploadLine));
    }

   
    public void Render()
    {
        if (!Show) return;
        _shader!.Bind();
        _vao!.Bind();
        _ibo!.Bind();
         var customLineWidth = System.Math.Abs(LineWidth - 1) > 0.0001;
        if (customLineWidth) GL.LineWidth(LineWidth);
        GL.DrawElements(BeginMode.Lines, _uploadedVertexCount, DrawElementsType.UnsignedInt,0);
        if (customLineWidth) GL.LineWidth(1); // Clean state
        _vao!.Unbind();
        _shader!.Unbind();
        OpenGLHelper.LogGlError(nameof(LineView), nameof(Render));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _vbo?.Unbind();
        _vbo?.Dispose();
        _ibo?.Unbind();
        _ibo?.Dispose();
        _vao?.Unbind();
        _vao?.Dispose();
        _shader?.Unbind();
        _shader?.Dispose();
    }
}