using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly partial record struct OclKernel
{
    public readonly record struct Arg(CLKernel Kernel, uint Index)
    {
        public bool ValidIndex => GetNumArgs(Kernel).TryGetValue(out var argCount) && argCount > Index;
        public Result<CLResultCode, string> Name => GetArgInfoString(Kernel, Index, KernelArgInfo.Name);
        public Result<CLResultCode, string> TypeName => GetArgInfoString(Kernel, Index, KernelArgInfo.TypeName);

        public Result<CLResultCode, string> TypeQualifier => GetArgInfoString(Kernel, Index, KernelArgInfo.TypeQualifier);

        public Result<CLResultCode, string> AccessQualifier => GetArgInfoString(Kernel, Index, KernelArgInfo.AccessQualifier);

        public Result<CLResultCode, string> AddressQualifier => GetArgInfoString(Kernel, Index, KernelArgInfo.AddressQualifier);

        public Result<CLResultCode> Set<T>(OclBuffer<T> buffer) where T : unmanaged => CL.SetKernelArg(Kernel, Index, buffer.Underlying);
        public Result<CLResultCode> Set<T>(T value) where T : unmanaged => CL.SetKernelArg(Kernel, Index, value);
    }
}