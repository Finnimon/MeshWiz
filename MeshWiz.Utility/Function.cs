namespace MeshWiz.Utility;

public static class Function
{
    public static bool TryInvoke<TArg, TResult>(this Func<TArg, TResult> func, TArg arg, out TResult? result)
    {
        try
        {
            result = func(arg);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
    public static bool TryInvoke<TResult>(this Func<TResult> func, out TResult? result)
    {
        try
        {
            result = func();
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static TResult? TryInvoke<TResult>(this Func<TResult> func)
    => func.TryInvoke(out var result) ? result : default;
    public static TResult? TryInvoke<TArg, TResult>(this Func<TArg, TResult> func, TArg arg)
        => func.TryInvoke(arg, out var result) ? result : default;
    public static TResult? TryInvoke<TArg,TArg2, TResult>(this Func<TArg,TArg2, TResult> func, TArg arg,TArg2 arg2)
    {
        try
        {
            return func(arg, arg2);
        }
        catch 
        {
            return default;
        }
    }
}
