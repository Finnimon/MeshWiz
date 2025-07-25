namespace MeshWiz.Utility.Tests;

public class RollingListTest
{
    [Test]
    public void TestEquivalenceToLinkedList()
    {
        
        LinkedList<int> linkedList = [];
        RollingList<int> rolyPoly=[];
        var rand = new Random();
        for (var i = 0; i < 100000; i++)
        {
            var front = rand.Next();
            var back = rand.Next();
            linkedList.AddFirst(front);
            linkedList.AddLast(back);
            rolyPoly.PushFront(front);
            rolyPoly.PushBack(back);
        }
        Assert.That(rolyPoly, Is.EquivalentTo(linkedList));
    }
    [Test]
    public void TestToArrayFast()
    {
        LinkedList<int> linkedList = [];
        RollingList<int> rolyPoly=[];
        var rand = new Random();
        for (var i = 0; i < 100000; i++)
        {
            var front = rand.Next();
            var back = rand.Next();
            linkedList.AddFirst(front);
            linkedList.AddLast(back);
            rolyPoly.PushFront(front);
            rolyPoly.PushBack(back);
        }
        Assert.That(rolyPoly.ToArrayFast(), Is.EquivalentTo(linkedList.ToArray()));
        Assert.That(rolyPoly.ToArrayFast(), Is.EquivalentTo(rolyPoly.ToArray()));
    }
}