using CommunityToolkit.Diagnostics;

namespace MeshWiz.Utility;

public static class EnumResultHelper<TInfo>
    where TInfo : unmanaged, Enum
{
    public static readonly TInfo SuccessConstant;
    public static readonly TInfo DefaultFailureConstant;
    
    public static void ValidateFailureInfo(TInfo failureInfo)
    {
        if (!Enums.IsSuccess(failureInfo)) return;
        ThrowHelper.ThrowInvalidOperationException("Invalid failure type");
    }

    static EnumResultHelper()
    {
        bool invalidType;
        var type = typeof(TInfo);
        try
        {
            SuccessConstant = default;
            DefaultFailureConstant = (TInfo)(object)1;
            var isEnum = type.IsAssignableTo(typeof(Enum));
            if (!isEnum)
                return;
            if(!Enum.IsDefined(type, DefaultFailureConstant))
                return;
            DefaultFailureConstant = (TInfo)(object)-1;
            
            if(!Enum.IsDefined(type, DefaultFailureConstant))
                return;
            invalidType = true;
        }
        catch
        {
            invalidType = true;
        }

        if (!invalidType) return;
        ThrowHelper.ThrowNotSupportedException(GetBadTypeMessage());
    }

    private static string GetBadTypeMessage()
        => $"The type {typeof(TInfo)} is not supported. Int32 must be explicitly convertible to {nameof(TInfo)} and 1 must be defined for enums.";
}