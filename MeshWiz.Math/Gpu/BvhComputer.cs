using OpenTK.Compute.OpenCL;
namespace MeshWiz.Math.Gpu;

public sealed class BvhComputer
{
    private readonly IIndexedMesh<float> _mesh;
    public BvhComputer(IIndexedMesh<float> mesh)
    {
        _mesh = mesh;
    }

    public static void Initialize()
    {
        var result= CL.GetPlatformIds(out var platformIds);
    }
    
    
}