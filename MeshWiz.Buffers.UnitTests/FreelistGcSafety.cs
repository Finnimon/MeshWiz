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
            b.Span[0] = new Random().Next().ToString();
        }
        
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Default,blocking:true);
        var weakRef= new WeakReference(b.Span[0]);
        Assert.That(weakRef.IsAlive,Is.True);
        return weakRef;
    }
}