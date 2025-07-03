namespace MeshWiz.TestConsole;

public class TestUtils
{
    public static unsafe int Size<TUnm>(TUnm item)
        where TUnm:unmanaged =>
        sizeof(TUnm);
}