namespace MeshWiz.Math;

public sealed partial record RotationalSurface<TNum>
{
    public enum PeriodicalGeodesics
    {
        Success = 0,
        Failure = 1,
        BoundaryHit = 2,
        SegmentFailure = 3,
        CyclesExceeded = 4,
    }
}