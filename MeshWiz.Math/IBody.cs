using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace MeshWiz.Math;

public interface IBody<TNum> : ISurface3<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonIgnore,XmlIgnore,SoapIgnore,IgnoreDataMember,Pure]
    TNum Volume { get; }
}
