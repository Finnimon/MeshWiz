using System.Numerics;

namespace MeshWiz.Math;

public static partial class Mesh
{

    public static class Math
    {
        public record Mesh3Info<TNum>(
            Vec3<TNum> VertexCentroid,
            Vec3<TNum> SurfaceCentroid,
            Vec3<TNum> VolumeCentroid,
            TNum SurfaceArea,
            TNum Volume,
            AABB<Vec3<TNum>> Box)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>;

        public static Mesh3Info<TNum> AllInfo<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var vertexCentroid = Vec3<TNum>.Zero;
            var surfaceCentroid = Vec3<TNum>.Zero;
            var volumeCentroid = Vec3<TNum>.Zero;
            var surfaceArea = TNum.Zero;
            var volume = TNum.Zero;
            var box = AABB<Vec3<TNum>>.Empty;

            foreach (var triangle in mesh)
            {
                var (a, b, c) = triangle;
                var currentCentroid = a + b + c;
                var currentSurf = triangle.SurfaceArea;
                var currentVolume = Tetrahedron<TNum>.CalculateSignedVolume(a, b, c, Vec3<TNum>.Zero);
                vertexCentroid += currentCentroid;
                surfaceCentroid += currentCentroid * currentSurf;
                volumeCentroid += currentCentroid * currentVolume;
                volume += currentVolume;
                surfaceArea += currentSurf;
                box = box.CombineWith(triangle.A,triangle.B,triangle.C);
            }

            vertexCentroid /= TNum.CreateTruncating(mesh.Count * 3);
            surfaceCentroid /= surfaceArea * TNum.CreateTruncating(3);
            volumeCentroid /= volume * TNum.CreateTruncating(4);
            volume = TNum.Abs(volume);

            return new Mesh3Info<TNum>(
                vertexCentroid,
                surfaceCentroid,
                volumeCentroid,
                surfaceArea,
                volume,
                box
            );
        }

        public static Vec3<TNum> VertexCentroid<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var centroid = Vec3<TNum>.Zero;
            foreach (var tri in mesh) centroid += tri.A + tri.B + tri.C;

            return centroid / TNum.CreateTruncating(mesh.Count * 3);
        }

        public static Vec4<TNum> SurfaceCentroid<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var centroid = Vec4<TNum>.Zero;
            foreach (var triangle in mesh)
            {
                var currentCentroid = triangle.A + triangle.B + triangle.C;
                var currentArea = triangle.SurfaceArea;
                centroid += new Vec4<TNum>(currentCentroid * currentArea, currentArea);
            }

            return new Vec4<TNum>(
                centroid.XYZ / centroid.W / TNum.CreateTruncating(3),
                TNum.Abs(centroid.W));
        }

        public static Vec4<TNum> VolumeCentroid<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var centroid = Vec4<TNum>.Zero;
            foreach (var t in mesh)
            {
                Tetrahedron<TNum> tetra = new(t);
                var currentVolume = tetra.Volume;
                var currentCentroid = tetra.Centroid;
                centroid += new Vec4<TNum>(currentCentroid * currentVolume, currentVolume);
            }

            return new Vec4<TNum>(
                centroid.XYZ / centroid.W,
                TNum.Abs(centroid.W));
        }

        public static TNum Volume<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var volume = TNum.Zero;
            foreach (var tri in mesh)
                volume += new Tetrahedron<TNum>(tri).Volume;

            return volume;
        }


        public static TNum SurfaceArea<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var area = TNum.Zero;
            foreach (var tri in mesh) area += tri.SurfaceArea;
            return area;
        }

        public static AABB<Vec3<TNum>> BBox<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var bbox = AABB<Vec3<TNum>>.Empty;
            foreach (var tri in mesh)
                bbox = bbox.CombineWith(tri.A,tri.B,tri.C);

            return bbox;
        }
    }
}
