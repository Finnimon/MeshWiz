using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace MeshWiz.Math;

public interface ILine<TVector, TNum>
    : IDiscreteCurve<TVector, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVector Direction => End-Start;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVector NormalDirection => Direction.Normalized();

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TNum IDiscreteCurve<TVector, TNum>.Length => Direction.Length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    bool ICurve<TVector, TNum>.IsClosed => false;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVector MidPoint { get; }
}