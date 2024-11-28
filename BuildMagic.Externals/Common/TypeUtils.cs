// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace BuildMagic;

internal static class TypeUtils
{
    [ThreadStatic] private static StringBuilder? _tempStringBuilder;

    [ThreadStatic] private static List<string>? _tempStringList;

    private static readonly ReadOnlyMemory<string> BuiltinSerializableTypes = new[]
    {
        "UnityEngine.Vector3",
        "UnityEngine.Vector3Int",
        "UnityEngine.Vector2",
        "UnityEngine.Vector2Int",
        "UnityEngine.Quaternion",
        "UnityEngine.Color",
        "UnityEngine.Bounds",
        "UnityEngine.Vector4",
        "UnityEngine.Rect",
        "UnityEngine.RectInt",
        "UnityEngine.Matrix4x4",
        "UnityEngine.Color32",
        "UnityEngine.LayerMask",
        "UnityEngine.PropertyName",
        "UnityEngine.Rendering.SphericalHarmonicsL2",
        "UnityEngine.Hash128",
        "UnityEngine.RenderingLayerMask",
        "UnityEngine.AnimationCurve",
        "UnityEngine.Gradient",
        "UnityEngine.RectOffset"
    };

    public static bool MatchFullMetadataName(this ISymbol? symbol, string fullMetadataName)
    {
        if (symbol is null) return false;

        static bool Match(ref ReadOnlySpan<char> name, ISymbol? symbol)
        {
            if (symbol is null) return false;
            if (symbol is IModuleSymbol) return true;
            if (symbol is INamespaceSymbol { IsGlobalNamespace: true }) return true;
            if (!Match(ref name, symbol.ContainingSymbol)) return false;

            if (!name.StartsWith(symbol.MetadataName.AsSpan())) return false;
            name = name.Slice(symbol.MetadataName.Length);
            if (name.Length >= 1 && name[0] == '.') name = name.Slice(1);
            return true;
        }

        var s = fullMetadataName.AsSpan();
        return Match(ref s, symbol);
    }

    public static string ToFullMetadataName(this INamedTypeSymbol symbol)
    {
        _tempStringList ??= new List<string>();
        _tempStringList.Clear();

        _tempStringList.Add(symbol.MetadataName);

        var cursor = symbol;
        while (cursor.ContainingType != null)
        {
            cursor = cursor.ContainingType;
            _tempStringList.Add(cursor.MetadataName);
        }

        if (cursor.ContainingNamespace != null && !cursor.ContainingNamespace.IsGlobalNamespace)
            _tempStringList.Add(cursor.ContainingNamespace.ToString());

        _tempStringBuilder ??= new StringBuilder();
        _tempStringBuilder.Clear();

        for (var i = _tempStringList.Count - 1; i >= 0; i--)
        {
            _tempStringBuilder.Append(_tempStringList[i]);
            if (i != 0) _tempStringBuilder.Append(".");
        }

        return _tempStringBuilder.ToString();
    }

    public static string ToTypeParameterConstraintExpression(this ITypeParameterSymbol symbol)
    {
        _tempStringList ??= new List<string>();
        _tempStringList.Clear();

        if (symbol.HasValueTypeConstraint && !symbol.HasUnmanagedTypeConstraint) _tempStringList.Add("struct");

        if (symbol.HasReferenceTypeConstraint) _tempStringList.Add("class");

        if (symbol.HasNotNullConstraint) _tempStringList.Add("notnull");

        if (symbol.HasUnmanagedTypeConstraint) _tempStringList.Add("unmanaged");

        foreach (var baseTypeConstraint in symbol.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Class))
            _tempStringList.Add(baseTypeConstraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        foreach (var baseTypeConstraint in symbol.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Interface))
            _tempStringList.Add(baseTypeConstraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        if (symbol.HasConstructorConstraint) _tempStringList.Add("new()");

        if (_tempStringList.Count == 0) return "";

        return $"where {symbol.Name} : {string.Join(", ", _tempStringList)}";
    }

    public static string ToTypeParametersExpression(this INamedTypeSymbol symbol)
    {
        var typeParameters = symbol.TypeParameters;
        return typeParameters.Length == 0
            ? ""
            : $"<{string.Join(", ", typeParameters.Select(p => p.Name))}>";
    }

    public static string ToTypeConstraintsExpression(this INamedTypeSymbol symbol)
    {
        var typeParameters = symbol.TypeParameters;
        return typeParameters.Length == 0
            ? ""
            : string.Join(" ",
                typeParameters.Select(p => p.ToTypeParameterConstraintExpression())
                    .Where(e => !string.IsNullOrEmpty(e)));
    }

    public static bool IsSerializedField(IFieldSymbol field)
    {
        if (field.IsStatic) return false;
        if (field.IsReadOnly) return false;
        if (field.GetAttributes()
            .Any(attr => attr.AttributeClass.MatchFullMetadataName("System.NonSerializedAttribute"))) return false;

        var hasSerializeReference = field.GetAttributes().Any(attr =>
            attr.AttributeClass.MatchFullMetadataName("UnityEngine.SerializeReference"));

        if (hasSerializeReference && field.Type.IsReferenceType) return true;

        var hasSerializeField = field.GetAttributes()
            .Any(attr => attr.AttributeClass.MatchFullMetadataName("UnityEngine.SerializeField"));

        if (field.DeclaredAccessibility != Accessibility.Public && !hasSerializeField && !hasSerializeReference)
            return false;

        return IsSerializableFieldType(field.Type);
    }

    public static bool IsSerializableFieldType(ITypeSymbol type)
    {
        if (type.IsStatic) return false;

        if (type is IArrayTypeSymbol arrayType) return IsSerializableFieldType(arrayType.ElementType);

        if (type.SpecialType is
            SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_UInt16 or
            SpecialType.System_Int16 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt64 or
            SpecialType.System_Int64 or
            SpecialType.System_Boolean or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Char or
            SpecialType.System_String)
            return true;

        if (type.BaseType is { SpecialType: SpecialType.System_Enum }) return true;

        if (GetBaseTypes(type, true).OfType<INamedTypeSymbol>()
            .Any(t => t.MatchFullMetadataName("UnityEngine.Object"))) return true;

        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.IsUnboundGenericType) return false;
            if (namedType.IsGenericType && namedType.ConstructUnboundGenericType()
                    .MatchFullMetadataName("System.Collections.Generic.List`1"))
                return IsSerializableFieldType(namedType.TypeArguments[0]);

            foreach (var fullMetadataName in BuiltinSerializableTypes.Span)
                if (namedType.MatchFullMetadataName(fullMetadataName))
                    return true;

            if (namedType.IsSerializable && !type.IsReadOnly) return true;
        }

        return false;
    }

    public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol type, bool includeSelf)
    {
        var cursor = type;
        if (includeSelf) yield return cursor;
        while ((cursor = cursor.BaseType) != null) yield return cursor;
    }

    public static string ToDisplayString(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.NotApplicable => "",
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null)
        };
    }
}
