using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace MeshWiz.Math;

public interface ILine<TVector, TNum>
    : IContiguousDiscreteCurve<TVector, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVector AxisVector => End-Start;
    
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVector Direction => AxisVector.Normalized();

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TNum IDiscreteCurve<TVector, TNum>.Length => AxisVector.Length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    bool ICurve<TVector, TNum>.IsClosed => false;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVector MidPoint { get; }

    /// <inheritdoc />
    TVector IContiguousCurve<TVector, TNum>.GetTangent(TNum t)
        => Direction;

    /// <inheritdoc />
    TVector IContiguousDiscreteCurve<TVector, TNum>.EntryDirection => Direction;
    TVector IContiguousDiscreteCurve<TVector, TNum>.ExitDirection => Direction;
}