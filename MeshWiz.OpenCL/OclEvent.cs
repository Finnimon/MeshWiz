using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct OclEvent(nint Handle) : IDisposable
{
    public Result<CLResultCode, OclContext> Context => GetContext(this);
    public Result<CLResultCode, OclCommandQueue> Queue => GetQueue(this);
    public Result<CLResultCode, int> ReferenceCount => GetRefCount(this);
    public Result<CLResultCode, CommandExecutionStatus> ExecutionStatus => GetExecStatus(this);

    public static implicit operator CLEvent(OclEvent obj) => new(obj.Handle);
    public static implicit operator OclEvent(CLEvent obj) => new(obj.Handle);
    public static OclEvent Create(CLEvent obj) => obj;

    private sealed class AwaitableState
    {
        public CL.ClEventCallback? Callback;
    }

    public Task<CommandExecutionStatus> MakeAwaitable()
    {
        AwaitableState state = new();
        var src = new TaskCompletionSource<CommandExecutionStatus>(state);
        state.Callback = (_, _) =>
        {
            src.SetResult(CommandExecutionStatus.Complete);
            state.Callback = null;
        };

        CL.SetEventCallback(this, 0, state.Callback!);

        return src.Task;
    }


    public void Retain() => CL.RetainEvent(this);
    public void Dispose() => CL.ReleaseEvent(this);

    public static Result<CLResultCode, byte[]> GetInfo(CLEvent ev, EventInfo target)
        => CL.GetEventInfo(ev, target, out var dat).AsResult(dat);

    public static Result<CLResultCode, OclContext> GetContext(CLEvent ev) =>
        GetInfo(ev, EventInfo.Context)
            .Select(dat => Unsafe.ReadUnaligned<CLContext>(in dat[0]))
            .Select(OclContext.Create);

    public static Result<CLResultCode, OclCommandQueue> GetQueue(CLEvent ev)
        => GetInfo(ev, EventInfo.CommandQueue)
            .Select(dat => Unsafe.ReadUnaligned<CLCommandQueue>(in dat[0]))
            .Select(OclCommandQueue.Create);

    public static Result<CLResultCode, int> GetRefCount(CLEvent ev)
        => GetInfo(ev, EventInfo.ReferenceCount)
            .Select(dat => BitConverter.ToInt32(dat));

    public static Result<CLResultCode, CommandExecutionStatus> GetExecStatus(CLEvent ev)
        => GetInfo(ev, EventInfo.CommandExecutionStatus)
            .Select(dat => Unsafe.ReadUnaligned<CommandExecutionStatus>(in dat[0]));
}