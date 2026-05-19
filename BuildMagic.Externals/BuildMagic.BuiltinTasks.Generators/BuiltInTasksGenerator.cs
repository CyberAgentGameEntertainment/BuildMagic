// --------------------------------------------------------------
// Copyright 2026 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BuildMagic.BuiltinTasks.Generators;

[Generator(LanguageNames.CSharp)]
public class BuiltInTasksGenerator : IIncrementalGenerator
{
    private const string TargetAssemblyName = "BuildMagic.Editor";
    private const string EmittedNamespace = "BuildMagicEditor.BuiltIn";

    private static readonly SymbolDisplayFormat FullyQualifiedNoKeywords = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    private static readonly string[] RootTypeNames =
    {
        "UnityEditor.PlayerSettings",
        "UnityEditor.EditorUserBuildSettings",
    };

    // Priority order for dictionary key selection on multi-parameter setters.
    // Mirrors BuildMagic.BuiltinTaskGenerator/appsettings.json "DictionaryKeyTypes".
    private static readonly string[] DictionaryKeyTypes =
    {
        "global::UnityEditor.Build.NamedBuildTarget",
        "global::UnityEditor.BuildTarget",
        "global::UnityEditor.BuildTargetGroup",
        "global::UnityEditor.IconKind",
        "global::UnityEngine.LogType",
        "global::UnityEditor.AspectRatio",
        "global::UnityEditor.iOSLaunchScreenImageType",
        "global::UnityEditor.PlayerSettings.WSAImageType",
        "global::UnityEditor.PlayerSettings.WSAImageScale",
        "global::UnityEditor.PlayerSettings.WSACapability",
        "global::UnityEditor.PlayerSettings.WSATargetFamily",
    };

    // Built-in serialization wrappers. Mirrors BuildMagic/Common/BuiltinSerializationWrapperRegistry.cs.
    private static readonly (string targetTypeFullMetadataName, string wrapperTypeExpression)[] Wrappers =
    {
        ("UnityEditor.Build.NamedBuildTarget", "global::BuildMagicEditor.NamedBuildTargetSerializationWrapper"),
    };

    // Per-setter-expression overrides. Mirrors BuildMagic.BuiltinTaskGenerator/appsettings.json "Apis".
    // Key is setter expression with `global::` stripped (same form as offline matches).
    private static readonly Dictionary<string, ApiOverride> ApiOverrides = new()
    {
        ["UnityEditor.PlayerSettings.SetScriptingDefineSymbols({0}, {1});"] = new ApiOverride(
            new[] { "global::UnityEditor.Build.NamedBuildTarget", "global::System.String" },
            ignored: true, overrideDisplayName: null),
        ["UnityEditor.PlayerSettings.SetIl2CppCompilerConfiguration({0}, {1});"] = new ApiOverride(
            new[] { "global::UnityEditor.Build.NamedBuildTarget", "global::UnityEditor.Il2CppCompilerConfiguration" },
            ignored: null, overrideDisplayName: "PlayerSettings: C++ Compiler Configuration (IL2CPP)"),
        ["UnityEditor.PlayerSettings.Android.buildApkPerCpuArchitecture = {0};"] = new ApiOverride(
            new[] { "global::System.Boolean" }, null, "PlayerSettings.Android: Split APKs by target architecture"),
        ["UnityEditor.PlayerSettings.Android.androidIsGame = {0};"] = new ApiOverride(
            new[] { "global::System.Boolean" }, null, "PlayerSettings.Android: Android Game"),
        ["UnityEditor.PlayerSettings.Android.minSdkVersion = {0};"] = new ApiOverride(
            new[] { "global::UnityEditor.AndroidSdkVersions" }, null, "PlayerSettings.Android: Minimum API Level"),
        ["UnityEditor.PlayerSettings.Android.targetSdkVersion = {0};"] = new ApiOverride(
            new[] { "global::UnityEditor.AndroidSdkVersions" }, null, "PlayerSettings.Android: Target API Level"),
        ["UnityEditor.PlayerSettings.Android.useAPKExpansionFiles = {0};"] = new ApiOverride(
            new[] { "global::System.Boolean" }, null, "PlayerSettings.Android: Split Application Binary"),
        ["UnityEditor.PlayerSettings.Android.forceSDCardPermission = {0};"] = new ApiOverride(
            new[] { "global::System.Boolean" }, null, "PlayerSettings.Android: Write Permission (Force SD Card)"),
        ["UnityEditor.PlayerSettings.Android.startInFullscreen = {0};"] = new ApiOverride(
            new[] { "global::System.Boolean" }, null, "PlayerSettings.Android: Hide Navigation Bar"),
        ["UnityEditor.PlayerSettings.vulkanEnablePreTransform = {0};"] = new ApiOverride(
            new[] { "global::System.Boolean" }, null, "PlayerSettings: Apply display rotation during rendering (Android)"),
        ["UnityEditor.PlayerSettings.iOS.appleEnableAutomaticSigning = {0};"] = new ApiOverride(
            new[] { "global::System.Boolean" }, null, "PlayerSettings.iOS: Automatically Sign"),
        ["UnityEditor.PlayerSettings.iOS.appleDeveloperTeamID = {0};"] = new ApiOverride(
            new[] { "global::System.String" }, null, "PlayerSettings.iOS: Signing Team ID"),
        ["UnityEditor.PlayerSettings.iOS.sdkVersion = {0};"] = new ApiOverride(
            new[] { "global::UnityEditor.iOSSdkVersion" }, null, "PlayerSettings.iOS: Target SDK"),
        ["UnityEditor.PlayerSettings.iOS.targetOSVersionString = {0};"] = new ApiOverride(
            new[] { "global::System.String" }, null, "PlayerSettings.iOS: Target minimum iOS Version"),
    };

