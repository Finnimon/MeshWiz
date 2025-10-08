namespace MeshWiz.Math;

public readonly record struct PolylineTessellationParameter<TNum>(TNum MaxAbsDeviation, TNum MaxAngularDeviation);