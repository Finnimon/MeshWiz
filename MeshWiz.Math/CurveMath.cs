using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static class CurveMath
{
    public static PolyLine<TVec, TNum>[] Unify<TVec, TNum>(IEnumerable<Line<TVec, TNum>> segments, TNum? squareTolerance=null) 
        where TNum : unmanaged, IFloatingPointIeee754<TNum> 
        where TVec : unmanaged, IFloatingVector<TVec, TNum> 
        =>Unify(new Queue<Line<TVec,TNum>>(segments),squareTolerance);
    public static PolyLine<TVec, TNum>[] Unify<TVec, TNum>(Queue<Line<TVec, TNum>> segments, TNum? squareTolerance=null) 
        where TNum : unmanaged, IFloatingPointIeee754<TNum> 
        where TVec : unmanaged, IFloatingVector<TVec, TNum>
    {
        var epsilon=squareTolerance?? TNum.CreateTruncating(0.000001);
        if (segments is { Count: 0 }) return [];
        List<PolyLine<TVec, TNum>> polyLines = [];
        LinkedList<Line<TVec, TNum>> connected = [];
        connected.AddLast(segments.Dequeue());
        var checkedSinceLastAdd = 0;
        while (segments.TryDequeue(out var line))
        {
            if (checkedSinceLastAdd > segments.Count)
            {
                polyLines.Add(PolyLine<TVec, TNum>.FromSegments(connected));
                connected = [];
                connected.AddLast(line);
                checkedSinceLastAdd = 0;
                continue;
            }
            
            var currentStart= connected.First!.Value.Start;
            var currentEnd = connected.Last!.Value.End;
            var checkedPrev=checkedSinceLastAdd;
            checkedSinceLastAdd = 0;
            if (currentStart.IsApprox(line.End,epsilon)) { connected.AddFirst(line); }
            else if (currentEnd.IsApprox(line.Start,epsilon)){ connected.AddLast(line); }
            else if(currentStart.IsApprox(line.Start,epsilon)) {connected.AddFirst(line.Reversed);  }
            else if (currentEnd.IsApprox(line.End,epsilon)) {connected.AddLast(line.Reversed);  }
            else {segments.Enqueue(line); checkedSinceLastAdd = checkedPrev+1; }
        }
        if (connected.Count>0)
        {
            polyLines.Add(PolyLine<TVec,TNum>.FromSegments(connected));
        }
        return polyLines.ToArray();
    }
    
    public static PolyLine<TVec, TNum>[] UnifyNonReversing<TVec, TNum>(RollingList<Line<TVec, TNum>> segments, TNum? squareTolerance=null) 
        where TNum : unmanaged, IFloatingPointIeee754<TNum> 
        where TVec : unmanaged, IFloatingVector<TVec, TNum>
    {
        var epsilon=squareTolerance?? TNum.CreateTruncating(0.000001);
        if (segments is { Count: 0 }) return [];
        if (segments is { Count: 1 }) return [new([segments[0].Start,segments[0].End])];
        
        List<PolyLine<TVec, TNum>> polyLines = [];
        RollingList<Line<TVec, TNum>> connected = [segments.PopBack()];
        
        var checkedSinceLastAdd = 0;
        while (segments.TryPopBack(out var line))
        {
            if (checkedSinceLastAdd > segments.Count)
            {
                polyLines.Add(PolyLine<TVec, TNum>.FromSegments(connected));
                connected = [];
                connected.PushBack(line);
                checkedSinceLastAdd = 0;
                continue;
            }
            
            var connectedStart= connected[0].Start;
            var connectedEnd = connected[^1].End;
            var checkedPrev=checkedSinceLastAdd;
            checkedSinceLastAdd = 0;
            if (connectedStart.IsApprox(line.End,epsilon)) { connected.PushFront(line); }
            else if (connectedEnd.IsApprox(line.Start,epsilon)){ connected.PushBack(line); }
            else {segments.PushFront(line); checkedSinceLastAdd = checkedPrev+1; }
        }
        if (connected.Count>0) polyLines.Add(PolyLine<TVec,TNum>.FromSegments(connected));
        return polyLines.ToArray();
    }

    public static PolyLine<TVec, TNum>[] UnifyNoReversal<TVec, TNum>(IReadOnlyList<Line<TVec, TNum>> segments,
        TNum? squareTolerance = null)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TVec : unmanaged, IFloatingVector<TVec, TNum>
    {
        throw new NotImplementedException();
    }

    
    

}