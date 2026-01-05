using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using MeshWiz.Contracts;
using MeshWiz.OpenCL;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.Math.OpenCL;

public static partial class MathPrograms
{
    public static class AABB
    {
        public record ProgramContainer(OclProgram Program, string T, uint N, uint Pts)
        {
            public Result<OclResultCode, OclKernel> CreateIndexed() =>
                Program.CreateKernel($"aabb_indexed");

            public Result<OclResultCode, OclKernel> CreatePacked() =>
                Program.CreateKernel($"aabb_packed");
        }

        public static Result<OclResultCode, ProgramContainer> Create<TPointBased, TVec, TNum>(OclContext context)
            where TPointBased : unmanaged
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var containerSize = Unsafe.SizeOf<TPointBased>();
            var dims = TVec.Dimensions;
            var numSize = Unsafe.SizeOf<TNum>();
            var vecSize = dims * numSize;
            if (containerSize % vecSize != 0)
                return Result<OclResultCode, ProgramContainer>.DefaultFailure;
            var pCount = containerSize / vecSize;
            return Create<TNum>(context, (uint)dims, (uint)pCount);
        }

        public static Result<OclResultCode, ProgramContainer> Create<TNum>(OclContext context, uint dims, uint pts)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var source = ForceReadEmbeddedRes("aabb.cl");
            var numName = TNum.Zero switch
            {
                Half => "half",
                float => "float",
                double => "double",
                _ => ""
            };
            if (numName.Length == 0)
                return Result<OclResultCode, ProgramContainer>.Failure();
            var defPacked = $"DEFINE_PACKED_AABB_KERNEL({numName}, {dims}, {pts})\n";
            var defIndexed = $"DEFINE_INDEXED_AABB_KERNEL({numName}, {dims}, {pts})\n";
            source = $"{source}{defIndexed}{defPacked}";

            if (numName is "half")
            {
                if (!context.Devices.Iterate().All(d => d.Fp16Supported))
                    return OclResultCode.InvalidDevice;
                source = $"#pragma OPENCL EXTENSION cl_khr_fp16 : enable\n{source}";
            }
            else if (numName is "double")
            {
                if(!context.Devices.Iterate().All(d=>d.Fp64Supported))
                    return OclResultCode.InvalidDevice;
                source = $"#pragma OPENCL EXTENSION cl_khr_fp64 : enable\n{source}";
            }
            var progRes = context.CreateProgramFromSource(source);
            if (!progRes.TryGetValue(out var program))
                return progRes.Info;

            var buildRes = program.Build(context.Devices, opts: "-cl-kernel-arg-info");
            if (!buildRes)
                return buildRes.Info;
            return new ProgramContainer(program, numName, dims, pts);
        }
    }

    internal static string ForceReadEmbeddedRes(string fileName) => ReadEmbeddedRes(fileName)!;

    internal static string? ReadEmbeddedRes(string fileName)
    {
        var type = typeof(MathPrograms);
        var assy = Assembly.GetAssembly(type);
        if (assy is null)
            return null;
        var str = assy.GetManifestResourceStream(type.Namespace + "." + fileName);
        if (str is null)
            return null;
        using var reader = new StreamReader(str);
        return reader.ReadToEnd();
    }
}