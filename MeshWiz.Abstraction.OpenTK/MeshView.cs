using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.Abstraction.OpenTK;

public class MeshView : IOpenGLControl
{
    public required ICamera Camera { get; set; }

    public RenderMode RenderModeFlags { get; set; } = RenderMode.Solid;
    public Color4 WireFrameColor { get; set; } = Color4.Black;
    public Color4 SolidColor { get; set; } = Color4.Gray;
    public Color4 LightColor { get; set; } = Color4.White;

    private Vector3<float> LightPosition
    {
        get => field != Vector3<float>.NaN ? field : AboveHead;
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
                .Unbind();
    }

    private void UploadMesh()
    {
        throw new NotImplementedException();
    }

    public void Render()
    {
        throw new NotImplementedException();
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
}

[Flags]
public enum RenderMode
{
    None = 0,
    Solid = 1 << 0,
    Wireframe = 1 << 1
}