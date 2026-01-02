using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly partial record struct OclKernel
{
    public readonly record struct Arg(CLKernel Kernel, uint Index)
    {
        public bool ValidIndex => GetNumArgs(Kernel).TryGetValue(out var argCount) && argCount > Index;
        public Result<OclResultCode, string> Name => GetArgInfoString(Kernel, Index, KernelArgInfo.Name);
        public Result<OclResultCode, string> TypeName => GetArgInfoString(Kernel, Index, KernelArgInfo.TypeName);

        public Result<OclResultCode, string> TypeQualifier => GetArgInfoString(Kernel, Index, KernelArgInfo.TypeQualifier);

        public Result<OclResultCode, string> AccessQualifier => GetArgInfoString(Kernel, Index, KernelArgInfo.AccessQualifier);

        public Result<OclResultCode, string> AddressQualifier => GetArgInfoString(Kernel, Index, KernelArgInfo.AddressQualifier);

        public Result<OclResultCode> Set<T>(OclBuffer<T> buffer) where T : unmanaged =>(OclResultCode) CL.SetKernelArg(Kernel, Index, buffer.Underlying);
        public Result<OclResultCode> Set<T>(T value) where T : unmanaged => (OclResultCode)CL.SetKernelArg(Kernel, Index, value);
    }
}