using System.Runtime.CompilerServices;
using MeshWiz.Contracts;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclImage(IntPtr Handle) : IAbstraction<OclImage, CLImage>
{
    public static implicit operator CLImage(OclImage obj) => Unsafe.As<OclImage, CLImage>(ref obj);
    public static implicit operator OclImage(CLImage obj) => Unsafe.As<CLImage, OclImage>(ref obj);
    public static CLImage LowLevel(OclImage obj) => obj;
    public static OclImage Abstract(CLImage obj) => obj;
    public void Dispose() => CL.ReleaseMemoryObject(this);
    public void Retain() => CL.RetainMemoryObject(this);
}