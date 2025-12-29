using System.Runtime.CompilerServices;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclCommandQueue(IntPtr Handle)
    : IDisposable
{
    public static implicit operator CLCommandQueue(OclCommandQueue obj) =>
        Unsafe.As<OclCommandQueue, CLCommandQueue>(ref obj);

    public static implicit operator OclCommandQueue(CLCommandQueue obj) =>
        Unsafe.As<CLCommandQueue, OclCommandQueue>(ref obj);

    public static OclCommandQueue Create(CLCommandQueue obj) => obj;


    public void Retain() => CL.RetainCommandQueue(this);
    public void Dispose() => CL.ReleaseCommandQueue(this);
}