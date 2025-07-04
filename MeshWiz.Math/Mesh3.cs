using System.Numerics;

namespace MeshWiz.Math;

public sealed record Mesh3<TNum>(Triangle3<TNum>[] TessellatedSurface)
    : IBody<TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public Vector3<TNum> Centroid => VolumeCentroid;
    public Vector3<TNum> VertexCentroid => _vertexCentroid ??= CalculateVertexCentroid(TessellatedSurface);
    public Vector3<TNum> SurfaceCentroid => _surfaceCentroid ??= CalculateAreaCentroid(TessellatedSurface);
    public Vector3<TNum> VolumeCentroid => _volumeCentroid ??= CalculateVolumeCentroid(TessellatedSurface);
    public TNum Volume => _volume ??= CalculateVolume(TessellatedSurface);
    public TNum SurfaceArea => _surfaceArea ??= CalculateSurfaceArea(TessellatedSurface);
    public IFace<Vector3<TNum>, TNum>[] Surface => _surface ??= [..TessellatedSurface];
    public BBox3<TNum> BBox =>_bBox??=GetBBox(TessellatedSurface);

    private IFace<Vector3<TNum>, TNum>[]? _surface;
    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vector3<TNum>? _vertexCentroid;
    private Vector3<TNum>? _surfaceCentroid;
    private Vector3<TNum>? _volumeCentroid;
    private BBox3<TNum>? _bBox;

    public static Mesh3<TNum> Initialized(Triangle3<TNum>[] tessellatedSurface)
    {
        var mesh=new Mesh3<TNum>(tessellatedSurface);
        mesh.InitializeLazies();
        return mesh;
    }

    public static async Task<Mesh3<TNum>> InitializedAsync(Triangle3<TNum>[] tessellatedSurface) 
        => await Task.Run(() => Initialized(tessellatedSurface));
    
    public void InitializeLazies()
    {
        var vertexCentroid = Vector3<TNum>.Zero;
        var surfaceCentroid = Vector3<TNum>.Zero;
        var volumeCentroid = Vector3<TNum>.Zero;
        var surfaceArea = TNum.Zero;
        var volume = TNum.Zero;
        var box = BBox3<TNum>.NegativeInfinity;
        
        for (var i = 0; i < TessellatedSurface.Length; i++)
        {
            ref var triangle = ref TessellatedSurface[i];
            Tetrahedron<TNum> tetra = new(in triangle);
            var currentCentroid = triangle.Centroid;
            var currentSurf = triangle.SurfaceArea;
            var currentVolu = tetra.Volume;
            vertexCentroid += currentCentroid;
            surfaceCentroid += currentCentroid * currentSurf;
            volumeCentroid += tetra.Centroid * currentVolu;
            volume += tetra.Volume;
            surfaceArea += currentSurf;
            box.CombineWith(triangle.A).CombineWith(triangle.B).CombineWith(triangle.C);
        }

        _vertexCentroid = vertexCentroid / TNum.CreateTruncating(TessellatedSurface.Length);
        _surfaceCentroid = surfaceCentroid / surfaceArea;
        _volumeCentroid = volumeCentroid / volume;
        _surfaceArea = surfaceArea;
        _volume = volume;
        _bBox = box;
    }

    public async Task InitializeAsync() => await Task.Run(InitializeLazies);

    public static Vector3<TNum> CalculateVertexCentroid(Triangle3<TNum>[] mesh)
    {
        var centroid = Vector3<TNum>.Zero;
        for (var i = 0; i < mesh.Length; i++)
        {
            ref var triangle = ref mesh[i];
            centroid += triangle.A + triangle.B + triangle.C;
        }

        return centroid / TNum.CreateTruncating(mesh.Length * 3);
    }

    public static Vector3<TNum> CalculateAreaCentroid(Triangle3<TNum>[] mesh)
    {
        var centroid = Vector3<TNum>.Zero;
        var area = TNum.Zero;
        for (var i = 0; i < mesh.Length; i++)
        {
            ref var triangle = ref mesh[i];
            var currentCentroid = triangle.A + triangle.B + triangle.C;
            var currentArea = triangle.SurfaceArea;
            centroid += currentCentroid * currentArea;
            area += currentArea;
        }

        return centroid / area / TNum.CreateTruncating(3);
    }


    public static Vector3<TNum> CalculateVolumeCentroid(Triangle3<TNum>[] mesh)
    {
        var centroid = Vector3<TNum>.Zero;
        var volume = TNum.Zero;
        for (var i = 0; i < mesh.Length; i++)
        {
            Tetrahedron<TNum> tetra = new(in mesh[i]);
            var currentVolume = tetra.Volume;
            var currentCentroid = tetra.Centroid;
            centroid += currentCentroid * currentVolume;
            volume += currentVolume;
        }

        return centroid / volume;
    }


    public static TNum CalculateVolume(Triangle3<TNum>[] mesh)
    {
        var volume = TNum.Zero;
        for (var i = 0; i < mesh.Length; i++) volume += new Tetrahedron<TNum>(in mesh[i]).Volume;
        return volume;
    }


    public static TNum CalculateSurfaceArea(Triangle3<TNum>[] mesh)
    {
        var area = TNum.Zero;
        for (var i = 0; i < mesh.Length; i++) area += mesh[i].SurfaceArea;
        return area;
    }

    public static BBox3<TNum> GetBBox(Triangle3<TNum>[] mesh)
    {
        var bbox = BBox3<TNum>.NegativeInfinity;
        for (var i = 1; i < mesh.Length; i++)
        {
            ref var triangle = ref mesh[i];
            bbox=bbox.CombineWith(triangle.A).CombineWith(triangle.B).CombineWith(triangle.C);
        }
        return bbox;
    }
}