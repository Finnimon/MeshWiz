namespace MeshWiz.UI.Avalonia;

public interface IOpenGLControl : IDisposable
{
    public bool IsInitialized { get; set; }
    public void Init();
    public void Update();
    public void Render();
}