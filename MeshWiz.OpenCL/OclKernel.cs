using System.Runtime.CompilerServices;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclKernel(IntPtr Handle) : IDisposable
{
    public static implicit operator CLKernel(OclKernel obj) => Unsafe.As<OclKernel, CLKernel>(ref obj);
    public static implicit operator OclKernel(CLKernel obj) => Unsafe.As<CLKernel, OclKernel>(ref obj);
    public static CLKernel LowLevel(OclKernel obj) => obj;
    public static OclKernel Abstract(CLKernel obj) => obj;

    public void Retain() => CL.RetainKernel(this);
    public void Dispose() => CL.ReleaseKernel(this);
}