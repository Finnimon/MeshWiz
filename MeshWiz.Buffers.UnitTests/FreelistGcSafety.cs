using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;

namespace MeshWiz.Buffers.UnitTests;

public class FreelistGcSafety
{
    private readonly Freelist _freelist = new(1024, true);

    [TestCaseSource(typeof(EnumerableTestCases), nameof(EnumerableTestCases.Cases))]
    public void TestOriginal<T>(IEnumerable<T> data)
    {
        var count = data.Count();
        using var buf = Freelist.Shared.Rent<T>(count);
        var i = -1;
        foreach (var item in data)
        {
            buf.Span[++i] = item;
        }

        Thread.Sleep(100);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
        Assert.That(buf.Span[..(i + 1)].ToArray(), Is.EquivalentTo(data));
    }

    [Test]
    public void TestOnStructWithObj()
    {
        WeakReference wr;
        using (var buf = Freelist.Shared.Rent<StructWithObjectProp>(1))
        {
            NewMethod(buf);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
            GC.Collect();
            wr = WeakReference(buf);
            Assert.That(wr.IsAlive, Is.True);
        }

        var spaceKeeper = AllocationHelper.CreateRandomString(10000000);
        Thread.Sleep(1000);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
        GC.Collect();
        using var buf2 = Freelist.Shared.Rent<StructWithObjectProp>(1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(buf2.Span[0].RefType, Is.Null);
            Assert.That(wr.IsAlive, Is.False, "object not collected correctly");
        }
    }
    [Test]
    public void TestOnObj()
    {
        WeakReference wr;
        using (var buf = Freelist.Shared.Rent<string>(1))
        {
            {
                NewMethod1(buf);
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
            GC.Collect();
            wr = Wr(buf);
            Assert.That(wr.IsAlive, Is.True);
        }

        var spaceKeeper = AllocationHelper.CreateRandomString(10000000);
        Thread.Sleep(1000);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
        GC.Collect();
        using var buf2 = Freelist.Shared.Rent<StructWithObjectProp>(1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(buf2.Span[0].RefType, Is.Null);
            Assert.That(wr.IsAlive, Is.False, "object not collected correctly");
        }
    }

    private static WeakReference Wr(Freelist.Buffer<string> buf)
    {
        WeakReference wr;
        wr = new WeakReference(buf.Span[0]);
        return wr;
    }

    private static void NewMethod1(Freelist.Buffer<string> buf)
    {
        buf.Span[0] = AllocationHelper.CreateRandomString(10000000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference WeakReference(Freelist.Buffer<StructWithObjectProp> buf)
    {
        WeakReference wr = new(buf.Span[0].RefType);
        return wr;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NewMethod(Freelist.Buffer<StructWithObjectProp> buf)
    {
        StructWithObjectProp t = new(AllocationHelper.CreateRandomString(1000000));
        buf.Span[0] = t;
    }

    private readonly record struct StructWithObjectProp(string RefType);
}