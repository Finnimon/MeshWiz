namespace MeshWiz.Utility;

public static class Function
{
    public static bool Try<TArg, TResult>(this Func<TArg, TResult> func, TArg arg, out TResult? result)
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
    public static bool Try<TResult>(this Func<TResult> func, out TResult? result)
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

    public static TResult? Try<TResult>(this Func<TResult> func)
    => func.Try(out var result) ? result : default;
    public static TResult? Try<TArg, TResult>(this Func<TArg, TResult> func, TArg arg)
        => func.Try(arg, out var result) ? result : default;
    public static TResult? Try<TArg,TArg2, TResult>(this Func<TArg,TArg2, TResult> func, TArg arg,TArg2 arg2)
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
