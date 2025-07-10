using Avalonia.OpenGL;
using OpenTK;

namespace MeshWiz.Abstraction.Avalonia;

public sealed record AvaloniaGLContext(GlInterface AvaloniaInterface) : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => AvaloniaInterface.GetProcAddress(procName);
}