using System.Numerics;
using System.Reflection;
using MeshWiz.OpenCL;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.Math.OpenCL;

public static partial class Programs
{
    public static class AABB
    {
        public record ProgramContainer(OclProgram Program,string T, uint N, uint Pts)
        {
            public Result<CLResultCode, OclKernel> CreateIndexed() =>
                Program.CreateKernel($"aabb_indexed");
            public Result<CLResultCode, OclKernel> CreatePacked() =>
                Program.CreateKernel($"aabb_packed");
        }
        public static Result<CLResultCode,ProgramContainer> Create<TNum>(OclContext context,uint dims, uint pts)
            where TNum : unmanaged, IFloatingPoint<TNum>
        {
            var source = ForceReadEmbeddedRes("aabb.cl");
            var numName = TNum.Zero switch
            {
                Half => "half",
                float => "float",
                double => "double",
                _ => ""
            };
            if(numName.Length==0)
                return Result<CLResultCode,ProgramContainer>.Failure();
            var defPacked = $"DEFINE_PACKED_AABB_KERNEL({numName}, {dims}, {pts})\n";
            var defIndexed = $"DEFINE_INDEXED_AABB_KERNEL({numName}, {dims}, {pts})\n";
            source = $"{source}{defIndexed}{defPacked}";
            var progRes= context.CreateProgramFromSource(source);
            if (!progRes.TryGetValue(out var program))
                return progRes.Info;
            
            var buildRes= program.Build(context.Devices,opts:"-cl-kernel-arg-info");
            if (!buildRes)
                return buildRes.Info;
            return new ProgramContainer(program,numName,dims,pts);
        }
    }

    internal static string ForceReadEmbeddedRes(string fileName) => ReadEmbeddedRes(fileName)!;
    internal static string? ReadEmbeddedRes(string fileName)
    {
        var type = typeof(Programs);
        var assy = Assembly.GetAssembly(type);
        if (assy is null)
            return null;
        var str= assy.GetManifestResourceStream(type.Namespace + "." + fileName);
        if (str is null)
            return null;
        using var reader = new StreamReader(str);
        return reader.ReadToEnd();
    }
}