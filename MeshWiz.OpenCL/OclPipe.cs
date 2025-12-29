using System.Runtime.CompilerServices;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclPipe(IntPtr Handle)
{
    public static implicit operator CLPipe(OclPipe obj) => Unsafe.As<OclPipe, CLPipe>(ref obj);
    public static implicit operator OclPipe(CLPipe obj) => Unsafe.As<CLPipe, OclPipe>(ref obj);
    public static OclPipe Create(CLPipe obj) => obj;
    public void Dispose() => CL.ReleaseMemoryObject(this);
    public void Retain() => CL.RetainMemoryObject(this);
}