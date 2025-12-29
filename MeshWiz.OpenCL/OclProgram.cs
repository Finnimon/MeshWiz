using System.Runtime.CompilerServices;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclProgram(IntPtr Handle) : IDisposable
{
    
    public static implicit operator OclProgram(CLProgram lowLevel)
        => Unsafe.As<CLProgram, OclProgram>(ref lowLevel);

    public static implicit operator CLProgram(OclProgram highLevel)
        => Unsafe.As<OclProgram, CLProgram>(ref highLevel);

    
    public static OclProgram Create(CLProgram lowLevel)
        => lowLevel;

    
    
    public void Dispose() => CL.ReleaseProgram(this);

    public void Retain() => CL.RetainProgram(this);
}