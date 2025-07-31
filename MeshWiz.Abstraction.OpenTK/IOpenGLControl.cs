namespace MeshWiz.Abstraction.OpenTK;

public interface IOpenGLControl : IDisposable
{
    public bool Show { get; set; }
    public bool GLInitialized { get; }
    public void Init();
    public void Update(float aspectRatio);
    public void Render();
}
