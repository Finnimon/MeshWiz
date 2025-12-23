namespace MeshWiz.Utility.Extensions;

public static class RangeExt
{
    public static bool IsAll(this Range r) => r is {Start:{Value:0,IsFromEnd:false},End:{Value:1,IsFromEnd:true}};
}