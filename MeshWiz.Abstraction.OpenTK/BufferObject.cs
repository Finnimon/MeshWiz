using MeshWiz.Contracts;

namespace MeshWiz.Abstraction.OpenTK;

public sealed record BufferObject(int Handle, BufferTarget Target)
{
    private bool _disposed = false;
    public BufferObject(BufferTarget Target) : this(GL.GenBuffer(), Target) { }
    public void Bind() => GL.BindBuffer(Target, Handle);
    
    public BufferObject BindAnd()
    {
        Bind();
        return this;
    }
    public void Unbind() => GL.BindBuffer(Target, 0);
    public void Delete()=>GL.DeleteBuffer(Handle);
    public void Dispose()
    {
        if (_disposed) return; 
        _disposed = true;
        Unbind();
        Delete();
    }

    public BufferObject BufferData<T>(T[] data,BufferUsageHint usage)
    where T : unmanaged
    {
        GL.BufferData(Target, OpenGLHelper.UnsafeByteSize(data), data, usage);
        return this;
    }
}