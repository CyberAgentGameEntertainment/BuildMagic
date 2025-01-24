// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public abstract class Enumeration : IComparable
{
    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Name { get; }
    public int Id { get; }
    public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);

    /// <summary>
    ///     Enumerate all values of the given enum
    /// </summary>
    public static IEnumerable<T> GetValues<T>() where T : Enumeration
    {
        var fields = typeof(T).GetFields(BindingFlags.Public |
                                         BindingFlags.Static |
                                         BindingFlags.DeclaredOnly);

        return fields.Where(f => f.FieldType == typeof(T)).Select(f => f.GetValue(null)).Cast<T>()
                     .OrderBy(_ => _.Id);
    }

    public override bool Equals(object obj)
    {
        var otherValue = obj as Enumeration;

        if (otherValue == null)
            return false;

        var typeMatches = GetType() == obj.GetType();
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    public override int GetHashCode() => Id;
    public override string ToString() => Name;
}
