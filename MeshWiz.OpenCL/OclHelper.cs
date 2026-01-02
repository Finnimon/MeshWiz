using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
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

    public static Result<CLResultCode, T> AsResult<T>(this CLResultCode code, T? value)
        => code is CLResultCode.Success ? value! : code;

    public static Result<CLResultCode, T> AsResult<T>(this T? value,CLResultCode code)
        => code.AsResult(value);

    private static string ClErrorMessage(CLResultCode clResultCode, string caller) =>
        $"OpenCL Error: {clResultCode} in {caller}";

    
    // ReSharper disable once InconsistentNaming
    public static string GetCLString(ReadOnlySpan<byte> nullTerminatedAscii)
    {
        if(nullTerminatedAscii.Length == 0) return "";
        var rangeEnd = nullTerminatedAscii.IndexOf<byte>(0);
        return Encoding.ASCII.GetString(nullTerminatedAscii[..rangeEnd]);
    }

    // ReSharper disable once InconsistentNaming
    public static string GetCLString(byte[] nullTerminatedAscii)
        => GetCLString(nullTerminatedAscii.AsSpan());

    // ReSharper disable once InconsistentNaming
    public static string[] GetCLStrings(byte[] nullTerminatedAsciiStrings) =>
        GetCLStrings(nullTerminatedAsciiStrings.AsSpan());
    // ReSharper disable once InconsistentNaming
    public static string[] GetCLStrings(ReadOnlySpan<byte> nullTerminatedAsciiStrings)
        => GetCLString(nullTerminatedAsciiStrings).Split(';');
}