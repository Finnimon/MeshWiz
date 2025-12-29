using System.Runtime.CompilerServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclContext(IntPtr Handle) : IDisposable
{
    public static implicit operator OclContext(CLContext lowLevel) => new(lowLevel.Handle);

    public static implicit operator CLContext(OclContext highLevel) => new(highLevel.Handle);

    public static OclContext Create(CLContext lowLevel) => lowLevel;


    public Result<CLResultCode,IntPtr[]> Properties => GetProperties(this);
    public OclDevice[] Devices => GetDevices(this);
    public int DeviceCount => GetDeviceCount(this);
    public bool IsMultiDevice => DeviceCount != 1;
    public int ReferenceCount => GetReferenceCount(this);
    
    public static OclContext FromDevices(params OclDevice[] devices)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(devices.Length, 0, nameof(devices));
        OclContext context = CL.CreateContext(IntPtr.Zero, devices.As
                <OclDevice, CLDevice>().ToArray(),
            IntPtr.Zero, IntPtr.Zero, out var resultCode);
        resultCode.ThrowOnError();
        return context;
    }

    public static OclContext FromDevice(OclDevice obj)
        => FromDevices(obj);

    public static OclContext FromDeviceType(DeviceType type)
    {
        var context = CL.CreateContextFromType(IntPtr.Zero, type,IntPtr.Zero ,IntPtr.Zero, out var result);
        result.ThrowOnError();
        return context;
    }

    public static bool TryDeviceType(DeviceType type, out OclContext context, bool log = true)
    {
        context = CL.CreateContextFromType(IntPtr.Zero, type,IntPtr.Zero,IntPtr.Zero, out var result);
        if (log) result.LogError();
        return result is CLResultCode.Success;
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
        => GetInfo(context, ContextInfo.Properties).Select(x => x.As<byte, IntPtr>().ToArray());
    public static Result<CLResultCode, byte[]> GetInfo(CLContext context, ContextInfo target)
        => CL.GetContextInfo(context, target, out var dat).AsResult(dat);

    public OclBuffer CreateBuffer<T>(MemoryFlags flags, Span<T> data) where T : unmanaged
    {
        var buf= CL.CreateBuffer(this, flags, data, out var code);
        code.ThrowOnError();
        return buf;
    }

    public bool TryCreateBuffer<T>(MemoryFlags flags, Span<T> data, out OclBuffer buffer, bool log=true) 
        where T : unmanaged
    {
        buffer= CL.CreateBuffer(this, flags, data, out var code);
        if (log) code.LogError();
        return code is CLResultCode.Success;
    }

    public bool TryCreateCommandQueue(OclDevice device,IntPtr properties, out OclCommandQueue commandQueue, bool log=true)
    {
        commandQueue= CL.CreateCommandQueueWithProperties(this, device.Handle, properties, out var code);
        if (log) code.LogError();
        return code is CLResultCode.Success;
    }
    
    

    /// <inheritdoc />
    public void Dispose() => CL.ReleaseContext(this);
    public void Retain() => CL.RetainContext(this);
}