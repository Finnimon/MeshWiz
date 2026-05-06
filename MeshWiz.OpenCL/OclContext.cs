using System;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclContext(nint Handle) : IDisposable
{
    public static implicit operator OclContext(CLContext lowLevel) => new(lowLevel.Handle);

    public static implicit operator CLContext(OclContext highLevel) => new(highLevel.Handle);

    public static OclContext Create(CLContext lowLevel) => lowLevel;


    public Result<OclResultCode,nint[]> Properties => GetProperties(this);
    public OclDevice[] Devices => GetDevices(this);
    public int DeviceCount => GetDeviceCount(this);
    public bool IsMultiDevice => DeviceCount != 1;
    public int ReferenceCount => GetReferenceCount(this);
    
    public static Result<OclResultCode,OclContext> Create(params ReadOnlySpan<OclDevice> devices)
    {
        if(devices.Length==0)
            return Result<OclResultCode, OclContext>.DefaultFailure;
        return CL.CreateContext(nint.Zero,
            devices.As<OclDevice, CLDevice>().ToArray(),
            0,
            0,
            out var resultCode)
            .AsResult(resultCode)
            .Select(Create);
    }

    public static Result<OclResultCode, byte[]> GetInfo(CLContext context, ContextInfo info)
        => CL.GetContextInfo(context, info, out var bytes).AsResult(bytes);


    public static Result<OclResultCode,OclContext> Create(DeviceType type)
    {
        return CL.CreateContextFromType(nint.Zero, type, nint.Zero, nint.Zero, out var code)
            .AsResult(code).Select(Create);
    }


    public static OclDevice[] GetDevices(CLContext context)
    => GetInfo(context, ContextInfo.Devices).Select(b => b.As<byte,CLDevice>().Select(OclDevice.Create).ToArray());

    public static int GetReferenceCount(CLContext context)
        => GetInfo(context, ContextInfo.ReferenceCount).Select(b => BitConverter.ToInt32(b));


    public static Result<OclResultCode,int> GetDeviceCount(CLContext context)
        => GetInfo(context, ContextInfo.NumberOfDevices).Select(b => BitConverter.ToInt32(b));

    public static nint[] GetProperties(CLContext context)
        => GetInfo(context, ContextInfo.Properties).Select(x => x.As<byte, nint>().ToArray());

    public Result<OclResultCode, OclBuffer<T>> CreateBuffer<T>(MemoryFlags flags, int count) where T : unmanaged
        => OclBuffer<T>.Create(this, flags, count);

    public Result<OclResultCode,OclBuffer> CreateBuffer(MemoryFlags flags, nuint size) => CL.CreateBuffer(this, flags, size,0, out var code).AsResult(code)
            .Select(OclBuffer.Create);

    public Result<OclResultCode, OclCommandQueue> CreateCommandQueue(OclDevice device = default, nint properties = 0)
    {
        if (device == default)
            device = this.Devices[0];
        return CL.CreateCommandQueueWithProperties(this, device, properties, out var code)
            .AsResult(code)
            .Select(OclCommandQueue.Create);
    }
    
    

    /// <inheritdoc />
    public void Dispose() => CL.ReleaseContext(this);
    public Result<OclResultCode> Retain() =>(OclResultCode) CL.RetainContext(this);


    public Result<OclResultCode, OclProgram> CreateProgramFromSource(string source)
    {
        return CL.CreateProgramWithSource(this, source, out var res)
            .AsResult(res)
            .Select(OclProgram.Create);
    }
}