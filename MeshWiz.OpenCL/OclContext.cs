using System.Runtime.CompilerServices;
using MeshWiz.Contracts;
using MeshWiz.Utility.Extensions;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclContext(IntPtr Handle) : IAbstraction<OclContext, CLContext>, IDisposable
{
    /// <inheritdoc />
    public static implicit operator OclContext(CLContext lowLevel) => new(lowLevel.Handle);

    /// <inheritdoc />
    public static implicit operator CLContext(OclContext highLevel) => new(highLevel.Handle);

    /// <inheritdoc />
    public static OclContext Abstract(CLContext lowLevel) => lowLevel;

    /// <inheritdoc />
    public static CLContext LowLevel(OclContext highLevel) => highLevel;

    public IntPtr[] Properties => GetProperties(this);
    public OclDevice[] Devices => GetDevices(this);
    public int DeviceCount => GetDeviceCount(this);
    public bool IsMultiDevice => DeviceCount != 1;
    public int ReferenceCount => GetReferenceCount(this);
    
    public static OclContext FromDevices(params OclDevice[] devices)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(devices.Length, 0, nameof(devices));
        OclContext context = CL.CreateContext(IntPtr.Zero, devices.As<OclDevice, CLDevice>().ToArray(),
            IntPtr.Zero, IntPtr.Zero, out var resultCode);
        resultCode.ThrowOnError();
        return context;
    }

    public static OclDevice[] GetDevices(CLContext context)
    {
        CL.GetContextInfo(context, ContextInfo.Devices, out var value).ThrowOnError();
        _ = value ?? throw new NullReferenceException();
        return value.As<byte, OclDevice>().ToArray();
    }

    public static int GetReferenceCount(CLContext context)
    {
        CL.GetContextInfo(context, ContextInfo.ReferenceCount, out var value).LogError();
        if (value is null) return -1;
        return BitConverter.ToInt32(value);
    }


    public static int GetDeviceCount(CLContext context)
    {
        CL.GetContextInfo(context, ContextInfo.NumberOfDevices, out var value).LogError();
        if (value is null) return -1;
        return BitConverter.ToInt32(value);
    }

    public static unsafe IntPtr[] GetProperties(CLContext context)
    {
        CL.GetContextInfo(context, ContextInfo.Properties, out var value).ThrowOnError();
        _ = value ?? throw new NullReferenceException();
        var count = value.Length / Unsafe.SizeOf<IntPtr>();
        fixed (void* ptr = &value[0]) return new ReadOnlySpan<IntPtr>(ptr, count).ToArray();
    }


    /// <inheritdoc />
    public void Dispose() => CL.ReleaseContext(this);

    public void Retain() => CL.RetainContext(this);
}