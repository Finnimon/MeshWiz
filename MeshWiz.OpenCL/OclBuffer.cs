using System.Runtime.CompilerServices;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclBuffer(IntPtr Handle) : IDisposable
{
    public static implicit operator CLBuffer(OclBuffer obj) => Unsafe.As<OclBuffer, CLBuffer>(ref obj);
    public static implicit operator OclBuffer(CLBuffer obj) => Unsafe.As<CLBuffer, OclBuffer>(ref obj);
    public static OclBuffer Create(CLBuffer obj) => obj;

    public void Dispose() => CL.ReleaseMemoryObject(this);
    public Result<CLResultCode> Retain() => CL.RetainMemoryObject(this);
}