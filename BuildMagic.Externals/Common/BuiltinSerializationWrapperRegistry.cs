// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Linq;
using Microsoft.CodeAnalysis;

namespace BuildMagic;

internal static class BuiltinSerializationWrapperRegistry
{
    private static readonly (string targetTypeFullMetadataName, string wrapperTypeExpression)[] _wrappers =
    {
        ("UnityEditor.Build.NamedBuildTarget", "global::BuildMagicEditor.NamedBuildTargetSerializationWrapper")
    };

    public static bool BuiltinSerializationWrapperExists(this ITypeSymbol typeSymbol, out string wrapperTypeExpression)
    {
        wrapperTypeExpression = _wrappers
            .FirstOrDefault(tuple => typeSymbol.MatchFullMetadataName(tuple.targetTypeFullMetadataName))
            .wrapperTypeExpression;

        return !string.IsNullOrEmpty(wrapperTypeExpression);
    }
}
