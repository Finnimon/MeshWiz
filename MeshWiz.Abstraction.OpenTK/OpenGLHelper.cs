using MeshWiz.Contracts;

namespace MeshWiz.Abstraction.OpenTK;

public static class OpenGLHelper
{
    public static ErrorCode LogGlError(string nameofClass, string nameofMethod)
    {
        var errorCode= GL.GetError();
        if(errorCode==ErrorCode.NoError) return errorCode;
        Console.Error.WriteLine($"GL Error in {nameofClass}.{nameofMethod}: {errorCode}");
        return errorCode;
    }

    public static int ByteSize<T>(T[] array)
        where T : unmanaged,IByteSize =>
        T.ByteSize*array.Length;
}