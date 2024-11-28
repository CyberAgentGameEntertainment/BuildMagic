// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace BuildMagic;

internal readonly record struct ValueTupleInfo
{
    private ValueTupleInfo(INamedTypeSymbol tupleType)
    {
        TupleType = tupleType;
    }

    public INamedTypeSymbol TupleType { get; }

    public IEnumerable<ValueTupleElementInfo> GetElements()
    {
        foreach (var info in GetElementsCore(TupleType)) yield return info;
    }

    private static IEnumerable<ValueTupleElementInfo> GetElementsCore(INamedTypeSymbol tupleType)
    {
        foreach (var element in tupleType.TupleElements)
        {
            var type = element.Type;
            if (type is INamedTypeSymbol { IsTupleType: true } tuple)
            {
                foreach (var info in GetElementsCore(tuple)) yield return info;

                continue;
            }

            yield return new ValueTupleElementInfo(element.Name, element.Type);
        }
    }

    public static bool TryCreate(ITypeSymbol type, out ValueTupleInfo info)
    {
        if (type is not INamedTypeSymbol { IsTupleType: true } tuple)
        {
            info = default;
            return false;
        }

        info = new ValueTupleInfo(tuple);
        return true;
    }
}

internal readonly record struct ValueTupleElementInfo(string? Name, ITypeSymbol Type)
{
}
