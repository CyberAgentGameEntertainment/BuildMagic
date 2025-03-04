// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using ZLogger;
using ILogger = Microsoft.Build.Framework.ILogger;

namespace BuildMagic.BuiltinTaskGenerator;

public class UnityApiAnalyzer(
    ILogger<UnityApiAnalyzer> logger,
    ILogger<UnityApiAnalyzer.MSBuildLogger> msBuildLogger,
    UnityCsReferenceRepository unityCsReferenceRepository,
    AnalysisLibrary library)
{
    private static readonly SymbolDisplayFormat DisplayFormatForType = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.IncludeTypeParameters);

    private CancellationTokenSource _cts;

    /// <summary>
    ///     Extract APIs from UnityCsReference for each version
    /// </summary>
    /// <param name="netfxBcl"></param>
    /// <param name="versionFilter"></param>
    /// <param name="forceAnalyze"></param>
    /// <param name="ct"></param>
    public async Task RunAsync(string? netfxBcl, Func<UnityVersion, bool> versionFilter, bool forceAnalyze,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(netfxBcl)) logger.ZLogInformation($"Using .NET Framework BCL: {netfxBcl}");

        // csproj files in UnityCsReference prior to Unity 2021 require .NET Framework 4.7.1 Development Pack
        // .NET Framework Development Pack is only provided for Windows, but it can be replaced with Mono or other BCL in this way
        using var workspace = string.IsNullOrEmpty(netfxBcl)
            ? MSBuildWorkspace.Create()
            : MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                {
                    "_TargetFrameworkDirectories",
                    $"{netfxBcl};{netfxBcl}/Facades"
                },
                {
                    "_FullFrameworkReferenceAssemblyPaths",
                    $"{netfxBcl};{netfxBcl}/Facades"
                }
            });

        unityCsReferenceRepository.Fetch();

        foreach (var version in unityCsReferenceRepository.GetVersions()
                     .Where(versionFilter).Order())
        {
            var (result, isNew) = await library.GetForVersionAsync(version, forceAnalyze, ct);

            if (!isNew) continue;

            logger.ZLogInformation($"Processing: {version}");

            unityCsReferenceRepository.Checkout(version);

            var solutionPath =
                Path.Combine(unityCsReferenceRepository.Path, "Projects", "CSharp", "UnityReferenceSource.sln");

            logger.ZLogInformation($"Opening solution: {solutionPath}");
            var solution = await workspace.OpenSolutionAsync(solutionPath, new MSBuildLogger(msBuildLogger), null, ct);

            logger.ZLogInformation($"Solution loaded: {solution.Projects.Count()} projects");

            foreach (var project in solution.Projects)
            {
                if (project.Name is not "UnityEditor")
                {
                    logger.ZLogInformation($"Skipping project: {project.Name}");
                    continue;
                }

                logger.ZLogInformation($"Processing project: {project.Name}");

                var compilation = await project.GetCompilationAsync(ct);

                if (compilation == null)
                {
                    logger.ZLogWarning($"Compilation for {project.Name} is null.");
                    continue;
                }

                if (compilation is CSharpCompilation cSharpCompilation)
                    logger.ZLogInformation($"LanguageVersion: {cSharpCompilation.LanguageVersion.ToString()}");


                var numErrors = compilation.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);

                if (numErrors > 0)
                {
                    foreach (var diagnostic in compilation.GetDiagnostics()
                                 .Where(d => d.Severity == DiagnosticSeverity.Error))
                        logger.ZLogInformation($"{diagnostic}");
                    logger.ZLogWarning($"{numErrors} compilation errors");
                }

                var playerSettingsEditor = compilation.GetTypeByMetadataName("UnityEditor.PlayerSettingsEditor");

                if (playerSettingsEditor == null)
                {
                    logger.ZLogWarning($"Failed to find UnityEditor.PlayerSettingsEditor");
                    continue;
                }

                var playerSettings = compilation.GetTypeByMetadataName("UnityEditor.PlayerSettings");

                if (playerSettings == null)
                {
                    logger.ZLogWarning($"Failed to find UnityEditor.PlayerSettings");
                    continue;
                }

                var editorUserBuildSettings = compilation.GetTypeByMetadataName("UnityEditor.EditorUserBuildSettings");

                if (editorUserBuildSettings == null)
                {
                    logger.ZLogWarning($"Failed to find UnityEditor.EditorUserBuildSettings");
                    continue;
                }

                var editorGuiAnalyzer = new UnityEditorGuiAnalyzer(compilation, playerSettingsEditor);

                foreach (var (key, value) in editorGuiAnalyzer.LoweredPropertyNameAndLabel)
                    logger.ZLogInformation($"{key}: {value}");

                var categories = result.Categories;
                if (!categories.TryGetValue("PlayerSettings", out var playerSettingsCategory))
                    playerSettingsCategory =
                        categories["PlayerSettings"] = new ApiCategory();

                if (!categories.TryGetValue("EditorUserBuildSettings", out var editorUserBuildSettingsCategory))
                    editorUserBuildSettingsCategory =
                        categories["EditorUserBuildSettings"] = new ApiCategory();

                ProcessType(playerSettingsCategory, playerSettings, "", "");
                ProcessType(editorUserBuildSettingsCategory, editorUserBuildSettings, "", "");

                void ProcessType(ApiCategory category,　ITypeSymbol type, string expectedName, string propertyName)
                {
                    var typeExpression = type.ToDisplayString(DisplayFormatForType);

                    var name = ToTitleCase(type.Name);
                    expectedName += name;
                    propertyName = !string.IsNullOrEmpty(propertyName) ? $"{propertyName}.{name}" : name;

                    var typeObsoleteAttribute = type.GetAttributes()
                        .FirstOrDefault(attr =>
                        {
                            if (attr.AttributeClass is IErrorTypeSymbol errorType)
                                return errorType.MetadataName is "Obsolete" or "ObsoleteAttribute";
                            return attr.AttributeClass?.MetadataName == "ObsoleteAttribute";
                        });

                    if (typeObsoleteAttribute is { ConstructorArguments.Length: 2 } &&
                        (bool)typeObsoleteAttribute.ConstructorArguments[1].Value)
                        // obsolete with error=true
                        return;

                    var isTypeObsolete = typeObsoleteAttribute != null;

                    var publicMembers = type.GetMembers()
                        .Where(m => m.DeclaredAccessibility == Accessibility.Public);

                    foreach (var member in publicMembers)
                    {
                        if (member is ITypeSymbol nestedType)
                            ProcessType(category, nestedType, expectedName, propertyName);

                        if (!member.IsStatic) continue;

                        var obsoleteAttribute = member.GetAttributes()
                            .FirstOrDefault(attr =>
                            {
                                if (attr.AttributeClass is IErrorTypeSymbol errorType)
                                    return errorType.MetadataName is "Obsolete" or "ObsoleteAttribute";
                                return attr.AttributeClass?.MetadataName == "ObsoleteAttribute";
                            });

                        if (obsoleteAttribute is { ConstructorArguments.Length: 2 } &&
                            (bool)obsoleteAttribute.ConstructorArguments[1].Value)
                            // obsolete with error=true
                            continue;

                        // Parse UnityUpgradable
                        // (UnityUpgradable) -> [<Assembly (optional)>] <Member>
                        UnityUpgradableData? unityUpgradableData = null;
                        if (obsoleteAttribute is { ConstructorArguments: [{ Value: string message }, ..] })
                        {
                            const string hint = "(UnityUpgradable) -> ";
                            var index = message.IndexOf(hint, StringComparison.OrdinalIgnoreCase);
                            if (index != -1)
                            {
                                var content = message.AsSpan()[index..][hint.Length..];
                                string? assemblyName = null;
                                if (content.Length > 0 && content[0] == '[')
                                {
                                    // parse assembly name
                                    var closer = content.IndexOf(']');
                                    if (closer == -1) goto BREAK;
                                    assemblyName = content[1..closer].ToString();
                                    content = content[closer..][1..];

                                    // NOTE: We have to ignore APIs moved to platform dependent assemblies because they may cause compilation errors.
                                    // It is done heuristically by checking if the assembly name starts with "UnityEditor." and ends with ".Extensions".
                                    if (assemblyName.StartsWith("UnityEditor.", StringComparison.Ordinal) &&
                                        assemblyName.EndsWith(".Extensions")) continue;
                                }

                                var memberName = content.Trim().ToString().Replace('/', '.');

                                unityUpgradableData = new UnityUpgradableData(assemblyName, memberName);
                            }

                            BREAK: ;
                        }

                        var isObsolete = isTypeObsolete || obsoleteAttribute != null;

                        // API updater-applied member info
                        string appliedMemberName;
                        if (unityUpgradableData?.MemberName is { } upgradedMemberName)
                        {
                            if (upgradedMemberName?.Contains('.', StringComparison.Ordinal) ?? false)
                                // fullname
                                appliedMemberName = $"global::{upgradedMemberName}";
                            else
                                appliedMemberName = $"{typeExpression}.{upgradedMemberName}";
                        }
                        else
                        {
                            appliedMemberName = $"{typeExpression}.{member.Name}";
                        }

                        ApiData data;
                        if (member is IMethodSymbol setterMethod)
                        {
                            if (setterMethod.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet ||
                                setterMethod.IsGenericMethod ||
                                !setterMethod.Name.StartsWith("Set"))
                                continue; // methods starts with "Set" are included

                            // Is all the parameters serializable?
                            if (setterMethod.Parameters.Any(p =>
                                    !p.Type.BuiltinSerializationWrapperExists(out _) &&
                                    !TypeUtils.IsSerializableFieldType(p.Type))) continue;

                            var setterExpression =
                                $"{appliedMemberName}({string.Join(", ", Enumerable.Range(0, setterMethod.Parameters.Length).Select(n => $"{{{n}}}"))});";

                            // getter expression
                            // find getter method
                            var (getterExpression, outputParameterIndices) = publicMembers.OfType<IMethodSymbol>()
                                .SelectWhereNotNull<IMethodSymbol, ValueTuple<string, IEnumerable<int>>>(getter =>
                                {
                                    if (getter.Name != $"Get{setterMethod.Name[3..]}") return null;

                                    var getterParametersSetterParameterOrdered =
                                        new IParameterSymbol?[setterMethod.Parameters.Length];

                                    var setterParameterIndicesGetterParameterOrdered =
                                        new int[getter.Parameters.Length];

                                    List<int> outputParameterIndices = new();

                                    for (var i = 0; i < getter.Parameters.Length; i++)
                                    {
                                        var getterParameter = getter.Parameters[i];
                                        var isOut = getterParameter.RefKind is RefKind.Out or RefKind.Ref;

                                        // finding setter parameter with same type and name as getter parameter
                                        var setterParameterIndex = setterMethod.Parameters.FindIndex(p =>
                                            p.Name == getterParameter.Name &&
                                            SymbolEqualityComparer.Default.Equals(p.Type, getterParameter.Type));

                                        if (setterParameterIndex == -1) return null;

                                        if (getterParametersSetterParameterOrdered[setterParameterIndex] != null)
                                            return null;
                                        getterParametersSetterParameterOrdered[setterParameterIndex] = getterParameter;

                                        setterParameterIndicesGetterParameterOrdered[i] = setterParameterIndex;

                                        if (isOut) outputParameterIndices.Add(setterParameterIndex);
                                    }

                                    // find return parameter index
                                    var returnParameterIndex = -1;
                                    for (var i = 0; i < getterParametersSetterParameterOrdered.Length; i++)
                                        if (getterParametersSetterParameterOrdered[i] == null)
                                        {
                                            if (returnParameterIndex != -1)
                                                // multiple return values
                                                return null;

                                            var parameter = setterMethod.Parameters[i];

                                            if (SymbolEqualityComparer.Default.Equals(parameter.Type,
                                                    getter.ReturnType))
                                                returnParameterIndex = i;
                                            else
                                                return null;
                                        }

                                    if (getter.ReturnType.SpecialType != SpecialType.System_Void &&
                                        returnParameterIndex == -1) return null;

                                    if (returnParameterIndex != -1) outputParameterIndices.Add(returnParameterIndex);

                                    // create expression
                                    var getterExpression =
                                        $"{(returnParameterIndex == -1 ? "" : $"{{{returnParameterIndex}}} = ")}{typeExpression}.{getter.Name}({string.Join(", ", setterParameterIndicesGetterParameterOrdered.Select((i, getterParameterIndex) => {
                                            return getter.Parameters[getterParameterIndex].RefKind switch {
                                                RefKind.Ref => $"ref {{{i}}}",
                                                RefKind.Out => $"out {{{i}}}",
                                                _ => $"{{{i}}}"
                                            };
                                        }))});";

                                    return (getterExpression, outputParameterIndices);
                                }).FirstOrDefault();

                            var resultPropertyName = $"{propertyName}.{ToTitleCase(setterMethod.Name)}()";

                            if (!editorGuiAnalyzer.LoweredPropertyNameAndLabel.TryGetValue(
                                    setterMethod.Name[3..].ToLowerInvariant(), out var labelName))
                                labelName = Utils.ToNiceLabelName(setterMethod.Name.AsSpan()[3..]);

                            data = new ApiData($"{expectedName}{ToTitleCase(member.Name)}", resultPropertyName,
                                $"{propertyName}: {labelName}", isObsolete,
                                setterExpression, getterExpression);

                            for (var i = 0; i < setterMethod.Parameters.Length; i++)
                            {
                                var parameter = setterMethod.Parameters[i];
                                data.Parameters.Add(
                                    new ParameterData(parameter.Type.ToDisplayString(DisplayFormatForType),
                                        parameter.Name, outputParameterIndices?.Contains(i) ?? true));
                            }
                        }
                        else if (member is IPropertySymbol property)
                        {
                            if (property.SetMethod == null) continue;
                            if (property.IsIndexer) continue;
                            if (property.Type is INamedTypeSymbol named && named.IsGenericType) continue;

                            // is it serializable?
                            if (!property.Type.BuiltinSerializationWrapperExists(out _) &&
                                !TypeUtils.IsSerializableFieldType(property.Type)) continue;

                            var setterExpression = $"{appliedMemberName} = {{0}};";
                            var resultPropertyName = $"{propertyName}.{ToTitleCase(member.Name)}";

                            var getterExpression = property.GetMethod == null
                                ? null
                                : $"{{0}} = {appliedMemberName};";

                            if (!editorGuiAnalyzer.LoweredPropertyNameAndLabel.TryGetValue(
                                    member.Name.ToLowerInvariant(), out var labelName))
                            {
                                // [NativeMethod] attributes can be a hint to serialized property name
                                var nativeMethodAttr = property.SetMethod.GetAttributes().FirstOrDefault(attr =>
                                    attr.AttributeClass.ContainingNamespace.ToString() ==
                                    "UnityEngine.Bindings.NativeMethodAttribute");

                                string? nativeName = null;
                                if (nativeMethodAttr != null)
                                {
                                    if (nativeMethodAttr.ConstructorArguments.Length >= 1)
                                        nativeName = nativeMethodAttr.ConstructorArguments[0].Value as string;
                                    else
                                        nativeName = nativeMethodAttr.NamedArguments
                                            .FirstOrDefault(a => a.Key == "name").Value.Value as string;
                                }

                                if (nativeName != null &&
                                    nativeName.StartsWith("Set", StringComparison.OrdinalIgnoreCase))
                                    nativeName = nativeName[3..];

                                if (nativeName == null || !editorGuiAnalyzer.LoweredPropertyNameAndLabel.TryGetValue(
                                        nativeName.ToLowerInvariant(), out labelName))
                                    labelName = Utils.ToNiceLabelName(member.Name);
                            }

                            data = new ApiData($"{expectedName}Set{ToTitleCase(member.Name)}", resultPropertyName,
                                $"{propertyName}: {labelName}", isObsolete,
                                setterExpression, getterExpression);
                            data.Parameters.Add(
                                new ParameterData(property.Type.ToDisplayString(DisplayFormatForType), property.Name,
                                    true));
                        }
                        else
                        {
                            continue;
                        }

                        category.Apis.Add(data);
                    }
                }
            }

            await library.SaveAsync(version, ct);
        }
    }

    /// <summary>
    ///     Makes the first letter of the string uppercase
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private string ToTitleCase(string s)
    {
        if (s.Length == 0) return s;
        if (char.IsUpper(s[0])) return s;
        Span<char> nameSpan = stackalloc char[s.Length];
        s.CopyTo(nameSpan);
        nameSpan[0] = char.ToUpperInvariant(nameSpan[0]);
        return nameSpan.ToString();
    }

    #region Nested type: MSBuildLogger

    public class MSBuildLogger : ILogger
    {
        private ILogger<MSBuildLogger> _logger;

        public MSBuildLogger(ILogger<MSBuildLogger> logger)
        {
            _logger = logger;
        }

        #region ILogger Members

        public void Initialize(IEventSource eventSource)
        {
            //eventSource.ErrorRaised += (sender, args) => { logger.ZLogInformation($"[MSBuild | {Verbosity}] {args.Message}"); };
            //eventSource.AnyEventRaised += (sender, args) => { _logger.ZLogInformation($"[MSBuild | {Verbosity}] {args.Message}"); };
        }

        public void Shutdown()
        {
        }

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Quiet;
        public string Parameters { get; set; }

        #endregion
    }

    #endregion
}
