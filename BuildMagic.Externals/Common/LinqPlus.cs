// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildMagic;

public static class LinqPlus
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        return source.Where(x => x != null)!;
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        return source.Where(x => x.HasValue).Select(x => x!.Value);
    }

    public static IEnumerable<TResult> SelectWhereNotNull<T, TResult>(this IEnumerable<T> source,
        Func<T, TResult?> predicate) where TResult : class
    {
        return source.Select(predicate).WhereNotNull();
    }

    public static IEnumerable<TResult> SelectWhereNotNull<T, TResult>(this IEnumerable<T> source,
        Func<T, TResult?> predicate) where TResult : struct
    {
        return source.Select(predicate).WhereNotNull();
    }

    public static int FindIndex<T>(this IEnumerable<T> source, Predicate<T> predicate)
    {
        var i = 0;
        foreach (var element in source)
        {
            if (predicate(element))
                return i;
            i++;
        }

        return -1;
    }

#if NETCOREAPP3_0_OR_GREATER
    public static int FindIndex<T>(this IEnumerable<T> source, ReadOnlySpan<T> matchingElements)
        where T : IEquatable<T>?
    {
        var i = 0;
        foreach (var element in source)
        {
            if (matchingElements.Contains(element))
                return i;
            i++;
        }

        return -1;
    }

#endif
}
