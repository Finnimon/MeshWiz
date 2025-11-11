
using MeshWiz.Utility;

var @true=Once.True;
var @false=Once.False;
for (var i = 0; i < 10; i++)
{
    
    Console.WriteLine($"True {(bool)@true}");
    Console.WriteLine($"False {(bool)@false}");
}