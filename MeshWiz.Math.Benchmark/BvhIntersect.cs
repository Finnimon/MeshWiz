using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using CommunityToolkit.Diagnostics;
using MeshWiz.RefLinq;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math.Benchmark;

// [MemoryDiagnoser]
[Obsolete]
public class BvhIntersect
{
    private BvhMesh<float>? _bvh;
    private Triangle3<float>[]? _elements;
    private Bvh.Node<Vec3<float>, float>[]? _nodes;
    private int _depth;
    private Ray3<float> _ray;

    [GlobalSetup]
    public void Setup()
    {
        var mesh = new Sphere<float>(Vec3<float>.Zero, 10).Tessellate(512, 512);
        _bvh = BvhMesh<float>.SurfaceAreaHeuristic(mesh);
        var info = Bvh.Create.Sah<Triangle3<float>, Vec3<float>, float>(mesh);
        _elements = info.IndexShuffle!.Iterate().Select(i => mesh[i]).ToArray();
        _nodes = info.Nodes;
        _depth = info.Depth;

        _ray = new Ray3<float>(Vec3<float>.Zero, Vec3<float>.UnitX);
    }

    [Benchmark(Baseline = true)]
    public float TypedBvh()
    {
        _ = _bvh!.Intersect(_ray, out float hit);
        if(hit.IsApprox(10)) return hit;
        ThrowHelper.ThrowInvalidOperationException();
        return hit;
    }
    
    [Benchmark]
    public float GenericBvh()
    {
        float hit=0;
        var traverser=new TestTraverser(_ray, ref hit);
        _ = Bvh.Traverse<TestTraverser, Triangle3<float>, float, Vec3<float>, float>(_elements!, _nodes!, traverser, _depth);
        return hit;
    }


    private readonly ref struct TestTraverser : Bvh.ITraverser<Triangle3<float>, float, Vec3<float>, float>
    {
        public readonly Ray3<float> Ray;
        public readonly ref float Hit;
        public TestTraverser(Ray3<float> ray, ref float hitRef)
        {
            Ray = ray;
            Hit = ref hitRef;
        }

        /// <inheritdoc />
        [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DoIntersect(AABB<Vec3<float>> test) => Ray.DoIntersect(test);

        /// <inheritdoc />
        [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DoIntersect(Triangle3<float> test)
            => Ray.DoIntersect(test);

        /// <inheritdoc />
        [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersect(Triangle3<float> test, out float result)
            => Ray.Intersect(test, out result);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bvh.HitReact AcceptHit(int index, Triangle3<float> element, float hit)
        {
            Hit = hit;
            return Bvh.HitReact.BreakCompletely;
        }
    }
}