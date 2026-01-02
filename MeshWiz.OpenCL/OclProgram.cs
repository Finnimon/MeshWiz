using System.Runtime.CompilerServices;
using System.Text;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclProgram(nint Handle) : IDisposable
{
    public Result<CLResultCode,int> ReferenceCount => GetRefCount(this);
    public Result<CLResultCode,int> DeviceCount => GetNumDevices(this);
    public Result<CLResultCode,int> KernelCount => GetNumKernels(this);
    public Result<CLResultCode, string[]> KernelNames => GetKernelNames(this);
    public Result<CLResultCode,string> Source => GetSource(this);
    public static implicit operator OclProgram(CLProgram lowLevel)
        => Unsafe.As<CLProgram, OclProgram>(ref lowLevel);

    public static implicit operator CLProgram(OclProgram highLevel)
        => Unsafe.As<OclProgram, CLProgram>(ref highLevel);

    
    public static OclProgram Create(CLProgram lowLevel)
        => lowLevel;

    
    
    public void Dispose() => CL.ReleaseProgram(this);

    public void Retain() => CL.RetainProgram(this);

    public Result<CLResultCode> Build(ReadOnlySpan<OclDevice> devices,string opts="",nint notifyCb=0,nint userDat=0) =>
        CL.BuildProgram(this,
            (uint)devices.Length,
            devices.Select(d => (CLDevice)d)
                .ToArray(),
            opts,
            notifyCb,
            userDat);

    public static Result<CLResultCode,int> GetRefCount(CLProgram program) => GetInfo(program, ProgramInfo.ReferenceCount)
        .Select(x => BitConverter.ToInt32(x));

    public static Result<CLResultCode,int> GetNumDevices(CLProgram program) => GetInfo(program, ProgramInfo.NumberOfDevices)
        .Select(x => BitConverter.ToInt32(x));
    public static Result<CLResultCode,int> GetNumKernels(CLProgram program) => GetInfo(program, ProgramInfo.NumberOfKernels)
        .Select(x => BitConverter.ToInt32(x));
    public static Result<CLResultCode,string> GetSource(CLProgram program) => GetInfo(program, ProgramInfo.Source)
        .Select(OclHelper.GetCLString);

    public static Result<CLResultCode, string[]> GetKernelNames(CLProgram program) =>
        GetInfo(program, ProgramInfo.KernelNames)
            .Select(OclHelper.GetCLStrings);
    public Result<CLResultCode, string> BuildLog(OclDevice device) => GetBuildLog(this, device);
    public static Result<CLResultCode, string> GetBuildLog(CLProgram program, OclDevice device)
        => GetBuildInfo(program, device, ProgramBuildInfo.Log).Select(OclHelper.GetCLString);
    public static Result<CLResultCode, byte[]> GetInfo(CLProgram program,ProgramInfo target) 
        => CL.GetProgramInfo(program, target, out var bytes).AsResult(bytes);

    public static Result<CLResultCode, byte[]> GetBuildInfo(CLProgram program,OclDevice device, ProgramBuildInfo target) 
        => CL.GetProgramBuildInfo(program, device, target, out var bytes).AsResult(bytes);
    
    public Result<CLResultCode, OclKernel> CreateKernel(string name)
        => CL.CreateKernel(this, name, out var code)
            .AsResult(code)
            .Select(OclKernel.Create);
}

[Flags]
public enum OclBuildOpts
{
    None=0,
    //Math intrinsic
    
}