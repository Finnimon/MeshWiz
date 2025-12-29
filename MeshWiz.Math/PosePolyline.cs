using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed class PosePolyline<TPose, TVec, TNum> : IDiscretePoseCurve<TPose,TVec, TNum>,
    IReadOnlyList<PoseLine<TPose, TVec, TNum>>,
    IBounded<TVec> 
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TPose : IPose<TPose, TVec, TNum>
{
    private readonly TPose[] _poses;
    public PosePolyline(IEnumerable<TPose> poses) => _poses = poses.ToArray();
    public PosePolyline(params ReadOnlySpan<TPose> poses) => _poses = poses.ToArray();
    private PosePolyline(TPose[] poses) => _poses = poses;
    internal static PosePolyline<TPose, TVec, TNum> CreateNonCopying(TPose[] poses) => new(poses);
    public static PosePolyline<TPose, TVec, TNum> CreateCulled(params ReadOnlySpan<TPose> poses)
    {
        if (poses.Length is 0 or 1)
            return new PosePolyline<TPose, TVec, TNum>();
        return CreateCulledNonCopying(poses.ToArray());
    }

    public static PosePolyline<TPose, TVec, TNum> CreateCulledNonCopying(TPose[] poses)
    {
        if (poses.Length is 0 or 1)
            return new PosePolyline<TPose, TVec, TNum>();
        var vertCount = 0;
        var verts = poses;
        for (var i = 0; i < verts.Length; ++i)
        {
            if (i == 0)
            {
                vertCount++;
                continue;
            }

            ref var previous = ref verts[vertCount - 1];
            var current = verts[i];
            var dist = previous.DistanceTo(current);
            var cull = dist.IsApproxZero();
            if (cull)
            {
                //lerp for similar orientation across two same posit poses
                previous = TPose.Lerp(previous, current, Numbers<TNum>.Half);
                continue;
            }

            //avoid reassign if no previous cascading change
            vertCount++;
            var noChange = i == vertCount;
            if (noChange) continue;
            verts[vertCount - 1] = current;
        }

        return new PosePolyline<TPose, TVec, TNum>(verts);
    }

    public static PosePolyline<TPose, TVec, TNum> CreateCulled(IEnumerable<TPose> poses) => CreateCulledNonCopying(poses.ToArray());

    public ReadOnlySpan<TPose> Poses => _poses;
    public int Count => int.Max(0, _poses.Length - 1);

    [field: AllowNull, MaybeNull]
    // ReSharper disable once InconsistentNaming
    private TNum[] _cumulativeDistances => field ??= Polyline.CalculateCumulativeDistances<TPose, TNum>(Poses);

    public ReadOnlySpan<TNum> CumulativeDistances => _cumulativeDistances;

    /// <inheritdoc />
    TVec IDiscreteCurve<TVec, TNum>.Start => this._poses[0].Position;

    /// <inheritdoc />
    TVec IDiscreteCurve<TVec, TNum>.End => this._poses[^1].Position;

    /// <inheritdoc />
    TVec IDiscreteCurve<TVec, TNum>.TraverseOnCurve(TNum t)
        => this.TraverseOnCurve(t).Position;

    public TPose StartPose => _poses[0];
    public TPose EndPose => _poses[^1];
    public TPose TraverseOnCurve(TNum t)
        => Polyline.TraverseOnCurve(t, _cumulativeDistances, IsClosed, _poses);

    public TNum Length => _poses.Length > 1 ? _cumulativeDistances[^1] : TNum.Zero;

    /// <inheritdoc />
    public Polyline<TVec, TNum> ToPolyline()
    {
        var poses = Poses;
        var pts = new TVec[poses.Length];
        for (var i = 0; i < poses.Length; i++)
            pts[i] = _poses[i].Position;
        return new Polyline<TVec, TNum>(pts);
    }

    /// <inheritdoc />
    Polyline<TVec, TNum> IDiscreteCurve<TVec, TNum>.ToPolyline(
        PolylineTessellationParameter<TNum> tessellationParameter)
        => new Polyline<TVec, TNum>(_poses.Select(p => p.Position)).CullDeadSegments();

    public TPose GetPose(TNum t)
        => Polyline.Traverse(t, _cumulativeDistances, IsClosed, _poses);

    /// <inheritdoc />
    public TVec Traverse(TNum t) => GetPose(t).Position;

    public bool IsClosed
        => Count > 2 && _poses[0].Position.IsApprox(_poses[^1].Position);

    /// <inheritdoc />
    public TVec GetTangent(TNum t)
        => GetPose(t).Front;

    /// <inheritdoc />
    public TVec EntryDirection => Count < 0 ? TVec.NaN : _poses[0].Front;

    /// <inheritdoc />
    public TVec ExitDirection => Count < 0 ? TVec.NaN : _poses[^1].Front;

    /// <inheritdoc />
    public PosePolyline<TPose, TVec, TNum> ToPosePolyline() => this;

    /// <inheritdoc />
    public PosePolyline<TPose, TVec, TNum> ToPosePolyline(PolylineTessellationParameter<TNum> tessellationParameter) => this;

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<PoseLine<TPose, TVec, TNum>> GetEnumerator()
        => Enumerable.Range(0, Count).Select(i => this[i]).GetEnumerator();

    public PoseLine<TPose, TVec, TNum> this[int i]
        => Count > (uint)i
            ? Unsafe.As<TPose, PoseLine<TPose, TVec, TNum>>(ref _poses[i])
            : IndexThrowHelper.Throw<PoseLine<TPose, TVec, TNum>>();

    public PosePolyline<TPose, TVec, TNum> Section(TNum start, TNum end)
        => ExactSection(start * Length, end * Length);

    public PosePolyline<TPose, TVec, TNum> ExactSection(TNum start, TNum end)
    {
        var section = Polyline.ExactSection(start, end,_poses,IsClosed,_cumulativeDistances);
        return new PosePolyline<TPose, TVec, TNum>(section);
    }

    private AABB<TVec>? _bbox;
    /// <inheritdoc />
    public AABB<TVec> BBox => _bbox ??= AABB.From<TPose,TVec,TNum>(Poses);

}