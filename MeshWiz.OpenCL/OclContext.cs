using System.Runtime.CompilerServices;
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


    public Result<CLResultCode,nint[]> Properties => GetProperties(this);
    public OclDevice[] Devices => GetDevices(this);
    public int DeviceCount => GetDeviceCount(this);
    public bool IsMultiDevice => DeviceCount != 1;
    public int ReferenceCount => GetReferenceCount(this);
    
    public static Result<CLResultCode,OclContext> Create(params ReadOnlySpan<OclDevice> devices)
    {
        if(devices.Length==0)
            return Result<CLResultCode, OclContext>.DefaultFailure;
        return CL.CreateContext(nint.Zero,
            devices.As<OclDevice, CLDevice>().ToArray(),
            0,
            0,
            out var resultCode)
            .AsResult(resultCode)
            .Select(Create);
    }

    public static Result<CLResultCode, byte[]> GetInfo(CLContext context, ContextInfo info)
        => CL.GetContextInfo(context, info, out var bytes).AsResult(bytes);


    public static Result<CLResultCode,OclContext> Create(DeviceType type)
    {
        return CL.CreateContextFromType(nint.Zero, type, nint.Zero, nint.Zero, out var code)
            .AsResult(code).Select(Create);
    }

    public static bool TryDeviceType(DeviceType type, out OclContext context, bool log = true)
    {
        context = CL.CreateContextFromType(0, type,0,0, out var result);
        if (log) result.LogError();
        return result is CLResultCode.Success;
    }

    public static OclDevice[] GetDevices(CLContext context)
    => GetInfo(context, ContextInfo.Devices).Select(b => b.As<byte,CLDevice>().Select(OclDevice.Create).ToArray());

    public static int GetReferenceCount(CLContext context)
        => GetInfo(context, ContextInfo.ReferenceCount).Select(b => BitConverter.ToInt32(b));


    public static Result<CLResultCode,int> GetDeviceCount(CLContext context)
        => GetInfo(context, ContextInfo.NumberOfDevices).Select(b => BitConverter.ToInt32(b));

    public static nint[] GetProperties(CLContext context)
        => GetInfo(context, ContextInfo.Properties).Select(x => x.As<byte, nint>().ToArray());

    public Result<CLResultCode, OclBuffer<T>> CreateBuffer<T>(MemoryFlags flags, int count) where T : unmanaged
        => OclBuffer<T>.Create(this, flags, count);

    public Result<CLResultCode,OclBuffer> CreateBuffer(MemoryFlags flags, nuint size) => CL.CreateBuffer(this, flags, size,0, out var code).AsResult(code)
            .Select(OclBuffer.Create);

    public Result<CLResultCode, OclCommandQueue> CreateCommandQueue(OclDevice device = default, nint properties = 0)
    {
        if (device == default)
            device = this.Devices[0];
        return CL.CreateCommandQueueWithProperties(this, device, properties, out var code)
            .AsResult(code)
            .Select(OclCommandQueue.Create);
    }
    
    

    /// <inheritdoc />
    public void Dispose() => CL.ReleaseContext(this);
    public Result<CLResultCode> Retain() => CL.RetainContext(this);


    public Result<CLResultCode, OclProgram> CreateProgramFromSource(string source)
    {
        return CL.CreateProgramWithSource(this, source, out var res)
            .AsResult(res)
            .Select(OclProgram.Create);
    }
}