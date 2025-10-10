using System.Numerics;
using System.Runtime.Intrinsics;
using MeshWiz.Math;
using OpenTK.Compute.OpenCL;

// Console.WriteLine("Hello World!");
// Console.WriteLine(Vector128.IsHardwareAccelerated);
// var circle=new Circle3<float>(Vector3<float>.Zero, Vector3<float>.UnitY, 1);
// var arcSection= circle.ArcSection(0, 3.14f);
// Console.WriteLine(arcSection.ToPolyline());


BigInteger n1=1;
BigInteger n2=1;
var pos = -1;
var nthFib = 2;
var run = true;
BigInteger prev = 1;
Task.Run(async () =>
{
    await Task.Delay(1000);
    run = false;
    await Task.Delay(10);
});
while (run)
{
    pos = (pos + 1) % 2;
    switch ( pos)
    {
        case 0:
            n1 += n2;
            break;
        case 1:
            n2 += n1;
            break;
    }
    
    nthFib++;
}

var fib = pos switch
{
    0 => n1,
    1 => n2,
    _ => throw new ArgumentOutOfRangeException()
};

Console.WriteLine($"{nthFib}: {fib}");
