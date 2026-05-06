using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using CommunityToolkit.Diagnostics;
using MeshWiz.Math;
using MeshWiz.RefLinq;
using MeshWiz.UpToDate;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Kinematics;

public sealed class KinematicChain : IReadOnlyList<IAxis>
{
    private readonly IAxis[] _nodes;
    public ReadOnlySpan<IAxis> Nodes => _nodes;
    public KinematicChain(IEnumerable<IAxis> nodes) => _nodes = nodes.Iterate().ToArray();
    public KinematicChain(ReadOnlySpan<IAxis> nodes) => _nodes = nodes.ToArray();

    /// <inheritdoc />
    public IEnumerator<IAxis> GetEnumerator() => ((IEnumerable<IAxis>)_nodes).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

    /// <inheritdoc />
    public int Count => _nodes.Length;

    /// <inheritdoc />
    public IAxis this[int index] => _nodes[index];
}

public sealed class KinematicChainState : IUpToDate
{
    public KinematicChain Chain { get; }
    private readonly double[] _state;
    public ReadOnlySpan<double> State => _state;
    private readonly Pose3<double>[] _relativeConnectors;
    public ReadOnlySpan<Pose3<double>> RelativeConnectors => _relativeConnectors;
    private readonly Pose3<double>[] _absoluteConnectors;
    public ReadOnlySpan<Pose3<double>> AbsoluteConnectors => _absoluteConnectors;

    public KinematicChainState(KinematicChain chain)
    {
        Chain = chain;
        _state = chain.Nodes.Select(n => n.Range.Center).ToArray();
        _relativeConnectors = Iterator.Range(0, _state.Length).Select(i => Chain[i].Reach(_state[i])).ToArray();
        _absoluteConnectors = new Pose3<double>[_relativeConnectors.Length];
        if (_absoluteConnectors.Length == 0) return; 
        _absoluteConnectors[0] = _relativeConnectors[0];
        CalculateAbsoluteConnectors(_relativeConnectors, _absoluteConnectors);
    }

    public Result<Kinematics> UpdateState(int pos, double value)
    {
        if (Chain.Count <= (uint)pos) IndexThrowHelper.Throw();
        var res= UpdateRelativeStateOnly(pos, value);
        if (res is Kinematics.CouldNotReach || _upToDate) return res;
        CalculateAbsoluteConnectors(_relativeConnectors.AsSpan(pos), _absoluteConnectors.AsSpan(pos));
        return Kinematics.Success;
    }

    private Kinematics UpdateRelativeStateOnly(int pos, double value)
    {
        if (_state[pos].Equals(value))
            return Kinematics.Success;
        if (!Chain[pos].TryReach(value, out var rel)) return Kinematics.CouldNotReach;
        OutOfDate();
        _relativeConnectors[pos] = rel;
        _state[pos] = value;
        return Kinematics.Success;
    }

    public Result<Kinematics> UpdateState(int startIndex, ReadOnlySpan<double> values)
    {
        if (startIndex < 0 || startIndex >= _state.Length)
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(startIndex));
        if (startIndex + values.Length > _state.Length) 
            ThrowHelper.ThrowArgumentException(nameof(values));
        
        for (var i = 0; i < values.Length; i++)
        {
            var pos = i + startIndex;
            var res= UpdateRelativeStateOnly(pos, values[pos]);
            if(res is Kinematics.Success) continue;
            return res;
        }

        if (_upToDate) return Kinematics.Success;
        CalculateAbsoluteConnectors(_relativeConnectors, _absoluteConnectors);
        return Kinematics.Success;
    }

    private static void CalculateAbsoluteConnectors(ReadOnlySpan<Pose3<double>> relativeConnectors,
        Span<Pose3<double>> outPutBuffer)
    {
        //todo handle pos0
        if(relativeConnectors.Length==0) return;
        outPutBuffer[0] = relativeConnectors[0];
        for (var i = 1; i < relativeConnectors.Length; i++)
        {
            var prev = outPutBuffer[i - 1];
            var cur = relativeConnectors[i];
            outPutBuffer[i] = prev.ToWorld(cur);
        }
    }

    public static KinematicChainState Empty() => new(new KinematicChain(ReadOnlySpan<IAxis>.Empty));
    private bool _upToDate = true;

    /// <inheritdoc />
    public void OutOfDate() => _upToDate = false;

    /// <inheritdoc />
    public bool ConsumeOutOfDate() => _upToDate.DeferredSet(true);
}

public enum Kinematics
{
    Success = 0,
    Faulty = 1,
    CouldNotReach = 2,
}