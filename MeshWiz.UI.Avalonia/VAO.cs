namespace MeshWiz.UI.Avalonia;

public sealed record VAO(int Handle):IDisposable
{
    private bool _disposed = false;
    public VAO() : this(GL.GenVertexArray()) { }
    public void Bind() => GL.BindVertexArray(Handle);
    public void Unbind() => GL.BindVertexArray(0);
    public void Delete()=>GL.DeleteVertexArray(Handle);
    public void Dispose()
    {
        if (_disposed) return; 
        _disposed = true;
        Unbind();
        Delete();
    }
}