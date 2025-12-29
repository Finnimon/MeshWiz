using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace MeshWiz.Math;

public interface ILine<TVec, TNum>
    : IContiguousDiscreteCurve<TVec, TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVec AxisVector => End-Start;
    
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVec Direction => AxisVector.Normalized();

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TNum IDiscreteCurve<TVec, TNum>.Length => AxisVector.Length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    bool ICurve<TVec, TNum>.IsClosed => false;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVec MidPoint { get; }

    /// <inheritdoc />
    TVec IContiguousCurve<TVec, TNum>.GetTangent(TNum t)
        => Direction;

    /// <inheritdoc />
    TVec IContiguousDiscreteCurve<TVec, TNum>.EntryDirection => Direction;
    TVec IContiguousDiscreteCurve<TVec, TNum>.ExitDirection => Direction;
}