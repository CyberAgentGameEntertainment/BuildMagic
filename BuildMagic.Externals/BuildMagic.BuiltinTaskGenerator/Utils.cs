// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BuildMagic.BuiltinTaskGenerator;

public class Utils
{
    private static bool IsAlphaNumeric(char c)
    {
        return c is >= '0' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }

    private static bool StartsWithKnownName(ReadOnlySpan<char> name, out int skipped)
    {
        ReadOnlySpan<string> knownNames = ["iOS", "iPad", "iPod", "iPhone", "visionOS", "x86", "x64", "ARM", "Il2Cpp"];

        foreach (var knownName in knownNames)
            if (name.StartsWith(knownName, StringComparison.Ordinal))
            {
                skipped = knownName.Length;
                return true;
            }

        skipped = 0;
        return false;
    }

    public static string ToNiceLabelName(ReadOnlySpan<char> src)
    {
        Span<char> buffer = stackalloc char[1024];

        var current = buffer;

        var prevDivided = false;

        var prev = ' ';

        while (current.Length > 0 && src.Length > 0)
        {
            var prevDividedCurrent = prevDivided;
            prevDivided = false;

            if (prevDividedCurrent)
            {
                current[0] = prev = ' ';
                current = current[1..];
            }

            if (StartsWithKnownName(src, out var skipped))
            {
                if (prev != ' ')
                {
                    current[0] = prev = ' ';
                    current = current[1..];
                }

                src[..skipped].CopyTo(current);
                src = src[skipped..];
                current = current[skipped..];
                prevDivided = true;
                continue;
            }

            var c = src[0];

            var isUpper = c is >= 'A' and <= 'Z';
            var isDigit = c is >= '0' and <= '9';
            var prevUpper = prev is >= 'A' and <= 'Z';
            // var nextLower = src.Length >= 2 && src[1] is >= 'a' and <= 'z';
            var prevDigit = prev is >= '0' and <= '9';

            if (prev != ' ' &&
                (isDigit && !prevDigit && !prevUpper || isUpper && !prevUpper /* || isUpper && prevUpper && nextLower*/
                ))
            {
                current[0] = prev = ' ';
                current = current[1..];
            }

            if (prev == ' ' && c is >= 'a' and <= 'z') c = (char)(c - (char)('a' - 'A'));

            current[0] = prev = c;
            current = current[1..];
            src = src[1..];
        }

        var length =
            (int)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(buffer), ref MemoryMarshal.GetReference(current)) /
            sizeof(char);

        return buffer[..length].ToString();
    }
}
