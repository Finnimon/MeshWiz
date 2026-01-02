using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly partial record struct OclKernel(nint Handle) : IDisposable
{
    public static implicit operator CLKernel(OclKernel obj) => new(obj.Handle);
    public static implicit operator OclKernel(CLKernel obj) => new(obj.Handle);

    public Result<CLResultCode, int> ReferenceCount => GetRefCount(this);
    public Result<CLResultCode, int> ArgumentCount => GetNumArgs(this);
    public Result<CLResultCode, int> MaxSubGroupCount => GetMaxNumSubGroups(this);
    public Result<CLResultCode, int> CompileSubGroupCount => GetCompileNumSubGroups(this);
    public Result<CLResultCode, string> FunctionName => GetFunctionName(this);
    public Result<CLResultCode, OclContext> Context => GetContext(this);
    public Result<CLResultCode, OclProgram> Program => GetProgram(this);
    public Arg[] Arguments => GetArguments(this);
    public Arg GetArgument(uint index) => GetArgument(this, index);
    public Dictionary<string, Arg> ArgMap => EnumerateArgs().ToDictionary(arg => arg.Name.Value,StringComparer.Ordinal);

    private IEnumerable<Arg> EnumerateArgs() => Enumerable.Sequence(0u,(uint)ArgumentCount.Select(i=>i-1).OrElse(0),1u).Select(GetArgument);

    public static OclKernel Create(CLKernel obj) => obj;
    

    public void Retain() => CL.RetainKernel(this);
    public void Dispose() => CL.ReleaseKernel(this);

    public static Result<CLResultCode, byte[]> GetInfo(CLKernel kernel, KernelInfo target)
        => CL.GetKernelInfo(kernel, target, out var bytes).AsResult(bytes);

    public static Result<CLResultCode, int> GetNumArgs(CLKernel kernel)
        => GetInfo(kernel, KernelInfo.NumberOfArguments)
            .Select(x => BitConverter.ToInt32(x));

    public static Result<CLResultCode, int> GetRefCount(CLKernel kernel)
        => GetInfo(kernel, KernelInfo.ReferenceCount)
            .Select(x => BitConverter.ToInt32(x));

    public static Result<CLResultCode, int> GetMaxNumSubGroups(CLKernel kernel)
        => GetInfo(kernel, KernelInfo.MaxNumberOfSubGroups)
            .Select(x => BitConverter.ToInt32(x));

    public static Result<CLResultCode, int> GetCompileNumSubGroups(CLKernel kernel)
        => GetInfo(kernel, KernelInfo.CompileNumberOfSubGroups)
            .Select(x => BitConverter.ToInt32(x));

    public static Result<CLResultCode, OclContext> GetContext(CLKernel kernel)
        => GetInfo(kernel, KernelInfo.Context)
            .Select(x => Unsafe.ReadUnaligned<CLContext>(in x[0]))
            .Select(OclContext.Create);

    public static Result<CLResultCode, OclProgram> GetProgram(CLKernel kernel)
        => GetInfo(kernel, KernelInfo.Program)
            .Select(x => Unsafe.ReadUnaligned<CLProgram>(in x[0]))
            .Select(OclProgram.Create);

    public static Result<CLResultCode, string> GetFunctionName(CLKernel kernel)
        => GetInfo(kernel, KernelInfo.FunctionName)
            .Select(OclHelper.GetCLString);

    internal static Result<CLResultCode, byte[]> GetArgInfo(CLKernel kernel, uint index, KernelArgInfo target)
        => CL.GetKernelArgInfo(kernel, index, target, out var bytes).AsResult(bytes);

    internal static Result<CLResultCode, string> GetArgInfoString(CLKernel kernel, uint index, KernelArgInfo target) =>
        GetArgInfo(kernel, index, target).Select(OclHelper.GetCLString);

    public static Arg GetArgument(CLKernel kernel, uint index) => new(kernel, index);

    public static Arg[] GetArguments(CLKernel kernel)
        => !GetNumArgs(kernel).TryGetValue(out var numArgs)
            ? []
            : Enumerable.Sequence(0u, (uint)numArgs-1, 1u)
                .Select(i => new Arg(kernel, i))
                .ToArray();

    public static Result<CLResultCode, byte[]> GetInfo(CLKernel kernel, CLDevice device, KernelWorkGroupInfo target)
        => CL.GetKernelWorkGroupInfo(kernel, device, target, out var bytes).AsResult(bytes);

    public Result<CLResultCode, OclEvent> Run(OclCommandQueue queue,
        nuint[] workSizes,
        nuint[]? globalWorkOffsets = null,
        nuint[]? localWorkSizes = null,
        OclEvent[]? waitList = null)
    {
        var waitFor = waitList?.Select(ev => (CLEvent)ev).ToArray();
        uint waitCount=(uint)(waitFor?.Length ?? 0);
        return CL.EnqueueNDRangeKernel(queue,
            this,
            (uint)workSizes.Length,
            globalWorkOffsets,
            workSizes,
            localWorkSizes,
            waitCount,
            waitFor,
            out var eventHandle)
            .AsResult<OclEvent>(eventHandle);
    }
    

    public Task<Result<CLResultCode>> RunAsync(OclCommandQueue queue,
        nuint[] workSizes,
        nuint[]? globalWorkOffsets = null,
        nuint[]? localWorkSizes = null,
        OclEvent[]? waitList = null)
    {
        var res= Run(queue,workSizes, globalWorkOffsets, localWorkSizes, waitList);
        return !res.TryGetValue(out var ev)
            ? Task.FromResult(Result<CLResultCode>.Failure(res.Info))
            : ev.MakeAwaitable()
                .ContinueWith(a =>
                    a is { Status: TaskStatus.RanToCompletion, Result: CommandExecutionStatus.Complete }
                        ? Result<CLResultCode>.Success()
                        : Result<CLResultCode>.Failure());
    }
    
    public Task<Result<CLResultCode>> RunAsync(OclCommandQueue queue,
        nuint workSize,
        OclEvent[]? waitList = null)
    {
        var res= Run(queue,workSize ,waitList:waitList);
        return !res.TryGetValue(out var ev)
            ? Task.FromResult(Result<CLResultCode>.Failure(res.Info))
            : ev.MakeAwaitable()
                .ContinueWith(a =>
                    a is { Status: TaskStatus.RanToCompletion, Result: CommandExecutionStatus.Complete }
                        ? Result<CLResultCode>.Success()
                        : Result<CLResultCode>.Failure());
    }

    public Result<CLResultCode, OclEvent> Run(OclCommandQueue queue, nuint workSizes, OclEvent[]? waitList=null) => Run(queue, [workSizes], waitList: waitList);
}