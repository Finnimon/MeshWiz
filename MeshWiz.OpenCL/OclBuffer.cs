using System.Runtime.CompilerServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclBuffer(IntPtr Handle) : IDisposable
{
    public static implicit operator CLBuffer(OclBuffer obj) => Unsafe.As<OclBuffer, CLBuffer>(ref obj);
    public static implicit operator OclBuffer(CLBuffer obj) => Unsafe.As<CLBuffer, OclBuffer>(ref obj);
    public static OclBuffer Create(CLBuffer obj) => obj;

    public void Dispose() => CL.ReleaseMemoryObject(this);
    public Result<CLResultCode> Retain() => CL.RetainMemoryObject(this);
}

public sealed record OclBuffer<T>(MemoryFlags MemFlags, OclBuffer Underlying, int Count)
    : IDisposable
    where T : unmanaged
{
    private readonly Once _alive = Bool.Once();
    public bool IsAlive => _alive.ReadValue();
    public nuint ByteSize => GetByteSize(Count);
    public bool HostCanRead => IsAlive && MemFlags.HasAnyFlags(HostReadFlags());
    public bool HostCanWrite => IsAlive && MemFlags.HasAnyFlags(HostWriteFlags());

    public static Result<CLResultCode, OclBuffer<T>> Create(OclContext context, MemoryFlags flags, int count) =>
        context.CreateBuffer(flags, GetByteSize(count))
            .Select(buf => new OclBuffer<T>(flags, buf, count));

    public Result<CLResultCode, OclEvent> EnqueueRead(OclCommandQueue queue, T[] into, bool blocking,
        nuint byteOffset,
        OclEvent[]? waitlist)
    {
        if (!HostCanRead)
            return Result<CLResultCode, OclEvent>.DefaultFailure;
        var waitFor = waitlist?.As<OclEvent, CLEvent>().ToArray();
        return CL.EnqueueReadBuffer(queue, Underlying, blocking, byteOffset, into, waitFor, out var eventHandle)
            .AsResult<OclEvent>(eventHandle);
    }

    public Result<CLResultCode, T[]> ReadBlocking(OclCommandQueue queue, T[]? into = null,uint itemOffset=0, OclEvent[]? waitlist = null)
    {
        var targetCount = Count - itemOffset;
        var hostBuffer = into ?? new T[targetCount];
        var res = EnqueueRead(queue, hostBuffer, true,GetByteSize((int)itemOffset), waitlist);
        return res.FailWhen(ev => ev.ExecutionStatus.When(s => s is CommandExecutionStatus.Complete))
            .Select(_ => hostBuffer);
    }

    public Result<CLResultCode, OclEvent> ReadNonBlocking(OclCommandQueue queue, T[] into,uint itemOffset=0, OclEvent[]? waitlist = null) 
        => EnqueueRead(queue, into, false,GetByteSize((int)itemOffset), waitlist);

    public Task<Result<CLResultCode, T[]>> ReadAsync(OclCommandQueue queue,Range r=default, T[]? into = null,
        OclEvent[]? waitlist = null)
    {
        if(r.Equals(default))
            r=Range.All;
        var (itemOffset,targetCount) = r.GetOffsetAndLength(Count);
        var hostBuffer=into is null||into.Length!=targetCount?new T[targetCount]:into;
        var res = ReadNonBlocking(queue, hostBuffer,(uint)itemOffset, waitlist);
        return !res.TryGetValue(out var ev)
            ? Task.FromResult(Result<CLResultCode, T[]>.Failure(res.Info))
            : ev.MakeAwaitable()
                .ContinueWith(a => Result<CLResultCode, T[]>.Success(hostBuffer)
                    .When(a is { Status: TaskStatus.RanToCompletion, Result: CommandExecutionStatus.Complete }));
    }


    public Result<CLResultCode, OclEvent> EnqueueWrite(OclCommandQueue queue, nuint byteOffset, Span<T> data,
        bool blocking,
        OclEvent[]? waitlist)
    {
        if (!HostCanWrite)
            return Result<CLResultCode, OclEvent>.Failure();
        var waitFor = waitlist?.As<OclEvent, CLEvent>().ToArray();

        return CL.EnqueueWriteBuffer(queue, Underlying, blocking, byteOffset, data, waitFor, out var eventHandle)
            .AsResult<OclEvent>(eventHandle);
    }

    public Result<CLResultCode> WriteBlocking(OclCommandQueue queue, Span<T> data, uint itemOffset = 0,
        OclEvent[]? waitlist = null)
        => EnqueueWrite(queue, GetByteSize((int)itemOffset), data, true, waitlist).Info;

    public Task<Result<CLResultCode>> WriteAsync(OclCommandQueue queue, Span<T> data, uint itemOffset = 0,
        OclEvent[]? waitlist = null)
    {
        var res = WriteNonBlocking(queue, data, itemOffset,waitlist);
        return !res.TryGetValue(out var ev)
            ? Task.FromResult(Result<CLResultCode>.Failure(res.Info))
            : ev.MakeAwaitable()
                .ContinueWith(a =>
                    a is { Status: TaskStatus.RanToCompletion, Result: CommandExecutionStatus.Complete }
                        ? Result<CLResultCode>.Success()
                        : Result<CLResultCode>.Failure());
        ;
    }


    public Result<CLResultCode, OclEvent> WriteNonBlocking(OclCommandQueue queue, Span<T> data, uint itemOffset = 0,
        OclEvent[]? waitlist = null) =>
        EnqueueWrite(queue, GetByteSize((int)itemOffset), data, false, waitlist);

    private static MemoryFlags HostWriteFlags() =>
        MemoryFlags.ReadWrite | MemoryFlags.HostWriteOnly | MemoryFlags.ReadOnly|MemoryFlags.WriteOnly;

    private static MemoryFlags HostReadFlags() =>
        MemoryFlags.ReadWrite | MemoryFlags.HostReadOnly | MemoryFlags.ReadOnly|MemoryFlags.WriteOnly;

    public static nuint GetByteSize(int count) => (nuint)(count * Unsafe.SizeOf<T>());
    public Result<CLResultCode> Retain() => _alive.ReadValue() ? Underlying.Retain() : Result<CLResultCode>.Failure();

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_alive) return;
        Underlying.Dispose();
    }
}