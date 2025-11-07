namespace MeshWiz.Utility;

public static class Func
{
    public static ExceptionResult<TResult> Try<TResult>(Func<TResult> func)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static ExceptionResult<TResult> Try<TArg, TResult>(this Func<TArg, TResult> func, TArg arg)
    {
        try
        {
            return func(arg);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static ExceptionResult<TResult> Try<TArg1, TArg2, TResult>(this Func<TArg1, TArg2, TResult> func, TArg1 arg1,
        TArg2 arg2)
    {
        try
        {
            return func(arg1, arg2);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static ExceptionResult<TResult> Try<TArg1, TArg2, TArg3, TResult>(
        this Func<TArg1, TArg2, TArg3, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
    {
        try
        {
            return func(arg1, arg2, arg3);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static ExceptionResult Try(Action func)
    {
        try
        {
            func();
            return ExceptionResult.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static ExceptionResult Try<TArg>(this Action<TArg> func, TArg arg)
    {
        try
        {
            func(arg);
            return ExceptionResult.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static ExceptionResult Try<TArg1, TArg2>(this Action<TArg1, TArg2> func, TArg1 arg1, TArg2 arg2)
    {
        try
        {
            func(arg1, arg2);
            return ExceptionResult.Success();
        }
        catch (Exception ex)
        {
            return ExceptionResult.Failure(ex);
        }
    }

    public static ExceptionResult Try<TArg1, TArg2, TArg3>(this Action<TArg1, TArg2, TArg3> func, TArg1 arg1,
        TArg2 arg2, TArg3 arg3)
    {
        try
        {
            func(arg1, arg2, arg3);
            return ExceptionResult.Success();
        }
        catch (Exception ex)
        {
            return ExceptionResult.Failure(ex);
        }
    }
}




// [SuppressMessage("ReSharper", "InconsistentNaming")]
// public enum Exceptions
// {
//     None = 0,
//     Exception = 1,
//     Unknown = 2,
//     BadImageFormat,
//     AmbiguousImplementation,
//     EncoderFallback,
//     DecoderFallback,
//     TaskScheduler,
//     TaskCanceled,
//     WaitHandleCannotBeOpened,
//     ThreadState,
//     ThreadStart,
//     ThreadInterrupted,
//     ThreadAbort,
//     SynchronizationLock,
//     SemaphoreFull,
//     LockRecursion,
//     AbandonedMutex,
//     Win32,
//     CultureNotFound,
//     MissingSatelliteAssembly,
//     MissingManifestResource,
//     Cryptographic,
//     Verification,
//     Security,
//     Serialization,
//     COM,
//     InvalidComObject,
//     InvalidOleVariantType,
//     EventSource,
//     Unreachable,
//     PathTooLong,
//     InvalidData,
//     EndOfStream,
//     DirectoryNotFound,
//     FileNotFound,
//     FileLoad,
//     TargetParameterCount,
//     TargetInvocation,
//     ReflectionTypeLoad,
//     InvalidFilterCriteria,
//     CustomAttributeFormat,
//     AmbiguousMatch,
//     SwitchExpression,
//     RuntimeWrapped,
//     SEH,
//     SafeArrayTypeMismatch,
//     SafeArrayRankMismatch,
//     MarshalDirective,
//     Target,
//     TypeUnloaded,
//     UnauthorizedAccess,
//     TypeAccess,
//     InsufficientMemory,
//     InsufficientExecutionStack,
//     IndexOutOfRange,
//     FieldAccess,
//     EntryPointNotFound,
//     DuplicateWaitObject,
//     DllNotFound,
//     TypeInitialization,
//     DivideByZero,
//     ContextMarshal,
//     CannotUnloadAppDomain,
//     ArrayTypeMismatch,
//     ArgumentOutOfRange,
//     ArgumentNull,
//     AppDomainUnloaded,
//     Aggregate,
//     AccessViolation,
//     DataMisaligned,
//     InvalidProgram,
//     InvalidCast,
//     KeyNotFound,
//     TimeZoneNotFound,
//     Timeout,
//     MethodAccess,
//     MissingField,
//     StackOverflow,
//     MissingMethod,
//     Rank,
//     MulticastNotSupported,
//     NotFiniteNumber,
//     NotImplemented,
//     PlatformNotSupported,
//     NullReference,
//     ObjectDisposed,
//     InvalidTimeZone,
//     Overflow,
//     OperationCanceled,
//     NotSupported,
//     Format,
//     OutOfMemory,
//     MissingMember,
//     InvalidOperation,
//     Arithmetic,
//     External,
//     TypeLoad,
//     Application,
//     IO,
//     MemberAccess,
//     Argument,
//     System
// }
//
// public static class ExceptionExt
// {
//     [Pure]
//     public static Exceptions GetEnumType(this Exception? ex)
//         => ex switch
//         {
//             null => Exceptions.None,
//             System.BadImageFormatException => Exceptions.BadImageFormat,
//             System.Runtime.AmbiguousImplementationException => Exceptions.AmbiguousImplementation,
//             System.Text.EncoderFallbackException => Exceptions.EncoderFallback,
//             System.Text.DecoderFallbackException => Exceptions.DecoderFallback,
//             System.Threading.Tasks.TaskSchedulerException => Exceptions.TaskScheduler,
//             System.Threading.Tasks.TaskCanceledException => Exceptions.TaskCanceled,
//             System.Threading.WaitHandleCannotBeOpenedException => Exceptions.WaitHandleCannotBeOpened,
//             System.Threading.ThreadStateException => Exceptions.ThreadState,
//             System.Threading.ThreadStartException => Exceptions.ThreadStart,
//             System.Threading.ThreadInterruptedException => Exceptions.ThreadInterrupted,
//             System.Threading.ThreadAbortException => Exceptions.ThreadAbort,
//             System.Threading.SynchronizationLockException => Exceptions.SynchronizationLock,
//             System.Threading.SemaphoreFullException => Exceptions.SemaphoreFull,
//             System.Threading.LockRecursionException => Exceptions.LockRecursion,
//             System.Threading.AbandonedMutexException => Exceptions.AbandonedMutex,
//             System.ComponentModel.Win32Exception => Exceptions.Win32,
//             System.Globalization.CultureNotFoundException => Exceptions.CultureNotFound,
//             System.Resources.MissingSatelliteAssemblyException => Exceptions.MissingSatelliteAssembly,
//             System.Resources.MissingManifestResourceException => Exceptions.MissingManifestResource,
//             System.Security.Cryptography.CryptographicException => Exceptions.Cryptographic,
//             System.Security.VerificationException => Exceptions.Verification,
//             System.Security.SecurityException => Exceptions.Security,
//             System.Runtime.Serialization.SerializationException => Exceptions.Serialization,
//             System.Runtime.InteropServices.COMException => Exceptions.COM,
//             System.Runtime.InteropServices.InvalidComObjectException => Exceptions.InvalidComObject,
//             System.Runtime.InteropServices.InvalidOleVariantTypeException => Exceptions.InvalidOleVariantType,
//             System.Diagnostics.Tracing.EventSourceException => Exceptions.EventSource,
//             System.Diagnostics.UnreachableException => Exceptions.Unreachable,
//             System.IO.PathTooLongException => Exceptions.PathTooLong,
//             System.IO.InvalidDataException => Exceptions.InvalidData,
//             System.IO.EndOfStreamException => Exceptions.EndOfStream,
//             System.IO.DirectoryNotFoundException => Exceptions.DirectoryNotFound,
//             System.IO.FileNotFoundException => Exceptions.FileNotFound,
//             System.IO.FileLoadException => Exceptions.FileLoad,
//             System.Reflection.TargetParameterCountException => Exceptions.TargetParameterCount,
//             System.Reflection.TargetInvocationException => Exceptions.TargetInvocation,
//             System.Reflection.ReflectionTypeLoadException => Exceptions.ReflectionTypeLoad,
//             System.Reflection.InvalidFilterCriteriaException => Exceptions.InvalidFilterCriteria,
//             System.Reflection.CustomAttributeFormatException => Exceptions.CustomAttributeFormat,
//             System.Reflection.AmbiguousMatchException => Exceptions.AmbiguousMatch,
//             System.Runtime.CompilerServices.SwitchExpressionException => Exceptions.SwitchExpression,
//             System.Runtime.CompilerServices.RuntimeWrappedException => Exceptions.RuntimeWrapped,
//             System.Runtime.InteropServices.SEHException => Exceptions.SEH,
//             System.Runtime.InteropServices.SafeArrayTypeMismatchException => Exceptions.SafeArrayTypeMismatch,
//             System.Runtime.InteropServices.SafeArrayRankMismatchException => Exceptions.SafeArrayRankMismatch,
//             System.Runtime.InteropServices.MarshalDirectiveException => Exceptions.MarshalDirective,
//             System.Reflection.TargetException => Exceptions.Target,
//             System.TypeUnloadedException => Exceptions.TypeUnloaded,
//             System.UnauthorizedAccessException => Exceptions.UnauthorizedAccess,
//             System.TypeAccessException => Exceptions.TypeAccess,
//             System.InsufficientMemoryException => Exceptions.InsufficientMemory,
//             System.InsufficientExecutionStackException => Exceptions.InsufficientExecutionStack,
//             System.IndexOutOfRangeException => Exceptions.IndexOutOfRange,
//             System.FieldAccessException => Exceptions.FieldAccess,
//             System.EntryPointNotFoundException => Exceptions.EntryPointNotFound,
//             System.DuplicateWaitObjectException => Exceptions.DuplicateWaitObject,
//             System.DllNotFoundException => Exceptions.DllNotFound,
//             System.TypeInitializationException => Exceptions.TypeInitialization,
//             System.DivideByZeroException => Exceptions.DivideByZero,
//             System.ContextMarshalException => Exceptions.ContextMarshal,
//             System.CannotUnloadAppDomainException => Exceptions.CannotUnloadAppDomain,
//             System.ArrayTypeMismatchException => Exceptions.ArrayTypeMismatch,
//             System.ArgumentOutOfRangeException => Exceptions.ArgumentOutOfRange,
//             System.ArgumentNullException => Exceptions.ArgumentNull,
//             System.AppDomainUnloadedException => Exceptions.AppDomainUnloaded,
//             System.AggregateException => Exceptions.Aggregate,
//             System.AccessViolationException => Exceptions.AccessViolation,
//             System.DataMisalignedException => Exceptions.DataMisaligned,
//             System.InvalidProgramException => Exceptions.InvalidProgram,
//             System.InvalidCastException => Exceptions.InvalidCast,
//             System.Collections.Generic.KeyNotFoundException => Exceptions.KeyNotFound,
//             System.TimeZoneNotFoundException => Exceptions.TimeZoneNotFound,
//             System.TimeoutException => Exceptions.Timeout,
//             System.MethodAccessException => Exceptions.MethodAccess,
//             System.MissingFieldException => Exceptions.MissingField,
//             System.StackOverflowException => Exceptions.StackOverflow,
//             System.MissingMethodException => Exceptions.MissingMethod,
//             System.RankException => Exceptions.Rank,
//             System.MulticastNotSupportedException => Exceptions.MulticastNotSupported,
//             System.NotFiniteNumberException => Exceptions.NotFiniteNumber,
//             System.NotImplementedException => Exceptions.NotImplemented,
//             System.PlatformNotSupportedException => Exceptions.PlatformNotSupported,
//             System.NullReferenceException => Exceptions.NullReference,
//             System.ObjectDisposedException => Exceptions.ObjectDisposed,
//             System.InvalidTimeZoneException => Exceptions.InvalidTimeZone,
//             System.OverflowException => Exceptions.Overflow,
//             System.OperationCanceledException => Exceptions.OperationCanceled,
//             System.NotSupportedException => Exceptions.NotSupported,
//             System.FormatException => Exceptions.Format,
//             System.OutOfMemoryException => Exceptions.OutOfMemory,
//             System.MissingMemberException => Exceptions.MissingMember,
//             System.InvalidOperationException => Exceptions.InvalidOperation,
//             System.ArithmeticException => Exceptions.Arithmetic,
//             System.Runtime.InteropServices.ExternalException => Exceptions.External,
//             System.TypeLoadException => Exceptions.TypeLoad,
//             System.ApplicationException => Exceptions.Application,
//             System.IO.IOException => Exceptions.IO,
//             System.MemberAccessException => Exceptions.MemberAccess,
//             System.ArgumentException => Exceptions.Argument,
//             System.SystemException => Exceptions.System,
//             _ => ex.GetType() == typeof(System.Exception) ? Exceptions.Exception : Exceptions.Unknown,
//         };
// }
