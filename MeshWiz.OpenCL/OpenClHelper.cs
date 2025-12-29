using System.Runtime.CompilerServices;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public static class OpenClHelper
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

    private static string ClErrorMessage(CLResultCode clResultCode, string caller) =>
        $"OpenCL Error: {clResultCode} in {caller}";
}