using System.Runtime.CompilerServices;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclEvent(IntPtr Handle) : IDisposable
{
    public static implicit operator CLEvent(OclEvent obj) => Unsafe.As<OclEvent, CLEvent>(ref obj);
    public static implicit operator OclEvent(CLEvent obj) => Unsafe.As<CLEvent, OclEvent>(ref obj);
    public static OclEvent Create(CLEvent obj) => obj;


    public void Retain() => CL.RetainEvent(this);
    public void Dispose() => CL.ReleaseEvent(this);
}