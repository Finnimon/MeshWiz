using System.Collections.Concurrent;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public sealed record OclQueueManager : IDisposable
{
    public OclCommandQueue Underlying { get; }
    public static implicit operator OclCommandQueue(OclQueueManager manager) => manager.Underlying;
    public static implicit operator CLCommandQueue(OclQueueManager manager) => manager.Underlying;
    private readonly Once _alive = Bool.Once();
    public bool Alive => _alive.ReadValue();
    private readonly ConcurrentQueue<OclEvent> _waitList = [];


    public OclQueueManager(OclCommandQueue Underlying)
    {
        this.Underlying = Underlying;
    }

    public OclEvent[]? WaitList => Filter(_waitList) ? _waitList.ToArray() : null;

    private static bool Filter(ConcurrentQueue<OclEvent> queue)
    {
        if (queue.Count == 0)
            return false;
        while (queue.TryPeek(out var current))
        {
            var status = current.ExecutionStatus
                .FailWhen(s => s is CommandExecutionStatus.Error or CommandExecutionStatus.Complete);
            var isErrorOrComplete = status.IsFailure;
            if (!isErrorOrComplete)
                break;
            queue.TryDequeue(out _);
        }

        return !queue.IsEmpty;
    }

    public void Enqueue(OclEvent ev)
    {
        if (ev.ExecutionStatus.OrElse(CommandExecutionStatus.Error) is CommandExecutionStatus.Error
            or CommandExecutionStatus.Complete)
            return;
        _waitList.Enqueue(ev);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_alive) return;
        Underlying.Dispose();
        _waitList.Clear();
    }


    public CLEvent[]? ClWaitList => Filter(_waitList) 
        ? _waitList.Select<OclEvent, CLEvent>(v => v).ToArray() 
        : null;
    public Result<OclResultCode> Finish() => Alive ? Underlying.Finish() : OclResultCode.InvalidCommandQueue;
}