// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///     列挙型クラス
///     GetValuesの値をstatic変数にキャッシュしておく
/// </summary>
internal abstract class CachedEnumeration<T> : Enumeration
    where T : Enumeration
{
    private static T[] _cachedValues;

    protected CachedEnumeration(int id, string name) : base(id, name)
    {
    }

    private static T[] CachedValues => _cachedValues ??= GetValues<T>().ToArray();

    public static IEnumerable<T> GetValues() => CachedValues;

    public static int GetLength() => CachedValues.Length;

    public static T GetValue(int index) => CachedValues[index];

    public static T GetValue(string name) => CachedValues.FirstOrDefault(_ => _.Name == name);

    public static T Next(T current)
    {
        var index = Array.IndexOf(CachedValues, current);
        if (index == -1)
            throw new ArgumentException($"{current}");

        // Next
        index = (index + 1) % GetLength();
        return _cachedValues[index];
    }
}
