using System.Runtime.CompilerServices;
using MeshWiz.Contracts;

namespace MeshWiz.Abstraction.OpenTK;

public static class OpenGLHelper
{
    public static ErrorCode LogGlError(string nameofClass,[CallerMemberName] string? nameofMethod=null)
    {
        var errorCode= GL.GetError();
        if(errorCode==ErrorCode.NoError) return errorCode;
        Console.Error.WriteLine($"GL Error in {nameofClass}.{nameofMethod}: {errorCode}");
        return errorCode;
    }

    public static void ThrowOnGlError(string nameofClass, [CallerMemberName] string? nameofMethod = null)
    {
        var errorCode= GL.GetError();
        if(errorCode==ErrorCode.NoError) return;
        throw new Exception($"GL Error in {nameofClass}.{nameofMethod}: {errorCode}");
    }

    public static int ByteSize<T>(T[] array)
        where T : unmanaged,IByteSize =>
        T.ByteSize*array.Length;
    public static unsafe int UnsafeByteSize<T>(T[] array)
        where T : unmanaged =>
        sizeof(T)*array.Length;

}