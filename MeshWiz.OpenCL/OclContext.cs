using System.Runtime.CompilerServices;
using MeshWiz.RefLinq;
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

    public static Result<CLResultCode, byte[]> GetInfo(CLContext context, ContextInfo info)
        => CL.GetContextInfo(context, info, out var bytes).AsResult(bytes);

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
    => GetInfo(context, ContextInfo.Devices).Select(b => b.As<byte,CLDevice>().Select(OclDevice.Create).ToArray());

    public static int GetReferenceCount(CLContext context)
        => GetInfo(context, ContextInfo.ReferenceCount).Select(b => BitConverter.ToInt32(b));


    public static Result<CLResultCode,int> GetDeviceCount(CLContext context)
        => GetInfo(context, ContextInfo.NumberOfDevices).Select(b => BitConverter.ToInt32(b));

    public static IntPtr[] GetProperties(CLContext context)
        => GetInfo(context, ContextInfo.Properties).Select(x => x.As<byte, IntPtr>().ToArray());

    public Result<CLResultCode,OclBuffer> CreateBuffer<T>(MemoryFlags flags, Span<T> data) where T : unmanaged 
        => CL.CreateBuffer(this, flags, data, out var code).AsResult(code)
            .Select(OclBuffer.Create);

    public bool TryCreateBuffer<T>(MemoryFlags flags, Span<T> data, out OclBuffer buffer)
        where T : unmanaged
        => CreateBuffer(flags, data).TryGetValue(out buffer);

    public bool TryCreateCommandQueue(OclDevice device,IntPtr properties, out OclCommandQueue commandQueue, bool log=true)
    {
        commandQueue= CL.CreateCommandQueueWithProperties(this, device.Handle, properties, out var code);
        if (log) code.LogError();
        return code is CLResultCode.Success;
    }
    
    

    /// <inheritdoc />
    public void Dispose() => CL.ReleaseContext(this);
    public Result<CLResultCode> Retain() => CL.RetainContext(this);
}