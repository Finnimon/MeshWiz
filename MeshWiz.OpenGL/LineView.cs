using MeshWiz.Math;
using MeshWiz.UpToDate;
using OpenTK.Mathematics;
using SysColor=System.Drawing.Color;
namespace MeshWiz.OpenTK;

public class LineView : IOpenGLControl
{
    private readonly EquatableScopedProperty<bool> _show;
    public bool Show { get=>_show; set=>_show.Value=value; }
    public ICamera Camera{get; set;}

    private readonly FloatingPointScopedProperty<float> _lineWidth;
    public float LineWidth
    {
        get=>_lineWidth;
        set => _lineWidth.Value = float.Max(value, 1f);
    }
    private readonly EquatableScopedProperty<Color4> _color;
    public Color4 Color{get=>_color;set=>_color.Value=value;}

    public SysColor SysColor
    {
        get => SysColor.FromArgb(Color.ToArgb());
        set=> Color = new Color4(value.R,value.G,value.B,value.A);
    }

    public int Argb
    {
        get=>SysColor.ToArgb();
        set=>SysColor=SysColor.FromArgb(value);
    }

    private VertexArrayObject? _vao;
    private BufferObject? _vbo;
    private ShaderProgram?  _shader; 
    public bool GLInitialized { get; private set; }
    private bool _newLine=true;
    private int _uploadedVertexCount;
    public Polyline<Vec3<float>, float> Polyline
    {
        get;
        set
        {
            field=value;
            _newLine = true;
            OutOfDate();
        }
    }

    

    public LineView() : this(Polyline<Vec3<float>, float>.Empty) { }

    public LineView(Polyline<Vec3<float>,float> polyline)
    {
        _show = this.Property(true);
        _lineWidth = this.FloatingPointProperty(1f);
        _color = this.Property(Color4.White);
        Polyline = polyline;
        Camera=OrbitCamera.Default();
        Camera.MoveForwards(-10);
        Color = Color4.White;
        LineWidth = 2;
    }


    public void Init()
    {
        _vao = new VertexArrayObject();
        _shader=ShaderProgram.FromFiles("./Shaders/solid_color/solid_color");
        OpenGLHelper.LogGlError(nameof(LineView));
        GLInitialized = true;
    }
    public void Update(float aspect)
    {
        if (_newLine) UploadLine();
        // RotateCamera();
        UpdateShader(aspect);
    }

    public float DepthOffset { get; set; } = 0.0005f;

    private void UpdateShader(float aspect)
    {
        var (model, view, projection) = Camera.CreateRenderMatrices(aspect);
        var objectColor = Color;
        var camDistance = Camera.Position.DistanceTo(Camera.LookAt);
        var depthOffset =  DepthOffset/(camDistance*camDistance);
        _shader!.ConsumeOutOfDate();
        _shader!.BindAnd()
            .SetUniform(nameof(model), ref model)
            .SetUniform(nameof(view), ref view)
            .SetUniform(nameof(projection), ref projection)
            .SetUniform(nameof(objectColor),in objectColor)
            .SetUniform(nameof(depthOffset), depthOffset)
            .Unbind();
        if(!_shader.ConsumeOutOfDate())
            this.OutOfDate();
        OpenGLHelper.LogGlError(nameof(LineView));
    }

    private void RotateCamera() => Camera.MoveRight(0.001f);

    private void UploadLine()
    {
        _newLine = false;
        _vao!.Bind();
        _vbo?.Unbind();
        _vbo?.Dispose();
        _vbo=new BufferObject(BufferTarget.ArrayBuffer);
        _vbo.BindAnd().BufferData(Polyline.Points,BufferUsageHint.StaticDraw);
        _uploadedVertexCount=Polyline.Points.Length;
        _shader!.Bind();
        int position;
        position =_shader!.GetAttribLoc(nameof(position));
        GL.VertexAttribPointer(position, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
        GL.EnableVertexAttribArray(position);
        _vbo.Unbind();
        _vao!.Unbind();
        OpenGLHelper.LogGlError(nameof(LineView));
    }

   
    public void Render()
    {
        if (!Show) return;
        _shader!.Bind();
        _vao!.Bind();
        ConsumeOutOfDate();   
         var customLineWidth = System.Math.Abs(LineWidth - 1) > 0.0001;
        if (customLineWidth) GL.LineWidth(LineWidth);
        GL.DrawArrays(PrimitiveType.LineStrip, 0, _uploadedVertexCount);
        if (customLineWidth) GL.LineWidth(1); // Clean state
        _vao!.Unbind();
        _shader!.Unbind();
        OpenGLHelper.LogGlError(nameof(LineView));
    }

    public void Dispose()
    {
        GLInitialized = false;
        OutOfDate();
        GC.SuppressFinalize(this);
        _vbo?.Unbind();
        _vbo?.Dispose();
        _vbo = null;
        _vao?.Unbind();
        _vao?.Dispose();
        _vao = null;
        _shader?.Unbind();
        _shader?.Dispose();
        _shader = null;
    }

    private bool _upToDate = false;

    /// <inheritdoc />
    public void OutOfDate()
        => _upToDate = false;

    /// <inheritdoc />
    public bool ConsumeOutOfDate()
    {
        var copy = _upToDate;
        _upToDate=true;
        return copy;
    }
}