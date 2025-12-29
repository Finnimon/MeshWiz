using Avalonia.OpenGL;
using OpenTK;

namespace MeshWiz.Avalonia;

public sealed record AvaloniaGLContext(GlInterface AvaloniaInterface) : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => AvaloniaInterface.GetProcAddress(procName);
}