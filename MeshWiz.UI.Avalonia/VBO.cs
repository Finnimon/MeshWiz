namespace MeshWiz.UI.Avalonia;

public sealed record VBO(int Handle, BufferTarget Target)
{
    bool _disposed = false;
    public VBO(BufferTarget Target) : this(GL.GenBuffer(), Target) { }
    public void Bind() => GL.BindBuffer(Target, Handle);
    public void Unbind() => GL.BindBuffer(Target, 0);
    public void Delete()=>GL.DeleteBuffer(Handle);
    public void Dispose()
    {
        if (_disposed) return; 
        _disposed = true;
        Unbind();
        Delete();
    }
}