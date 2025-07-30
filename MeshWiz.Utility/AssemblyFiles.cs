using System.Reflection;

namespace MeshWiz.Utility;

public static class AssemblyFiles<T>
{
    static AssemblyFiles()
    {
        var type=typeof(T);
        Assembly= type.Assembly;
        AssemblyLocation=Assembly.Location;
        AssemblyName=Assembly.GetName().Name??string.Empty;
        AssemblyDirectory=Path.GetDirectoryName(AssemblyLocation)??string.Empty;;
    }

    public static readonly Assembly Assembly;
    public static readonly string AssemblyDirectory;
    public static readonly string AssemblyName;
    public static readonly string AssemblyLocation;
    public static string RebaseRelativeFile(string file)=>Path.Combine(AssemblyDirectory,AssemblyName,file);
}