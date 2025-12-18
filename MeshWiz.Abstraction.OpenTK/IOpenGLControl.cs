using MeshWiz.UpToDate;

namespace MeshWiz.Abstraction.OpenTK;

public interface IOpenGLControl : IDisposable, IUpToDate
{
    public bool Show { get; set; }
    public bool GLInitialized { get; }
    public void Init();
    public void Update(float aspectRatio);
    public void Render();
}
