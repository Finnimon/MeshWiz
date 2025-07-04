namespace MeshWiz.UI.Avalonia;

public sealed record ShaderProgram(int Handle)
{
    private bool _disposed = false;
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