// See https://aka.ms/new-console-template for more information

using MeshWiz.Math;
using MeshWiz.Slicer;
using MeshWiz.TestConsole;

Console.WriteLine("Hello, World!");

var mode= TcpOptions.Additive|TcpOptions.Subtractive;
var explicitMode=mode&TcpOptions.MovementModeMask;
Console.WriteLine(explicitMode);
mode= TcpOptions.Subtractive;
explicitMode=mode&TcpOptions.MovementModeMask;
Console.WriteLine(explicitMode);
mode= TcpOptions.StepOver|TcpOptions.Subtractive;
 explicitMode=mode&TcpOptions.MovementModeMask;
Console.WriteLine(explicitMode);

Console.WriteLine(float.Sin(0));
Console.WriteLine(float.Sin(float.Pi/2));
Console.WriteLine(float.Cos(0));

Console.WriteLine(float.Cos(float.Pi));