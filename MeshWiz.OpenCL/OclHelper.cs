using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public static class OclHelper
{
    public static CLResultCode LogError(this CLResultCode clResultCode, [CallerMemberName] string caller = "")
    {
        if (clResultCode is CLResultCode.Success) return clResultCode;
        Console.WriteLine(ClErrorMessage(clResultCode, caller));
        return clResultCode;
    }

    public static CLResultCode ThrowOnError(this CLResultCode clResultCode, [CallerMemberName] string caller = "",
        params CLResultCode[] ignore)
    {
        if (clResultCode is CLResultCode.Success || ignore.Contains(clResultCode)) return clResultCode;
        throw new InvalidOperationException(ClErrorMessage(clResultCode, caller));
    }

    public static Result<CLResultCode, T> AsResult<T>(this CLResultCode code, T value)
        => code is CLResultCode.Success ? value : code;

    public static Result<CLResultCode, T> AsResult<T>(this T value,CLResultCode code)
        => code.AsResult(value);

    private static string ClErrorMessage(CLResultCode clResultCode, string caller) =>
        $"OpenCL Error: {clResultCode} in {caller}";
}