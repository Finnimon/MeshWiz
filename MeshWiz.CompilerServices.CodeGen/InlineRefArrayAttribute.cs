using System;

namespace MeshWiz.CompilerServices.CodeGen;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class InlineRefArrayAttribute(uint length) : Attribute
{
    public uint Length { get; } = length;
}