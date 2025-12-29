using System.Runtime.CompilerServices;
using MeshWiz.Contracts;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclBuffer(IntPtr Handle) : IAbstraction<OclBuffer, CLBuffer>, IDisposable
{
    public static implicit operator CLBuffer(OclBuffer obj) => Unsafe.As<OclBuffer, CLBuffer>(ref obj);
    public static implicit operator OclBuffer(CLBuffer obj) => Unsafe.As<CLBuffer, OclBuffer>(ref obj);
    public static CLBuffer LowLevel(OclBuffer obj) => obj;
    public static OclBuffer Abstract(CLBuffer obj) => obj;

    public void Dispose() => CL.ReleaseMemoryObject(this);
    public void Retain() => CL.RetainMemoryObject(this);
}