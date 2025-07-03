using MeshWiz.Contracts;

namespace MeshWiz.Utility.Extensions;

public static class StructExt
{
    
    public static unsafe ReadOnlySpan<byte> ToByteSpan<TStruct>(ref this TStruct me, int byteSize)
        where TStruct : unmanaged
    {
        fixed (TStruct* ptr = &me) return new ReadOnlySpan<byte>((byte*)ptr, byteSize);
    }


    public static ReadOnlySpan<byte> ToByteSpan<TStruct>(ref this TStruct me)
        where TStruct : unmanaged, IByteSize
        => me.ToByteSpan(TStruct.ByteSize);
    

    public static unsafe ReadOnlySpan<TValue> ToSpanSlow<TSource, TValue>(ref this TSource me)
        where TValue : unmanaged
        where TSource : unmanaged
    {
        var length = sizeof(TSource)/sizeof(TValue);
        return me.ToSpanFast<TSource,TValue>(length);
    }
    
    
    public static unsafe ReadOnlySpan<TValue> ToSpanFast<TByteSize, TValue>(ref this TByteSize me)
        where TValue : unmanaged
        where TByteSize : unmanaged, IByteSize
    {
        var length = TByteSize.ByteSize/sizeof(TValue);
        return me.ToSpanFast<TByteSize,TValue>(length);
    }
    
    public static unsafe ReadOnlySpan<TValue> ToSpanFast<TSource, TValue>(ref this TSource me, int length)
        where TValue : unmanaged
        where TSource : unmanaged
    {
        fixed (TSource* ptr = &me) return new ReadOnlySpan<TValue>((TValue*)ptr, length);
    }

    public static unsafe TTo UnsafeAs<TFrom, TTo>(in TFrom from)
    where TFrom : unmanaged
    where TTo : unmanaged
    {
        fixed(TFrom* ptr = &from) return *(TTo*)ptr;
    }
}