using NUnit.Framework;

namespace MeshWiz.Buffers.UnitTests;

public class PoolGcSafety
{
    [Test]
    public void RentReturnCollect()
    {
        var weakRef=WriteToPoolBuf();
        GC.Collect(GC.MaxGeneration,GCCollectionMode.Default,blocking:true);
        // using var buf = Pool.Rent<string>(1);
        Assert.That(weakRef.IsAlive,Is.False);
    }

    private static WeakReference WriteToPoolBuf()
    {
        using var b = Pool.Rent<string>(1);
        
        {
            b.Span[0] = new Random().Next().ToString();
        }
        
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Default,blocking:true);
        var weakRef= new WeakReference(b.Span[0]);
        Assert.That(weakRef.IsAlive,Is.True);
        return weakRef;
    }
}