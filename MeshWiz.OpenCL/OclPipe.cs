using System.Runtime.CompilerServices;
using MeshWiz.Contracts;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclPipe(IntPtr Handle) : IAbstraction<OclPipe, CLPipe>
{
    public static implicit operator CLPipe(OclPipe obj) => Unsafe.As<OclPipe, CLPipe>(ref obj);
    public static implicit operator OclPipe(CLPipe obj) => Unsafe.As<CLPipe, OclPipe>(ref obj);
    public static CLPipe LowLevel(OclPipe obj) => obj;
    public static OclPipe Abstract(CLPipe obj) => obj;
    public void Dispose() => CL.ReleaseMemoryObject(this);
    public void Retain() => CL.RetainMemoryObject(this);
}