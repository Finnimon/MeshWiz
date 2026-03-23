using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Math;
using MeshWiz.OpenGL;
using MeshWiz.RefLinq;
using MeshWiz.UpToDate;
using MeshWiz.Utility.Extensions;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MeshWiz.Kinematics.OpenGL;

public class KinematicChainView : IOpenGLControl
{
    private readonly IScopedProperty<KinematicChainState> _chain;

    public KinematicChainState Chain
    {
        get => _chain.Value;
        set => _chain.Value = value;
    }

    private readonly IScopedProperty<IMesh<float>[]> _meshes;

    public IMesh<float>[] Meshes
    {
        get => _meshes.Value;
        set => _meshes.Value = value;
    }

    private readonly IScopedProperty<OrbitCamera> _camera;

    public OrbitCamera Camera
    {
        get => _camera.Value;
        set => _camera.Value = value;
    }

    private bool _upToDate;
    private ShaderProgram? _shader;
    private BufferObject? _ibo;
    private BufferObject? _vbo;
    private VertexArrayObject? _vao;
    private BufferObject? _transformBuffer;


    public Color4 WireFrameColor { get; set; }

    public KinematicChainView()
    {
        _chain = this.ClassProperty(KinematicChainState.Empty());
        _meshes = this.ClassProperty(Array.Empty<IMesh<float>>());
        _camera = this.ClassProperty(OrbitCamera.Default());
    }


    /// <inheritdoc />
    public void OutOfDate() => _upToDate = false;

    /// <inheritdoc />
    public bool ConsumeOutOfDate() => _upToDate.DeferredSet(true)
                                      && Chain.ConsumeOutOfDate()
                                      && (_shader?.ConsumeOutOfDate() ?? true);

    /// <inheritdoc />
    public void Dispose()
    {
        OutOfDate();
        if (!GLInitialized) return;
        _ibo?.Dispose();
        _ibo = null;
        _vbo?.Dispose();
        _vbo = null;
        _transformBuffer?.Dispose();
        _transformBuffer = null;
        _vao?.Unbind();
        _vao = null;
        _vao?.Dispose();
        _vao = null;
        _shader?.Dispose();
        _shader = null;
    }

    /// <inheritdoc />
    public bool Show { get; set; }


    /// <inheritdoc />
    public bool GLInitialized { get; private set; }

    /// <inheritdoc />
    public void Init()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Update(float aspectRatio)
    {
        UpdateShader(aspectRatio);
        UpdateTransforms();
        UpdateMeshes();
    }

    private IMesh<float>[] _lastUploaded = [];

    private void UpdateMeshes()
    {
        if (_lastUploaded.SequenceEqual(Meshes, EqualityComparer<IMesh<float>>.Create(ReferenceEquals))) return;
        var indicated = Mesh.Indexing.Indicate(_meshes.Value.Iterate().SelectMany(x => x).ToArray());
        var transformMatcher = new int[indicated.Indices.Length * 3];
        var pos = Meshes.Length == 0 ? 0 : Meshes[0].Count;
        for (var i = 1; i < Meshes.Length; i++)
        {
            var len=Meshes[i].Count;
            transformMatcher.AsSpan(pos,len).Fill(i);
            pos += len;
        }

        _vao ??= new VertexArrayObject();
        _vao.Bind();
        _vbo ??= new BufferObject(BufferTarget.ArrayBuffer);
        _vbo.BindAnd().BufferData(indicated.Vertices,BufferUsageHint.StaticDraw).Unbind();
        _ibo ??= new BufferObject(BufferTarget.ElementArrayBuffer);
        _ibo.BindAnd().BufferData(indicated.Indices, BufferUsageHint.StaticDraw).Unbind();
        var posLoc= _shader!.GetAttribLoc("position");
        var idxLoc=_shader.GetAttribLoc("transformIndex");
        
        
        OpenGLHelper.LogGlError(nameof(KinematicChainView));
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct KinematicVertex
    {
    }

    private Mat4x4<double>[] _lastPoses = [];
    private void UpdateTransforms()
    {
        var poses = Chain.AbsoluteConnectors.Select(pose => pose.AsMat4x4()).ToArray();
        if (poses.SequenceEqual(_lastPoses)) return;
        _lastPoses = poses;
        _transformBuffer!.BindAnd().BufferData(_lastPoses, BufferUsageHint.StaticDraw);
    }

    public Mat4x4<float> Model { get; set; } = Mat4x4<float>.Identity;
    private void UpdateShader(float aspectRatio)
    {
        var model = Model;
        var (view, projection) = Camera.CreateRenderMatrices(aspectRatio);
        _shader!.BindAnd()
            .SetUniform(nameof(model),ref model)
            .SetUniform(nameof(view), ref view)
            .SetUniform(nameof(projection), ref projection)
            .SetUniform("color", WireFrameColor);
    }

    /// <inheritdoc />
    public void Render()
    {
        if (!Show) return;
    }
}