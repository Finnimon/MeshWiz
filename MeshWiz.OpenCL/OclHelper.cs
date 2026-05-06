using System;
using System.Runtime.CompilerServices;
using System.Text;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public static class OclHelper
{
    public static OclResultCode LogError(this CLResultCode clCode, [CallerMemberName] string caller = "")
    {
        var clResultCode=(OclResultCode)clCode;
        if (clResultCode is OclResultCode.Success) return clResultCode;
        Console.WriteLine(ClErrorMessage(clResultCode, caller));
        return clResultCode;
    }

    public static OclResultCode ThrowOnError(this CLResultCode clCode, [CallerMemberName] string caller = "",
        params OclResultCode[] ignore)
    {
        var clResultCode=(OclResultCode)clCode;
        if (clResultCode is OclResultCode.Success || ignore.Contains(clResultCode)) return clResultCode;
        throw new InvalidOperationException(ClErrorMessage(clResultCode, caller));
    }

    public static Result<OclResultCode, T> AsResult<T>(this CLResultCode code, T? value)
        => code is CLResultCode.Success ? value! : (OclResultCode)code;

    public static Result<OclResultCode, T> AsResult<T>(this T? value,CLResultCode code)
        => code.AsResult(value);

    private static string ClErrorMessage(OclResultCode clResultCode, string caller) =>
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

    public static OclQueueManager Managed(this OclCommandQueue queue) => new(queue);

    public static Result<OclResultCode, byte[]> MemObjectInfo(this nint memObject, MemoryObjectInfo target)
        => CL.GetMemObjectInfo(memObject, target, out var dat).AsResult(dat);
    public static Result<OclResultCode, OclContext> MemObjectContext(this nint memObject)
        => MemObjectInfo(memObject,MemoryObjectInfo.Context)
            .Select(b=>Unsafe.ReadUnaligned<CLContext>(in b[0]))
            .Select(OclContext.Create);

    public static Result<OclResultCode, int> MemObjectRefCount(this nint memObject)
        => MemObjectInfo(memObject, MemoryObjectInfo.ReferenceCount)
            .Select(b => BitConverter.ToInt32(b));

    public static Result<OclResultCode, MemoryObjectType> MemObjectType(this nint memObject)
        => MemObjectInfo(memObject, MemoryObjectInfo.Type).Select(b => Unsafe.ReadUnaligned<MemoryObjectType>(in b[0]));
    public static Result<OclResultCode, MemoryFlags> MemObjectFlags(this nint memObject)
        => MemObjectInfo(memObject, MemoryObjectInfo.Flags).Select(b => Unsafe.ReadUnaligned<MemoryFlags>(in b[0]));

}