using MeshWiz.RefLinq;

namespace MeshWiz.Math.Tests;

public class BvhTest
{
    private readonly BvhMesh<float>  _bvh;
    private readonly Triangle3<float>[] _elements;
    private readonly Bvh.Node<Vec3<float>, float>[] _nodes;
    private readonly int _depth;
    
    public BvhTest()
    {
        var mesh = new Sphere<float>(Vec3<float>.Zero, 10).Tessellate(128,128);
        _bvh=BvhMesh<float>.SurfaceAreaHeuristic(mesh);
        var info = Bvh.Create.Sah<Triangle3<float>, Vec3<float>, float>(mesh);
        _elements = info.IndexShuffle!.Iterate().Select(i => mesh[i]).ToArray();
        _nodes = info.Nodes;
        _depth = info.Depth;
    }

    [Test]
    public void TestBvh()
    {
        var ray = new Ray3<float>(Vec3<float>.Zero, Vec3<float>.UnitX);
        var bvhDidIntersect=_bvh.Intersect(ray, out BvhHitInfo<float>[] hits);
        var traverser = new TestTraverser(ray);
        var genericDidHit=Bvh.Traverse<TestTraverser, Triangle3<float>, float, Vec3<float>, float>(_elements, _nodes,traverser,_depth);
        Assert.That(bvhDidIntersect,Is.EqualTo(genericDidHit));
        var orderBvhHits= hits.Select(h => h.Distance).Select(f => float.Round(f, 2)).Distinct().ToArray();
        Array.Sort(orderBvhHits);
        var orderGenericHits = traverser.Hits.SkipSpan(0).Select(f => float.Round(f, 2)).Distinct().ToArray();
        Array.Sort(orderGenericHits);
        Assert.That(orderBvhHits, Is.EqualTo(orderGenericHits));
        Console.WriteLine(string.Join(" ",orderBvhHits));
    }
    private class TestTraverser(Ray3<float> ray) 
        : Bvh.ITraverser<Triangle3<float>, float, Vec3<float>, float>
    {
        public readonly Ray3<float> Ray = ray;
        public List<float> Hits = [];

        /// <inheritdoc />
        public bool DoIntersect(AABB<Vec3<float>> test) => Ray.DoIntersect(test);

        /// <inheritdoc />
        public bool DoIntersect(Triangle3<float> test)
            => Ray.DoIntersect(test);

        /// <inheritdoc />
        public bool Intersect(Triangle3<float> test, out float result)
        =>Ray.Intersect(test,out result);

        /// <inheritdoc />
        public Bvh.HitReact AcceptHit(int index, Triangle3<float> element, float hit)
        {
            Hits.Add(hit);
            return Bvh.HitReact.ContinueCurrentNode;
        }
    }
    
}