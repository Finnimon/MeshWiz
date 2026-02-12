using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace MeshWiz.Buffers.UnitTests;

public class FreelistGcSafety
{
    [Test]
    public void RentReturnCollect()
    {
        var weakRef=WriteToFreelist();
        GC.Collect(GC.MaxGeneration,GCCollectionMode.Default,blocking:true);
        // using var buf = Pool.Rent<string>(1);
        Assert.That(weakRef.IsAlive,Is.False);
    }

    private static WeakReference WriteToFreelist()
    {
        using var b = Freelist.Shared.Rent<string>(1);
        {
            WriteRandomString(in b);
        }
        
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Default,blocking:true);
        var weakRef= new WeakReference(b.Span[0]);
        Assert.That(weakRef.IsAlive,Is.True);
        return weakRef;
    }

    private static void WriteRandomString(in Freelist.Buffer<string> b)
    {
        b.Span[0] = AllocationHelper.CreateRandomString(1000_000);
    }
}