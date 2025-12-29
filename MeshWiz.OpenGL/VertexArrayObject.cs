namespace MeshWiz.OpenTK;

public sealed record VertexArrayObject(int Handle):IDisposable
{
    private bool _disposed = false;
    public VertexArrayObject() : this(GL.GenVertexArray()) { }
    public void Bind() => GL.BindVertexArray(Handle);

    public VertexArrayObject BindAnd()
    {
        Bind();
        return this;
    }
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