using System.Numerics;

namespace MeshWiz.Math;

public static partial class Mesh
{
    public static class Heal
    {
        public static IMesh<TNum> HealHoles<TNum>(IMesh<TNum> mesh)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var split = Mesh.Indexing.Split(mesh.Indexed());
            //ignore other islands
            mesh = split.OrderByDescending(m => m.Count).First();
            List<int> counts = new((int)(mesh.Count * 1.5));
            foreach (var triangle3 in mesh)
            {
                
            }

            throw new NotImplementedException();
        }
    }
}