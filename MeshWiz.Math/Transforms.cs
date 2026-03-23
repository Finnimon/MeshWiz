using System.Numerics;

namespace MeshWiz.Math;

public static class Transforms<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly record struct FTransform<TVec>(Func<TVec, TVec> Point, Func<TVec, TVec> Direction, bool IsAffine)
        : ISpatialTransform<TVec>
        where TVec : unmanaged, IVec<TVec, TNum>
    {
        /// <inheritdoc />
        public TVec TransformPoint(TVec p)
            => Point(p);

        /// <inheritdoc />
        public TVec TransformDirection(TVec v)
            => Direction(v);
    }

    public readonly record struct TranslationData<TVec>(TVec Vector)
        : ISpatialTransform<TVec>
        where TVec : unmanaged, IVec<TVec, TNum>
    {
        /// <inheritdoc />
        public TVec TransformPoint(TVec p) => p + Vector;

        /// <inheritdoc />
        public TVec TransformDirection(TVec v) => v + Vector;

        /// <inheritdoc />
        public bool IsAffine => true;
    }

    public static TranslationData<TVec> Translation<TVec>(TVec v) where TVec : unmanaged, IVec<TVec, TNum> => new(v);

    public static FTransform<TVec> Create<TVec>(Func<TVec, TVec> point, Func<TVec, TVec> direction,
        bool isAffine = false) where TVec : unmanaged, IVec<TVec, TNum> => new(point,direction,isAffine);
}