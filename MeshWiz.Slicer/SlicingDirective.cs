namespace MeshWiz.Slicer;

public sealed record SlicingDirective<TNum>(TNum LayerHeight, TNum PathWidth);