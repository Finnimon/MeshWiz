using MeshWiz.Utility;

var once = Bool.Once();
Console.WriteLine((bool)once);
Console.WriteLine((bool)once);
once = Bool.Once();
Console.WriteLine((bool)once);
Console.WriteLine((bool)once);