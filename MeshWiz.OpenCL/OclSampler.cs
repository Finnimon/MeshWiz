using System.Runtime.CompilerServices;
using MeshWiz.Contracts;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclSampler(IntPtr Handle) : IAbstraction<OclSampler, CLSampler>, IDisposable
{
    public static implicit operator CLSampler(OclSampler obj) => Unsafe.As<OclSampler, CLSampler>(ref obj);
    public static implicit operator OclSampler(CLSampler obj) => Unsafe.As<CLSampler, OclSampler>(ref obj);
    public static CLSampler LowLevel(OclSampler obj) => obj;
    public static OclSampler Abstract(CLSampler obj) => obj;


    public void Retain() => CL.RetainSampler(this);
    public void Dispose() => CL.ReleaseSampler(this);
}