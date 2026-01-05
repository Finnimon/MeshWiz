using System.ComponentModel;

namespace MeshWiz.OpenGL;

[TypeConverter(typeof(EnumConverter)), Flags]
public enum RenderMode
{
    None = 0,
    Solid = 1 << 0,
    Wireframe = 1 << 1
}