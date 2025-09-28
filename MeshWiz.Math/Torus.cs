using System.Numerics;

namespace MeshWiz.Math;

public readonly struct Torus<TNum> : IBody<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Circle3<TNum> MajorCircle=>new(Centroid,Normal,MajorRadius);
    public Circle3<TNum> MinorCircle=>new(Centroid,Normal,MinorRadius);
    public Circle3<TNum> InnerCircle=>new(Centroid,Normal,Numbers<TNum>.Half*(MajorRadius + MinorRadius));
    public readonly Vector3<TNum> Normal;
    public readonly TNum MinorRadius;
    public readonly TNum MajorRadius;
    public Vector3<TNum> Centroid { get; }

    /// <summary>
    /// Surface area = 4 * pi^2 * R * r
    /// </summary>
    public TNum SurfaceArea => Numbers<TNum>.TwoPi * Numbers<TNum>.TwoPi * MajorRadius * MinorRadius;

    /// <summary>
    /// Volume = 2 * pi^2 * R * r^2
    /// </summary>
    public TNum Volume => Numbers<TNum>.TwoPi * TNum.Pi * MajorRadius * MinorRadius * MinorRadius;

    public AABB<Vector3<TNum>> BBox
    {
        get
        {
            var plane = MajorCircle.Plane;
            var (u, v) = plane.Uv;
            var n = Normal;

            var two = TNum.CreateTruncating(2);
            var radialExtent = MajorRadius + MinorRadius; // max distance in u/v-plane from centroid
            var diag = Vector3<TNum>.Abs(u) + Vector3<TNum>.Abs(v);
            diag *= two * radialExtent;
            diag += Vector3<TNum>.Abs(n) * (two * MinorRadius);
            return AABB.Around(Centroid, diag);
        }
    }

    public Torus(Vector3<TNum> centroid,Vector3<TNum> normalUp, TNum minorRadius,TNum majorRadius)
    {
        Centroid=centroid;
        MinorRadius = TNum.Abs(minorRadius);
        MajorRadius = TNum.Abs(majorRadius);
        Normal = TNum.Sign(minorRadius) == TNum.Sign(majorRadius) ? normalUp : -normalUp;
    }

    public IMesh<TNum> Tessellate() => Tessellate(32, 16);


    public IndexedMesh<TNum> Tessellate(int radialSegments, int tubularSegments)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(radialSegments, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(tubularSegments, 3);

        var vertices = new Vector3<TNum>[radialSegments * tubularSegments];

        var plane = MajorCircle.Plane;
        var (u, v) = plane.Uv;
        var n = MajorCircle.Normal;

        var twoPi = Numbers<TNum>.TwoPi;
        var rCount = TNum.CreateTruncating(radialSegments);
        var tCount = TNum.CreateTruncating(tubularSegments);
        var iTNum=TNum.NegativeOne;
        for (var i = 0; i < radialSegments; i++)
        {
            var theta = twoPi * ++iTNum / rCount;
            var cosTheta = TNum.Cos(theta);
            var sinTheta = TNum.Sin(theta);

            // center of the tube circle for this theta
            var radialDir = u * cosTheta + v * sinTheta;
            var center = MajorCircle.Centroid + radialDir * MajorRadius;

            // local basis for the tube circle: radialDir and major normal
            var jTNum = TNum.NegativeOne;
            for (var j = 0; j < tubularSegments; j++)
            {
                var phi = twoPi * ++jTNum / tCount;
                var cosPhi = TNum.Cos(phi);
                var sinPhi = TNum.Sin(phi);

                var pos = center + (radialDir * cosPhi + n * sinPhi) * MinorRadius;
                vertices[i * tubularSegments + j] = pos;
            }
        }

        // triangles: two per quad
        var indices = new TriangleIndexer[radialSegments * tubularSegments * 2];
        var idx = 0;
        for (var i = 0; i < radialSegments; i++)
        {
            var ni = (i + 1) % radialSegments;
            for (var j = 0; j < tubularSegments; j++)
            {
                var nj = (j + 1) % tubularSegments;

                var a = i * tubularSegments + j;
                var b = ni * tubularSegments + j;
                var c = ni * tubularSegments + nj;
                var d = i * tubularSegments + nj;

                // two triangles (a,b,d) and (b,c,d)
                indices[idx++] = new TriangleIndexer(a, b, d);
                indices[idx++] = new TriangleIndexer(b, c, d);
            }
        }

        return new IndexedMesh<TNum>(vertices, indices);
    }
}