using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Collections;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public interface IPolyline<out TSelf, TLine, TVert, TVector, TNum>
    : IContiguousDiscreteCurve<TVector, TNum>,
        IVersionedList<TLine>,
        IBounded<TVector> 
    where TSelf : IPolyline<TSelf, TLine, TVert, TVector, TNum>
    where TLine : unmanaged, ILine<TVector, TNum>
    where TVert : ILerp<TVert, TNum>, IPosition<TVert, TVector, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    IReadOnlyList<TVert> Vertices { get; }
    IReadOnlyList<TNum> CumulativeLengths { get; }
    
    /// <inheritdoc />
    void ICollection<TLine>.Add(TLine item)
        => ThrowHelper.ThrowNotSupportedException();

    void ICollection<TLine>.Clear()
        => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    bool ICollection<TLine>.Remove(TLine item)
        => ThrowHelper.ThrowNotSupportedException<bool>();

    /// <inheritdoc />
    bool ICollection<TLine>.IsReadOnly => true;

    /// <inheritdoc />
    void IList<TLine>.RemoveAt(int index)
        => ThrowHelper.ThrowNotSupportedException<bool>();

    static virtual TSelf Create(params ReadOnlySpan<TVert> vertices) => TSelf.CreateNonCopying(vertices.ToArray());
    static abstract TSelf CreateNonCopying(TVert[] vertices);
    static virtual TSelf Create(IEnumerable<TVert> verts) => TSelf.CreateNonCopying(verts.ToArray());

    static virtual TSelf CreateCulled(IEnumerable<TVert> source)
        => TSelf.CreateNonCopying(Polyline.Cull<TVert, TNum>(source));

    /// <inheritdoc />
    void IList<TLine>.Insert(int index, TLine item)
        => ThrowHelper.ThrowNotSupportedException();
    TLine IList<TLine>.this[int index]
    {
        get => throw new NotImplementedException();
        set => ThrowHelper.ThrowNotSupportedException();
    }
}