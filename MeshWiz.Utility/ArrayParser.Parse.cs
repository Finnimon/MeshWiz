using System.Globalization;
using System.Numerics;

namespace MeshWiz.Utility;

public static class ArrayParser
{
    public const char Opener = '[';
    public const char Closer = ']';

    public static bool TryParse<TNum>(
        ReadOnlySpan<char> s,
        NumberStyles style,
        IFormatProvider? provider,
        Span<TNum> into
    )
        where TNum : INumberBase<TNum>
    {
        if (style.HasFlag(NumberStyles.AllowLeadingWhite)) s = s.TrimStart();
        if (style.HasFlag(NumberStyles.AllowTrailingWhite)) s = s.TrimEnd();

        if (s.Length < 2 || s[0] != Opener || s[^1] != Closer)
            return false;

        // Trim [ ... ]

        s = s[1..^1].Trim();

        var index = -1;
        while (!s.IsEmpty && index < into.Length-1)
        {
            // Skip leading spaces
            int start = 0;
            var depth = 0;
            while (start < s.Length && char.IsWhiteSpace(s[start]))
                start++;

            if (start == s.Length)
                break;

            
            // find end of next num, possibly skip recursive structures
            var end = start+1;
            if(s[start]==Opener) depth++;
            while (end < s.Length && ((!char.IsWhiteSpace(s[end]) || depth != 0)))
            {
                var endChar = s[end];
                if (endChar == Closer) depth--;
                else if (endChar == Opener) depth++;
                
                end++;
            }

            if (end > s.Length) return false;

            var token = s.Slice(start, end - start);

            if (!TNum.TryParse(token, style, provider, out var value))
                return false;

            into[++index] = value;

            // Advance
            s = s[end..];
        }

        // Must fill exactly
        return index == into.Length-1 && s.Trim().IsEmpty;
    }

    public static bool TryFormat<TNum>(
        ReadOnlySpan<TNum> nums,
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
        where TNum : INumberBase<TNum>
    {
        charsWritten = 0;

        if (destination.IsEmpty)
            return false;

        int written = 0;

        // Write opener
        if (written >= destination.Length)
            return false;
        destination[written++] = Opener;

        for (int i = 0; i < nums.Length; i++)
        {
            if (i > 0)
            {
                if (written >= destination.Length)
                    return false;
                destination[written++] = ' ';
            }

            // Format the number
            if (!nums[i].TryFormat(destination[written..], out int numWritten, format, provider))
                return false;

            written += numWritten;
        }

        // Write closer
        if (written >= destination.Length)
            return false;
        destination[written++] = Closer;

        charsWritten = written;
        return true;
    }
}