    private sealed class ApiOverride
    {
        public ApiOverride(string[] parameterTypes, bool? ignored, string? overrideDisplayName)
        {
            ParameterTypes = parameterTypes;
            Ignored = ignored;
            OverrideDisplayName = overrideDisplayName;
        }

        public string[] ParameterTypes { get; }
        public bool? Ignored { get; }
        public string? OverrideDisplayName { get; }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var output = context.CompilationProvider.Select(static (compilation, _) =>
        {
            if (compilation.AssemblyName != TargetAssemblyName) return null;
            return GenerateSource(compilation);
        });

        context.RegisterSourceOutput(output, static (spc, source) =>
        {
            if (source is null) return;
            spc.AddSource("BuiltInTasks.Dynamic.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static string GenerateSource(Compilation compilation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine($"namespace {EmittedNamespace}");
        sb.AppendLine("{");

        var unityObject = compilation.GetTypeByMetadataName("UnityEngine.Object");

        foreach (var rootName in RootTypeNames)
        {
            var rootType = compilation.GetTypeByMetadataName(rootName);
            if (rootType is null) continue;
            ProcessType(compilation, rootType, sb, unityObject);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void ProcessType(Compilation compilation, INamedTypeSymbol type, StringBuilder sb,
        INamedTypeSymbol? unityObject)
    {
        var classNamePrefix = BuildClassNamePrefix(type);
        var typeExpression = type.ToDisplayString(FullyQualifiedNoKeywords);
        var displayNamePrefix = BuildDisplayNamePrefix(type);

        // Collect candidate properties and methods grouped by emitted class name to handle overloads.
        var groups = new Dictionary<string, CandidateGroup>();

        foreach (var prop in type.GetMembers().OfType<IPropertySymbol>())
        {
            if (!IsCandidateProperty(prop)) continue;
            if (!IsSerializable(prop.Type, unityObject)) continue;
            var className = $"{classNamePrefix}Set{Capitalize(prop.Name)}Task";
            if (!groups.TryGetValue(className, out var g)) groups[className] = g = new CandidateGroup();
            g.Properties.Add(prop);
        }

        var methods = type.GetMembers().OfType<IMethodSymbol>().ToArray();
        foreach (var method in methods)
        {
            if (!IsCandidateMethod(method)) continue;
            if (method.Parameters.Any(p => !IsSerializable(p.Type, unityObject))) continue;
            var className = $"{classNamePrefix}{method.Name}Task";
            if (!groups.TryGetValue(className, out var g)) groups[className] = g = new CandidateGroup();
            g.Methods.Add(method);
        }

        foreach (var kvp in groups)
        {
            var className = kvp.Key;
            var group = kvp.Value;
            if (TypeAlreadyExists(compilation, className)) continue;

            // Prefer a method overload with NamedBuildTarget if there is more than one method candidate.
            if (group.Methods.Count > 1)
            {
                var withNamedBuildTarget = group.Methods.Where(m => m.Parameters.Any(p =>
                    p.Type.ToDisplayString(FullyQualifiedNoKeywords) == "global::UnityEditor.Build.NamedBuildTarget"))
                    .ToList();
                if (withNamedBuildTarget.Count == 1)
                {
                    group.Methods.Clear();
                    group.Methods.Add(withNamedBuildTarget[0]);
                }
                else
                {
                    continue; // ambiguous; skip rather than emit a duplicate
                }
            }

            // If property and method collide, skip property in favor of the (single) method.
            if (group.Properties.Count > 0 && group.Methods.Count == 0)
            {
                EmitPropertyTask(sb, group.Properties[0], classNamePrefix, typeExpression, displayNamePrefix, compilation);
            }
            else if (group.Methods.Count == 1)
            {
                EmitMethodTask(sb, group.Methods[0], methods, type, classNamePrefix, typeExpression,
                    displayNamePrefix, compilation, unityObject);
            }
        }

        foreach (var nested in type.GetTypeMembers())
        {
            if (nested.DeclaredAccessibility != Accessibility.Public) continue;
            if (!nested.IsStatic && nested.TypeKind != TypeKind.Class) continue;
            if (IsObsoleteError(nested)) continue;
            ProcessType(compilation, nested, sb, unityObject);
        }
    }

    private sealed class CandidateGroup
    {
        public List<IPropertySymbol> Properties { get; } = new();
        public List<IMethodSymbol> Methods { get; } = new();
    }

    private static bool IsCandidateProperty(IPropertySymbol prop)
    {
        if (!prop.IsStatic) return false;
        if (prop.IsIndexer) return false;
        if (prop.DeclaredAccessibility != Accessibility.Public) return false;
        if (prop.SetMethod is null) return false;
        if (prop.SetMethod.DeclaredAccessibility != Accessibility.Public) return false;
        if (prop.Type is INamedTypeSymbol named && named.IsGenericType) return false;
        if (IsObsoleteError(prop)) return false;
        return true;
    }

    private static bool IsCandidateMethod(IMethodSymbol method)
    {
        if (!method.IsStatic) return false;
        if (method.DeclaredAccessibility != Accessibility.Public) return false;
        if (method.MethodKind != MethodKind.Ordinary) return false;
        if (method.IsGenericMethod) return false;
        if (!method.Name.StartsWith("Set")) return false;
        if (method.Parameters.Length == 0) return false;
        if (IsObsoleteError(method)) return false;
        return true;
    }

    private static bool IsObsoleteError(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            if (attrName != "System.ObsoleteAttribute") continue;
            return true;
        }
        return false;
    }

    private static bool IsSerializable(ITypeSymbol type, INamedTypeSymbol? unityObject)
    {
        if (TryGetWrapperTypeExpression(type, out _)) return true;
        return IsPlainSerializable(type, unityObject);
    }

    private static bool IsPlainSerializable(ITypeSymbol type, INamedTypeSymbol? unityObject)
    {
        if (type is IArrayTypeSymbol arr) return IsPlainSerializable(arr.ElementType, unityObject);

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_String:
                return true;
        }

        if (type.TypeKind == TypeKind.Enum) return true;
        if (unityObject is not null && InheritsFrom(type, unityObject)) return true;
        return false;
    }

    private static bool TryGetWrapperTypeExpression(ITypeSymbol type, out string wrapperTypeExpression)
    {
        var name = type.ToDisplayString();
        foreach (var (target, wrapper) in Wrappers)
        {
            if (name == target)
            {
                wrapperTypeExpression = wrapper;
                return true;
            }
        }
        wrapperTypeExpression = string.Empty;
        return false;
    }

    private static bool InheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
            if (SymbolEqualityComparer.Default.Equals(t, baseType))
                return true;
        return false;
    }

    private static string BuildClassNamePrefix(INamedTypeSymbol type)
    {
        var stack = new Stack<string>();
        for (var t = type; t is not null; t = t.ContainingType)
            stack.Push(t.Name);
        return string.Concat(stack);
    }

    private static string BuildDisplayNamePrefix(INamedTypeSymbol type)
    {
        var stack = new Stack<string>();
        for (var t = type; t is not null && t.Name != "PlayerSettings" && t.Name != "EditorUserBuildSettings"; t = t.ContainingType)
            stack.Push(t.Name);

        var root = type;
        while (root.ContainingType is not null) root = root.ContainingType;
        var parts = new List<string> { root.Name };
        parts.AddRange(stack);
        return string.Join(".", parts);
    }

    private static void EmitPropertyTask(StringBuilder sb, IPropertySymbol prop, string classNamePrefix,
        string typeExpression, string displayNamePrefix, Compilation compilation)
    {
        var capitalizedName = Capitalize(prop.Name);
        var className = $"{classNamePrefix}Set{capitalizedName}Task";
        if (TypeAlreadyExists(compilation, className)) return;

        var paramTypeFqn = prop.Type.ToDisplayString(FullyQualifiedNoKeywords);
        var setterExpression = $"{typeExpression}.{prop.Name} = {{0}};";

        var overrideInfo = LookupOverride(setterExpression, new[] { paramTypeFqn });
        if (overrideInfo is { Ignored: true }) return;

        var displayName = overrideInfo?.OverrideDisplayName
                          ?? $"{displayNamePrefix}: {ToNiceLabelName(capitalizedName)}";
        var propertyNameAttr = $"{displayNamePrefix}.{capitalizedName}";

        var parameters = new[]
        {
            new WeaveParam(prop.Type, prop.Name, paramTypeFqn, isOutput: true, index: 0),
        };
        var rooted = WeaveRoot(parameters);

        EmitTask(sb, className, rooted, displayName, propertyNameAttr,
            setterTemplate: setterExpression,
            getterTemplate: prop.GetMethod is not null ? $"{{0}} = {typeExpression}.{prop.Name};" : null);
    }

    private static void EmitMethodTask(StringBuilder sb, IMethodSymbol method, IMethodSymbol[] siblings,
        INamedTypeSymbol containingType, string classNamePrefix, string typeExpression, string displayNamePrefix,
        Compilation compilation, INamedTypeSymbol? unityObject)
    {
        var className = $"{classNamePrefix}{method.Name}Task";
        if (TypeAlreadyExists(compilation, className)) return;

        var setterExpression = BuildMethodSetterExpression(typeExpression, method);
        var paramTypeFqns = method.Parameters.Select(p => p.Type.ToDisplayString(FullyQualifiedNoKeywords)).ToArray();

        var overrideInfo = LookupOverride(setterExpression, paramTypeFqns);
        if (overrideInfo is { Ignored: true }) return;

        var (getterTemplate, outputParameterIndices) =
            FindMatchingGetter(method, siblings, typeExpression, unityObject);

        var weaveParams = method.Parameters.Select((p, i) => new WeaveParam(
            p.Type,
            p.Name,
            paramTypeFqns[i],
            isOutput: outputParameterIndices?.Contains(i) ?? true,
            index: i)).ToList();

        var rooted = WeaveRoot(weaveParams);

        var displayLabel = method.Name.StartsWith("Set") ? method.Name.Substring(3) : method.Name;
        var displayName = overrideInfo?.OverrideDisplayName
                          ?? $"{displayNamePrefix}: {ToNiceLabelName(displayLabel)}";
        var propertyNameAttr = $"{displayNamePrefix}.{method.Name}()";

        EmitTask(sb, className, rooted, displayName, propertyNameAttr,
            setterTemplate: setterExpression,
            getterTemplate: getterTemplate);
    }

    private static string BuildMethodSetterExpression(string typeExpression, IMethodSymbol method)
    {
        var args = string.Join(", ", Enumerable.Range(0, method.Parameters.Length).Select(n => $"{{{n}}}"));
        return $"{typeExpression}.{method.Name}({args});";
    }

    private static (string? getterExpression, IReadOnlyList<int>? outputParameterIndices) FindMatchingGetter(
        IMethodSymbol setter, IMethodSymbol[] siblings, string typeExpression, INamedTypeSymbol? unityObject)
    {
        var expectedGetterName = $"Get{setter.Name.Substring(3)}";

        foreach (var getter in siblings)
        {
            if (!getter.IsStatic) continue;
            if (getter.DeclaredAccessibility != Accessibility.Public) continue;
            if (getter.MethodKind != MethodKind.Ordinary) continue;
            if (getter.IsGenericMethod) continue;
            if (getter.Name != expectedGetterName) continue;
            if (IsObsoleteError(getter)) continue;

            // Match each getter parameter to a setter parameter by name and type.
            var getterToSetterIndex = new int[getter.Parameters.Length];
            var setterIndexUsed = new bool[setter.Parameters.Length];
            var outputParameterIndices = new List<int>();
            var matchedAll = true;

            for (var i = 0; i < getter.Parameters.Length; i++)
            {
                var gp = getter.Parameters[i];
                var found = -1;
                for (var j = 0; j < setter.Parameters.Length; j++)
                {
                    if (setterIndexUsed[j]) continue;
                    var sp = setter.Parameters[j];
                    if (sp.Name != gp.Name) continue;
                    if (!SymbolEqualityComparer.Default.Equals(sp.Type, gp.Type)) continue;
                    found = j;
                    break;
                }

                if (found == -1)
                {
                    matchedAll = false;
                    break;
                }
                setterIndexUsed[found] = true;
                getterToSetterIndex[i] = found;
                if (gp.RefKind is RefKind.Out or RefKind.Ref) outputParameterIndices.Add(found);
            }

            if (!matchedAll) continue;

            // Find return parameter (setter parameter not consumed by getter).
            var returnParameterIndex = -1;
            for (var j = 0; j < setter.Parameters.Length; j++)
            {
                if (setterIndexUsed[j]) continue;
                if (returnParameterIndex != -1)
                {
                    returnParameterIndex = -2; // multiple unmatched → invalid
                    break;
                }
                if (SymbolEqualityComparer.Default.Equals(setter.Parameters[j].Type, getter.ReturnType))
                    returnParameterIndex = j;
                else
                    returnParameterIndex = -2;
            }

            if (returnParameterIndex == -2) continue;
            if (getter.ReturnType.SpecialType != SpecialType.System_Void && returnParameterIndex == -1) continue;

            if (returnParameterIndex >= 0) outputParameterIndices.Add(returnParameterIndex);

            var argParts = new string[getter.Parameters.Length];
            for (var i = 0; i < getter.Parameters.Length; i++)
            {
                var refKind = getter.Parameters[i].RefKind;
                var placeholder = $"{{{getterToSetterIndex[i]}}}";
                argParts[i] = refKind switch
                {
                    RefKind.Ref => $"ref {placeholder}",
                    RefKind.Out => $"out {placeholder}",
                    _ => placeholder,
                };
            }

            var prefix = returnParameterIndex >= 0 ? $"{{{returnParameterIndex}}} = " : string.Empty;
            var getterExpression = $"{prefix}{typeExpression}.{getter.Name}({string.Join(", ", argParts)});";
            return (getterExpression, outputParameterIndices);
        }

        // Property-style getter: if setter is actually a property's set, that case is handled separately.
        return (null, null);
    }

    private static ApiOverride? LookupOverride(string setterExpression, string[] paramTypeFqns)
    {
        const string globalPrefix = "global::";
        var key = setterExpression.StartsWith(globalPrefix)
            ? setterExpression.Substring(globalPrefix.Length)
            : setterExpression;
        if (!ApiOverrides.TryGetValue(key, out var ovr)) return null;
        if (!ovr.ParameterTypes.SequenceEqual(paramTypeFqns)) return null;
        return ovr;
    }

    // ===== Weaving =====================================================

    private sealed class EmitContext
    {
        private int _counter;
        public List<(string Name, IReadOnlyList<TupleMember> Members)> TupleContainers { get; } = new();

        public string AllocateTupleContainer(IReadOnlyList<TupleMember> members)
        {
            var name = $"__BUILDMAGIC__AnonymousTupleContainer__{_counter++}";
            TupleContainers.Add((name, members));
            return name;
        }
    }

    private readonly struct TupleMember
    {
        public TupleMember(string name, string serializableTypeExpression, string realTypeExpression)
        {
            Name = name;
            SerializableTypeExpression = serializableTypeExpression;
            RealTypeExpression = realTypeExpression;
        }

        public string Name { get; }
        public string SerializableTypeExpression { get; }
        public string RealTypeExpression { get; }
    }

    private sealed class WeaveParam
    {
        public WeaveParam(ITypeSymbol typeSymbol, string name, string fullyQualifiedTypeExpression, bool isOutput, int index)
        {
            TypeSymbol = typeSymbol;
            Name = name;
            TypeExpression = fullyQualifiedTypeExpression;
            IsOutput = isOutput;
            Index = index;
        }

        public ITypeSymbol TypeSymbol { get; }
        public string Name { get; }
        public string TypeExpression { get; }
        public bool IsOutput { get; }
        public int Index { get; }
    }

    private abstract class Weaved
    {
        public abstract bool HasTuple { get; }
        public abstract string ToTypeExpression();
        public abstract string ToSerializableTypeExpression(EmitContext ctx);
        public abstract (string Expression, bool NeedsTransform) ToBuildExpression(string sourceExpression);
        public abstract string ExtractParameter(string containerExpression, string next, ref int localCounter);

        // For applier emission. (getter, setter) container expressions thread the in/out.
        public abstract string ToParameter(string containerGetter, string containerSetter, string next,
            ref int localCounter, int indent);
    }

    private sealed class WeavedSingle : Weaved
    {
        public WeaveParam Parameter { get; }

        public WeavedSingle(WeaveParam parameter) { Parameter = parameter; }

        public override bool HasTuple => false;

        public override string ToTypeExpression() => Parameter.TypeExpression;

        public override string ToSerializableTypeExpression(EmitContext ctx)
        {
            return TryGetWrapperTypeExpression(Parameter.TypeSymbol, out var wrapper)
                ? wrapper
                : Parameter.TypeExpression;
        }

        public override (string, bool) ToBuildExpression(string sourceExpression)
        {
            if (TryGetWrapperTypeExpression(Parameter.TypeSymbol, out _))
                return ($"({Parameter.TypeExpression}){sourceExpression}", true);
            return (sourceExpression, false);
        }

        public override string ExtractParameter(string containerExpression, string next, ref int localCounter)
        {
            return $"        {next.Replace($"{{{Parameter.Index}}}", containerExpression)}";
        }

        public override string ToParameter(string containerGetter, string containerSetter, string next,
            ref int localCounter, int indent)
        {
            var token = Parameter.IsOutput ? containerSetter : containerGetter;
            return $"{Indent(indent)}{next.Replace($"{{{Parameter.Index}}}", token)}";
        }
    }

    private sealed class WeavedDictionary : Weaved
    {
        public Weaved Key { get; }
        public Weaved Value { get; }

        public WeavedDictionary(Weaved key, Weaved value) { Key = key; Value = value; }

        public override bool HasTuple => Key.HasTuple || Value.HasTuple;

        public override string ToTypeExpression()
            => $"global::System.Collections.Generic.IReadOnlyDictionary<{Key.ToTypeExpression()}, {Value.ToTypeExpression()}>";

        public override string ToSerializableTypeExpression(EmitContext ctx)
            => $"global::BuildMagicEditor.SerializableDictionary<{Key.ToSerializableTypeExpression(ctx)}, {Value.ToSerializableTypeExpression(ctx)}>";

        public override (string, bool) ToBuildExpression(string sourceExpression)
        {
            var (keyExp, keyNeedsTransform) = Key.ToBuildExpression("kvp.Key");
            var (valExp, valNeedsTransform) = Value.ToBuildExpression("kvp.Value");
            if (!keyNeedsTransform && !valNeedsTransform)
                return ($"{sourceExpression}.ToDictionary()", true);
            var realKey = Key.ToTypeExpression();
            var realValue = Value.ToTypeExpression();
            return (
                $"(global::System.Collections.Generic.IReadOnlyDictionary<{realKey}, {realValue}>)global::System.Linq.Enumerable.ToDictionary({sourceExpression}.ToDictionary(), kvp => {keyExp}, kvp => {valExp})",
                true);
        }

        public override string ExtractParameter(string containerExpression, string next, ref int localCounter)
        {
            var localKey = $"__BUILDMAGIC__{localCounter++}";
            var localValue = $"__BUILDMAGIC__{localCounter++}";
            var inner = Key.ExtractParameter(localKey, Value.ExtractParameter(localValue, next, ref localCounter), ref localCounter);
            return
$$"""
        foreach (var ({{localKey}}, {{localValue}}) in {{containerExpression}})
        {
{{inner}}
        }
""";
        }

        public override string ToParameter(string containerGetter, string containerSetter, string next,
            ref int localCounter, int indent)
        {
            var dictionaryName = $"__BUILDMAGIC__{localCounter++}";
            var localKey = $"__BUILDMAGIC__{localCounter++}";
            var localValue = $"__BUILDMAGIC__{localCounter++}";
            var indentStr = Indent(indent);

            var inner = Key.ToParameter(localKey, $"<INVALID-{localKey}>",
                Value.ToParameter(localValue, $"{dictionaryName}[{localKey}]", next, ref localCounter, indent + 1),
                ref localCounter, indent + 1);

            return
$$"""
{{indentStr}}var {{dictionaryName}} = {{containerGetter}};
{{indentStr}}{{dictionaryName}} = new();
{{indentStr}}foreach (var ({{localKey}}, {{localValue}}) in {{containerGetter}})
{{indentStr}}{
{{inner}}
{{indentStr}}}
{{indentStr}}{{containerSetter}} = {{dictionaryName}};
""";
        }
    }

    private sealed class WeavedTuple : Weaved
    {
        public IReadOnlyList<(Weaved Item, string Name)> Items { get; }
        public WeavedTuple(IReadOnlyList<(Weaved Item, string Name)> items) { Items = items; }

        public override bool HasTuple => true;

        public override string ToTypeExpression()
            => $"({string.Join(", ", Items.Select(i => $"{i.Item.ToTypeExpression()} {i.Name}"))})";

        public override string ToSerializableTypeExpression(EmitContext ctx)
        {
            var members = Items.Select(i => new TupleMember(
                i.Name,
                i.Item.ToSerializableTypeExpression(ctx),
                i.Item.ToTypeExpression())).ToList();
            return ctx.AllocateTupleContainer(members);
        }

        public override (string, bool) ToBuildExpression(string sourceExpression)
        {
            var inner = Items.Select(i => i.Item.ToBuildExpression($"{sourceExpression}.{i.Name}")).ToList();
            if (inner.All(x => !x.NeedsTransform))
                return (sourceExpression, false);
            var tup = string.Join(", ", inner.Select(x => x.Expression));
            return ($"({tup})", true);
        }

        public override string ExtractParameter(string containerExpression, string next, ref int localCounter)
        {
            var cursor = next;
            foreach (var (item, name) in Items)
                cursor = item.ExtractParameter(name, cursor, ref localCounter);
            return
$$"""
        var ({{string.Join(", ", Items.Select(i => i.Name))}}) = {{containerExpression}};
{{cursor}}
""";
        }

        public override string ToParameter(string containerGetter, string containerSetter, string next,
            ref int localCounter, int indent)
        {
            var cursor = next;
            foreach (var (item, name) in Items)
                cursor = item.ToParameter($"<INVALID-{name}>", $"var {name}", cursor, ref localCounter, indent);
            return
$$"""
{{cursor}}
{{Indent(indent)}}{{containerSetter}} = ({{string.Join(", ", Items.Select(i => i.Name))}});
""";
        }
    }

    private sealed class WeavedRoot
    {
        public Weaved Item { get; }
        public string FieldName { get; }
        public WeavedRoot(Weaved item, string fieldName) { Item = item; FieldName = fieldName; }
    }

    private static List<WeavedRoot> WeaveRoot(IReadOnlyList<WeaveParam> parameters)
    {
        var indexed = parameters.Select((p, i) => (param: p, index: i)).ToList();
        var weaved = Weave(indexed);
        return weaved.Select(w => new WeavedRoot(w.result, w.source.Name)).ToList();
    }

    private static List<(Weaved result, WeaveParam source)> Weave(List<(WeaveParam param, int index)> parameters)
    {
        if (parameters.Count == 1)
            return new List<(Weaved, WeaveParam)>
            {
                (new WeavedSingle(parameters[0].param), parameters[0].param),
            };

        var keyIndex = -1;
        foreach (var keyType in DictionaryKeyTypes)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].param.TypeExpression == keyType)
                {
                    keyIndex = i;
                    break;
                }
            }
            if (keyIndex != -1) break;
        }

        if (keyIndex == -1)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                if (!parameters[i].param.IsOutput)
                {
                    keyIndex = i;
                    break;
                }
            }
        }

        if (keyIndex == -1)
        {
            return parameters.Select(p => ((Weaved)new WeavedSingle(p.param), p.param)).ToList();
        }

        var key = new WeavedSingle(parameters[keyIndex].param);

        var values = parameters.ToList();
        values.RemoveAt(keyIndex);

        var weavedValues = Weave(values);
        Weaved value = weavedValues.Count == 1
            ? weavedValues[0].result
            : new WeavedTuple(weavedValues.Select(v => (v.result, v.source.Name)).ToList());

        return new List<(Weaved, WeaveParam)>
        {
            (new WeavedDictionary(key, value), parameters[keyIndex].param),
        };
    }

    // ===== Emission ====================================================

    private static void EmitTask(StringBuilder sb, string className, List<WeavedRoot> rootParameters,
        string displayName, string propertyNameAttr, string setterTemplate, string? getterTemplate)
    {
        var ctx = new EmitContext();
        var serializableTypeExpressions = rootParameters
            .Select(p => p.Item.ToSerializableTypeExpression(ctx))
            .ToList();

        var needDedicatedParameterType = rootParameters.Count > 1 || ctx.TupleContainers.Count > 0;
        var parametersClassName = $"{className}Parameters";
        var taskFqn = $"global::{EmittedNamespace}.{className}";
        var parameterTypeExpression = needDedicatedParameterType
            ? $"global::{EmittedNamespace}.{parametersClassName}"
            : serializableTypeExpressions[0];

        EmitTaskClass(sb, className, rootParameters, setterTemplate);
        EmitConfigurationClass(sb, className, taskFqn, parameterTypeExpression, rootParameters,
            getterTemplate, displayName, propertyNameAttr, needDedicatedParameterType);
        if (needDedicatedParameterType)
            EmitParametersClass(sb, parametersClassName, rootParameters, serializableTypeExpressions, ctx);
        EmitBuilderClass(sb, className, taskFqn, parameterTypeExpression, rootParameters, needDedicatedParameterType);
    }

    private static void EmitTaskClass(StringBuilder sb, string className, List<WeavedRoot> rootParameters,
        string setterTemplate)
    {
        sb.AppendLine($"public class {className} : global::BuildMagicEditor.BuildTaskBase<global::BuildMagicEditor.IPreBuildContext>");
        sb.AppendLine("{");

        var ctorParams = string.Join(", ",
            rootParameters.Select(p => $"{p.Item.ToTypeExpression()} {p.FieldName}"));
        sb.AppendLine($"    public {className}({ctorParams})");
        sb.AppendLine("    {");
        foreach (var p in rootParameters)
            sb.AppendLine($"        this.{p.FieldName} = {p.FieldName};");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void Run(global::BuildMagicEditor.IPreBuildContext context)");
        sb.AppendLine("    {");

        var counter = 0;
        var cursor = setterTemplate;
        foreach (var p in rootParameters)
            cursor = p.Item.ExtractParameter($"this.{p.FieldName}", cursor, ref counter);
        sb.AppendLine(cursor);

        sb.AppendLine("    }");
        foreach (var p in rootParameters)
            sb.AppendLine($"    private readonly {p.Item.ToTypeExpression()} {p.FieldName};");
        sb.AppendLine("}");
    }

    private static void EmitConfigurationClass(StringBuilder sb, string className, string taskFqn,
        string parameterTypeExpression, List<WeavedRoot> rootParameters, string? getterTemplate,
        string displayName, string propertyNameAttr, bool needDedicatedParameterType)
    {
        sb.AppendLine($"[global::BuildMagicEditor.BuildConfiguration(DisplayName = @\"{displayName}\", PropertyName = @\"{propertyNameAttr}\")]");
        sb.AppendLine("[global::System.Serializable]");
        var baseList = $"global::BuildMagicEditor.BuildConfigurationBase<{taskFqn}, {parameterTypeExpression}>";
        if (getterTemplate is not null)
            baseList += ", global::BuildMagicEditor.IProjectSettingApplier";
        sb.AppendLine($"public partial class {className}Configuration : {baseList}");
        sb.AppendLine("{");

        if (getterTemplate is not null)
            EmitApplyProjectSetting(sb, parameterTypeExpression, rootParameters, getterTemplate,
                needDedicatedParameterType);

        sb.AppendLine("}");
    }

    private static void EmitApplyProjectSetting(StringBuilder sb, string parameterTypeExpression,
        List<WeavedRoot> rootParameters, string getterTemplate, bool needDedicatedParameterType)
    {
        sb.AppendLine("    void global::BuildMagicEditor.IProjectSettingApplier.ApplyProjectSetting()");
        sb.AppendLine("    {");

        if (needDedicatedParameterType)
        {
            sb.AppendLine($"        var __BUILDMAGIC__newParams = new {parameterTypeExpression}();");
            if (rootParameters.Count == 1)
            {
                var p = rootParameters[0];
                var counter = 0;
                var cursor = p.Item.ToParameter($"this.Value.{p.FieldName}",
                    $"__BUILDMAGIC__newParams.{p.FieldName}", getterTemplate, ref counter, 2);
                sb.AppendLine(cursor);
            }
            else
            {
                // Multi-root: all WeavedSingle leaves. Replace each placeholder in the getter template
                // with the destination field reference; emit as a single statement.
                var cursor = getterTemplate;
                foreach (var p in rootParameters)
                {
                    if (p.Item is WeavedSingle ws)
                        cursor = cursor.Replace($"{{{ws.Parameter.Index}}}",
                            $"__BUILDMAGIC__newParams.{p.FieldName}");
                }
                sb.AppendLine($"        {cursor}");
            }
            sb.AppendLine("        this.Value = __BUILDMAGIC__newParams;");
        }
        else
        {
            var p = rootParameters[0];
            var counter = 0;
            var cursor = p.Item.ToParameter("this.Value", "var __BUILDMAGIC__newValue",
                getterTemplate, ref counter, 2);
            sb.AppendLine(cursor);
            sb.AppendLine("        this.Value = __BUILDMAGIC__newValue;");
        }

        sb.AppendLine("    }");
    }

    private static void EmitParametersClass(StringBuilder sb, string parametersClassName,
        List<WeavedRoot> rootParameters, List<string> serializableTypeExpressions, EmitContext ctx)
    {
        sb.AppendLine("[global::System.Serializable]");
        sb.AppendLine($"public class {parametersClassName}");
        sb.AppendLine("{");
        for (var i = 0; i < rootParameters.Count; i++)
            sb.AppendLine($"    public {serializableTypeExpressions[i]} {rootParameters[i].FieldName};");

        foreach (var (name, members) in ctx.TupleContainers)
            EmitTupleContainer(sb, name, members);

        sb.AppendLine("}");
    }

    private static void EmitTupleContainer(StringBuilder sb, string containerName,
        IReadOnlyList<TupleMember> members)
    {
        sb.AppendLine("    [global::System.Serializable]");
        sb.AppendLine($"    public struct {containerName}");
        sb.AppendLine("    {");
        foreach (var m in members)
            sb.AppendLine($"        public {m.SerializableTypeExpression} {m.Name};");

        var realTupleSig = string.Join(", ", members.Select(m => $"{m.RealTypeExpression} {m.Name}"));
        var fieldAccess = string.Join(", ", members.Select(m => $"source.{m.Name}"));
        sb.AppendLine($"        public static implicit operator ({realTupleSig})({containerName} source)");
        sb.AppendLine("        {");
        sb.AppendLine($"            return ({fieldAccess});");
        sb.AppendLine("        }");

        var valueTupleArgs = string.Join(", ", members.Select(m => m.RealTypeExpression));
        var resultLhs = string.Join(", ", members.Select(m => $"result.{m.Name}"));
        var sourceItems = string.Join(", ", Enumerable.Range(0, members.Count).Select(i => $"source.Item{i + 1}"));
        sb.AppendLine($"        public static implicit operator {containerName}(global::System.ValueTuple<{valueTupleArgs}> source)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var result = new {containerName}();");
        sb.AppendLine($"            ({resultLhs}) = ({sourceItems});");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void EmitBuilderClass(StringBuilder sb, string className, string taskFqn,
        string parameterTypeExpression, List<WeavedRoot> rootParameters, bool needDedicatedParameterType)
    {
        sb.AppendLine($"[global::BuildMagicEditor.BuildTaskBuilder(typeof({taskFqn}), typeof({parameterTypeExpression}))]");
        sb.AppendLine($"public class {className}Builder : global::BuildMagicEditor.BuildTaskBuilderBase<{taskFqn}, {parameterTypeExpression}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public override {taskFqn} Build({parameterTypeExpression} value)");
        sb.AppendLine("    {");
        var ctorArgs = string.Join(", ", rootParameters.Select(p =>
        {
            var source = needDedicatedParameterType ? $"value.{p.FieldName}" : "value";
            return p.Item.ToBuildExpression(source).Expression;
        }));
        sb.AppendLine($"        return new {taskFqn}({ctorArgs});");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private static bool TypeAlreadyExists(Compilation compilation, string className)
    {
        var fqn = $"{EmittedNamespace}.{className}";
        return compilation.GetTypeByMetadataName(fqn) is not null;
    }

    private static string Indent(int level) => new(' ', level * 4);

    private static string Capitalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (char.IsUpper(name[0])) return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    private static readonly string[] KnownNames =
    {
        "iOS", "iPad", "iPod", "iPhone", "visionOS", "x86", "x64", "ARM", "Il2Cpp"
    };

    private static string ToNiceLabelName(string src)
    {
        if (string.IsNullOrEmpty(src)) return src;
        var sb = new StringBuilder(src.Length * 2);
        var prev = ' ';
        var prevDivided = false;
        var i = 0;
        while (i < src.Length)
        {
            var prevDividedCurrent = prevDivided;
            prevDivided = false;

            if (prevDividedCurrent)
            {
                sb.Append(' ');
                prev = ' ';
            }

            var matched = false;
            foreach (var kn in KnownNames)
            {
                if (i + kn.Length > src.Length) continue;
                if (string.CompareOrdinal(src, i, kn, 0, kn.Length) != 0) continue;
                if (prev != ' ')
                {
                    sb.Append(' ');
                    prev = ' ';
                }
                sb.Append(kn);
                prev = kn[kn.Length - 1];
                i += kn.Length;
                prevDivided = true;
                matched = true;
                break;
            }

            if (matched) continue;

            var c = src[i];
            var isUpper = c is >= 'A' and <= 'Z';
            var isDigit = c is >= '0' and <= '9';
            var prevUpper = prev is >= 'A' and <= 'Z';
            var prevDigit = prev is >= '0' and <= '9';

            if (prev != ' ' && ((isDigit && !prevDigit && !prevUpper) || (isUpper && !prevUpper)))
            {
                sb.Append(' ');
                prev = ' ';
            }

            if (prev == ' ' && c is >= 'a' and <= 'z') c = (char)(c - ('a' - 'A'));

            sb.Append(c);
            prev = c;
            i++;
        }
        return sb.ToString();
    }
}
