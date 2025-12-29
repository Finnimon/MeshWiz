using System.Runtime.CompilerServices;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclImage(IntPtr Handle)
{
    public static implicit operator CLImage(OclImage obj) => Unsafe.As<OclImage, CLImage>(ref obj);
    public static implicit operator OclImage(CLImage obj) => Unsafe.As<CLImage, OclImage>(ref obj);
    public static OclImage Create(CLImage obj) => obj;
    public void Dispose() => CL.ReleaseMemoryObject(this);
    public void Retain() => CL.RetainMemoryObject(this);
}