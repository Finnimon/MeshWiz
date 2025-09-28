using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MeshWiz.Contracts;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public interface IVector<TSelf, TNum>
    : IReadOnlyList<TNum>,
        IUnmanagedDataVector<TNum>,
        INumber<TSelf>
    where TNum : unmanaged, INumber<TNum>
    where TSelf : unmanaged, IVector<TSelf, TNum>
{
    [Pure]
    static abstract TSelf FromComponents<TList>(TList components) where TList : IReadOnlyList<TNum>;

    [Pure]
    static abstract TSelf FromComponents<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumberBase<TOtherNum>;

    [Pure]
    static abstract TSelf FromComponentsConstrained<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumberBase<TOtherNum>;

    [Pure]
    static abstract TSelf FromComponentsConstrained<TList>(TList components) where TList : IReadOnlyList<TNum>;

    [Pure]
    static abstract TSelf FromValue(TNum value);

    [Pure]
    static abstract TSelf FromValue<TOtherNum>(TOtherNum other)
        where TOtherNum : INumberBase<TOtherNum>;
    
    [JsonIgnore,XmlIgnore,SoapIgnore,IgnoreDataMember,Pure] static abstract uint Dimensions { get; }
    [JsonIgnore,XmlIgnore,SoapIgnore,IgnoreDataMember,Pure] int IReadOnlyCollection<TNum>.Count => (int)TSelf.Dimensions;
    [JsonIgnore,XmlIgnore,SoapIgnore,IgnoreDataMember,Pure] TNum Length { get; }
    [JsonIgnore,XmlIgnore,SoapIgnore,IgnoreDataMember,Pure] TNum SquaredLength { get; }
    [JsonIgnore,XmlIgnore,SoapIgnore,IgnoreDataMember,Pure] TSelf Normalized { get; }

    [Pure]
    TSelf Add(TSelf other);

    [Pure]
    TSelf Subtract(TSelf other);

    [Pure]
    TSelf Scale(TNum scalar);

    [Pure]
    TSelf Divide(TNum divisor);

    [Pure]
    TNum Dot(TSelf other);

    [Pure]
    TNum DistanceTo(TSelf other);

    TNum SquaredDistanceTo(TSelf other);

    [Pure]
    static abstract TSelf Lerp(TSelf from, TSelf to, TNum t);

    [Pure]
    static abstract TSelf ExactLerp(TSelf from, TSelf toward, TNum exactDistance);

    [Pure]
    bool IsParallelTo(TSelf other);

    bool IsParallelTo(TSelf other, TNum tolerance);

    [Pure]
    bool IsApprox(TSelf other, TNum squareTolerance)
        => SquaredDistanceTo(other) < squareTolerance;

    [Pure]
    bool IsApprox(TSelf other);

    [Pure]
    public static abstract TSelf operator *(TSelf l, TNum r);

    [Pure]
    public static abstract TSelf operator *(TNum l, TSelf r);

    [Pure]
    public static abstract TSelf operator /(TSelf l, TNum r);

    [Pure]
    public static abstract TSelf operator /(TNum l, TSelf r);


    [JsonIgnore,XmlIgnore,SoapIgnore,IgnoreDataMember,Pure] public TNum Sum { get; }
    
    static bool INumberBase<TSelf>.TryConvertFromChecked<TOther>(TOther value, out TSelf result)
    {
        if (value is TNum)
        {
            result = TSelf.FromValue(value);
            return true;
        }

        if (value is TSelf self)
        {
            result = self;
            return true;
        }

        var valueIsPrimitive = IsBasicNumberType(value);
        if (valueIsPrimitive)
        {
            result = TSelf.FromValue(value);
            return true;
        }

        try
        {
            result = value switch
            {
                IEnumerable<byte> e => TSelf.FromComponentsConstrained<byte[], byte>(e.ToArray()),
                IEnumerable<sbyte> e => TSelf.FromComponentsConstrained<sbyte[], sbyte>(e.ToArray()),
                IEnumerable<ushort> e => TSelf.FromComponentsConstrained<ushort[], ushort>(e.ToArray()),
                IEnumerable<short> e => TSelf.FromComponentsConstrained<short[], short>(e.ToArray()),
                IEnumerable<uint> e => TSelf.FromComponentsConstrained<uint[], uint>(e.ToArray()),
                IEnumerable<int> e => TSelf.FromComponentsConstrained<int[], int>(e.ToArray()),
                IEnumerable<ulong> e => TSelf.FromComponentsConstrained<ulong[], ulong>(e.ToArray()),
                IEnumerable<long> e => TSelf.FromComponentsConstrained<long[], long>(e.ToArray()),
                IEnumerable<Half> e => TSelf.FromComponentsConstrained<Half[], Half>(e.ToArray()),
                IEnumerable<float> e => TSelf.FromComponentsConstrained<float[], float>(e.ToArray()),
                IEnumerable<double> e => TSelf.FromComponentsConstrained<double[], double>(e.ToArray()),
                IEnumerable<BigInteger> e => TSelf.FromComponentsConstrained<BigInteger[], BigInteger>(e.ToArray()),
                IEnumerable<decimal> e => TSelf.FromComponentsConstrained<decimal[], decimal>(e.ToArray()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            return true;
        }
        catch
        {
            // ignored
        }

        result = default!;
        return false;
    }

    /// <inheritdoc />
    static bool INumberBase<TSelf>.TryConvertFromSaturating<TOther>(TOther value, out TSelf result)
    {
        if (value is TNum)
        {
            result = TSelf.FromValue(value);
            return true;
        }

        if (value is TSelf self)
        {
            result = self;
            return true;
        }

        var valueIsPrimitive = IsBasicNumberType(value);
        if (valueIsPrimitive)
        {
            result = TSelf.FromValue(value);
            return true;
        }

        try
        {
            result = value switch
            {
                IEnumerable<byte> e => TSelf.FromComponentsConstrained<byte[], byte>(e.ToArray()),
                IEnumerable<sbyte> e => TSelf.FromComponentsConstrained<sbyte[], sbyte>(e.ToArray()),
                IEnumerable<ushort> e => TSelf.FromComponentsConstrained<ushort[], ushort>(e.ToArray()),
                IEnumerable<short> e => TSelf.FromComponentsConstrained<short[], short>(e.ToArray()),
                IEnumerable<uint> e => TSelf.FromComponentsConstrained<uint[], uint>(e.ToArray()),
                IEnumerable<int> e => TSelf.FromComponentsConstrained<int[], int>(e.ToArray()),
                IEnumerable<ulong> e => TSelf.FromComponentsConstrained<ulong[], ulong>(e.ToArray()),
                IEnumerable<long> e => TSelf.FromComponentsConstrained<long[], long>(e.ToArray()),
                IEnumerable<Half> e => TSelf.FromComponentsConstrained<Half[], Half>(e.ToArray()),
                IEnumerable<float> e => TSelf.FromComponentsConstrained<float[], float>(e.ToArray()),
                IEnumerable<double> e => TSelf.FromComponentsConstrained<double[], double>(e.ToArray()),
                IEnumerable<BigInteger> e => TSelf.FromComponentsConstrained<BigInteger[], BigInteger>(e.ToArray()),
                IEnumerable<decimal> e => TSelf.FromComponentsConstrained<decimal[], decimal>(e.ToArray()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            return true;
        }
        catch
        {
            // ignored
        }

        result = default!;
        return false;
    }

    /// <inheritdoc />
    static bool INumberBase<TSelf>.TryConvertFromTruncating<TOther>(TOther value, out TSelf result)
    {
        if (value is TNum)
        {
            result = TSelf.FromValue(value);
            return true;
        }

        if (value is TSelf self)
        {
            result = self;
            return true;
        }

        var valueIsPrimitive = IsBasicNumberType(value);
        if (valueIsPrimitive)
        {
            result = TSelf.FromValue(value);
            return true;
        }

        try
        {
            result = value switch
            {
                IEnumerable<byte> e => TSelf.FromComponentsConstrained<byte[], byte>(e.ToArray()),
                IEnumerable<sbyte> e => TSelf.FromComponentsConstrained<sbyte[], sbyte>(e.ToArray()),
                IEnumerable<ushort> e => TSelf.FromComponentsConstrained<ushort[], ushort>(e.ToArray()),
                IEnumerable<short> e => TSelf.FromComponentsConstrained<short[], short>(e.ToArray()),
                IEnumerable<uint> e => TSelf.FromComponentsConstrained<uint[], uint>(e.ToArray()),
                IEnumerable<int> e => TSelf.FromComponentsConstrained<int[], int>(e.ToArray()),
                IEnumerable<ulong> e => TSelf.FromComponentsConstrained<ulong[], ulong>(e.ToArray()),
                IEnumerable<long> e => TSelf.FromComponentsConstrained<long[], long>(e.ToArray()),
                IEnumerable<Half> e => TSelf.FromComponentsConstrained<Half[], Half>(e.ToArray()),
                IEnumerable<float> e => TSelf.FromComponentsConstrained<float[], float>(e.ToArray()),
                IEnumerable<double> e => TSelf.FromComponentsConstrained<double[], double>(e.ToArray()),
                IEnumerable<BigInteger> e => TSelf.FromComponentsConstrained<BigInteger[], BigInteger>(e.ToArray()),
                IEnumerable<decimal> e => TSelf.FromComponentsConstrained<decimal[], decimal>(e.ToArray()),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            return true;
        }
        catch
        {
            // ignored
        }

        result = default!;
        return false;
    }

    private static bool TryAsConvert<TOther>(TSelf value, out TOther result)
#nullable disable
        where TOther : INumberBase<TOther>
    {
        if (value is TOther other)
        {
            result = other;
            return true;
        }

        var len = value.Length;
        if (typeof(TOther) == typeof(TNum))
        {
            result = (TOther)(object)len;
            return true;
        }

        if (IsBasicNumberType(value) && IsBasicNumberType(len))
        {
            result = (TOther)(object)len;
            return true;
        }

        result = default!;
        return false;
    }
#nullable restore

    /// <inheritdoc />
    static bool INumberBase<TSelf>.TryConvertToChecked<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result)
        => TryAsConvert(value, out result)
           || TOther.TryConvertFromChecked(value, out result)
           || TNum.TryConvertToChecked(value.Length, out result)
           || TOther.TryConvertFromChecked(value.Length, out result);

    /// <inheritdoc />
    static bool INumberBase<TSelf>.TryConvertToSaturating<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result)
        => TryAsConvert(value, out result)
           || TOther.TryConvertFromSaturating(value, out result)
           || TNum.TryConvertToSaturating(value.Length, out result)
           || TOther.TryConvertFromSaturating(value.Length, out result);


    /// <inheritdoc />
    static bool INumberBase<TSelf>.TryConvertToTruncating<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result)
        => TryAsConvert(value, out result)
           || TOther.TryConvertFromTruncating(value, out result)
           || TNum.TryConvertToTruncating(value.Length, out result)
           || TOther.TryConvertFromTruncating(value.Length, out result);

    public static bool IsBasicNumberType<TOther>(TOther t)
        => t switch
        {
            byte => true,
            sbyte => true,
            ushort => true,
            short => true,
            uint => true,
            int => true,
            ulong => true,
            long => true,
            Half => true,
            float => true,
            double => true,
            BigInteger => true,
            decimal => true,
            _ => false,
        };


    /// <inheritdoc />
    static bool INumberBase<TSelf>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider,
        out TSelf result)
    {
        var buf = new TNum[TSelf.Dimensions];
        var success = ArrayParser.TryParse(s, style, provider, buf);
        if (!success)
        {
            result = default!;
            return false;
        }

        result = TSelf.FromComponents(buf);
        return true;
    }

    /// <inheritdoc />
    static bool INumberBase<TSelf>.TryParse([NotNullWhen(true)] string? s, NumberStyles style,
        IFormatProvider? provider, out TSelf result)
    {
        result = default;
        return s is not null && TSelf.TryParse(s.AsSpan(), style, provider, out result!);
    }
}