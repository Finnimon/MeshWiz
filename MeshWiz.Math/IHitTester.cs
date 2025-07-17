namespace MeshWiz.Math;

public interface IHitTester<in THit, TResult>
{
    public bool HitTest(THit test);
    public bool HitTest(THit test, out TResult result);
    
}