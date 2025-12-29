using System.Runtime.CompilerServices;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclProgram(IntPtr Handle) : IDisposable
{
    /// <inheritdoc />
    public static implicit operator OclProgram(CLProgram lowLevel)
        => Unsafe.As<CLProgram, OclProgram>(ref lowLevel);

    /// <inheritdoc />
    public static implicit operator CLProgram(OclProgram highLevel)
        => Unsafe.As<OclProgram, CLProgram>(ref highLevel);

    /// <inheritdoc />
    public static OclProgram Abstract(CLProgram lowLevel)
        => lowLevel;

    /// <inheritdoc />
    public static CLProgram LowLevel(OclProgram highLevel)
        => highLevel;

    /// <inheritdoc />
    public void Dispose() => CL.ReleaseProgram(this);

    public void Retain() => CL.RetainProgram(this);
}