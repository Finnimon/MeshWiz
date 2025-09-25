using System.Diagnostics.Contracts;

namespace MeshWiz.Contracts;

public  interface IAbstraction<TSelf,TLowLevel>
where TSelf:IAbstraction<TSelf, TLowLevel>
{
    [Pure]
    public static abstract implicit operator TSelf(TLowLevel lowLevel);
    [Pure]
    public static abstract implicit operator TLowLevel(TSelf highLevel);
    [Pure]
    public static abstract TSelf Abstract(TLowLevel lowLevel);
    [Pure]
    public static abstract TLowLevel LowLevel(TSelf highLevel);
}