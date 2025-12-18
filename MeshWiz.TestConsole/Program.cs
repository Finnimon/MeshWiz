

using MeshWiz.RefLinq;

int[] data = [0, 0, 1, 1, 2, 2];
Console.WriteLine(string.Join(" ",data.Iterate().Skip(1).ToArray()));