// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using BuildMagicEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BuildMagic.Generators;

[Generator(LanguageNames.CSharp)]
public class BuildTaskAccessoriesGenerator : IIncrementalGenerator
{
    #region IIncrementalGenerator Members

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tasks = context.SyntaxProvider.ForAttributeWithMetadataName(
            "BuildMagicEditor.GenerateBuildTaskAccessoriesAttribute",
            static (node, token) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
            static (context, token) => context);

        context.RegisterSourceOutput(tasks, EmitAccessories);
        context.RegisterSourceOutput(tasks.Collect().Combine(context.CompilationProvider), EmitRegisterer);
    }

    #endregion

    private void EmitRegisterer(SourceProductionContext context,
        (ImmutableArray<GeneratorAttributeSyntaxContext> sourceContexts, Compilation compilation) tuple)
    {
        var (sourceContexts, compilation) = tuple;
        if (compilation.AssemblyName != Consts.BuildMagicEditorAssemblyName) return;

        StringBuilder sourceBuilder = new();

        sourceBuilder.AppendLine(
/*  lang=c# */"""
              #pragma warning disable CS0612
              #pragma warning disable CS0618
              namespace BuildMagicEditor
              {
                  partial class BuildTaskBuilderProvider
                  {
                      private partial void RegisterBuiltInBuilder()
                      {
              """);

        foreach (var sourceContext in sourceContexts)
        {
            if (sourceContext.TargetSymbol is not INamedTypeSymbol typeSymbol) continue;
            if (typeSymbol.IsGenericType) continue; // TODO: support generics

            var constructors = GetTargetConstructor(typeSymbol);
            if (constructors.Length != 1) return;

            var constructor = constructors[0];

            string fullName;
            if (typeSymbol.ContainingType != null)
                fullName =
                    $"{typeSymbol.ContainingType.ToDisplayString(new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Included,
                        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, SymbolDisplayGenericsOptions.IncludeTypeParameters))}.{typeSymbol.Name}";
            else
                fullName = typeSymbol.ToDisplayString(new SymbolDisplayFormat(
                    SymbolDisplayGlobalNamespaceStyle.Included,
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

            var needDedicatedParameterType = constructor.Parameters.Length > 1;
            var parameterTypeExpression = $"{fullName}Parameters";
            if (!needDedicatedParameterType)
            {
                string EmitValueTupleContainer(IEnumerable<(string? name, string typeExpression)> tupleMembers)
                {
                    needDedicatedParameterType = true;
                    return "dummy";
                }

                var singleParameterTypeExpression = "global::BuildMagicEditor.EmptyParameter";
                foreach (var parameter in constructor.Parameters)
                    singleParameterTypeExpression =
                        ToParameterTypeExpression(parameter.Type,
                            new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default),
                            EmitValueTupleContainer);

                if (!needDedicatedParameterType) parameterTypeExpression = singleParameterTypeExpression;
            }

            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                            Register<{{fullName}}, {{parameterTypeExpression}}>(new {{fullName}}Builder());
                """);
        }

        sourceBuilder.AppendLine(
/*  lang=c# */"""
                      }
                  }
              }
              """);

        context.AddSource("BuildTaskBuilderProvider.BuiltIn.g.cs",
            SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    private static IMethodSymbol[] GetTargetConstructor(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.InstanceConstructors.Length == 1)
            // IBuildTask types with exact one constructor don't need attributes
            return new[] { typeSymbol.InstanceConstructors[0] };
        // multiple constructors with [BuildTaskConstructor] will cause error
        return typeSymbol.InstanceConstructors.Where(c =>
            c.GetAttributes().Any(attr =>
                attr.AttributeClass.MatchFullMetadataName(Consts.BuildTaskConstructorAttribute))).ToArray();
    }

    private void EmitAccessories(SourceProductionContext context, GeneratorAttributeSyntaxContext task)
    {
        var attribute = task.Attributes.FirstOrDefault(a =>
            a.AttributeClass.MatchFullMetadataName("BuildMagicEditor.GenerateBuildTaskAccessoriesAttribute"));

        if (attribute == default) return;

        var attrTargets = attribute.NamedArguments.FirstOrDefault(a => a.Key == "Targets");

        var generationTargets = attrTargets.Key == "Targets"
            ? (BuildTaskAccessories)attrTargets.Value.Value!
            : BuildTaskAccessories.All;

        var propertyName = attribute.NamedArguments.FirstOrDefault(a => a.Key == "PropertyName").Value.Value as string;

        var displayName = attribute.ConstructorArguments.Length == 1
            ? attribute.ConstructorArguments[0].Value as string
            : null;

        if (generationTargets == BuildTaskAccessories.None) return;

        if (task.TargetSymbol is not INamedTypeSymbol typeSymbol) throw new InvalidOperationException();
        var wrapperTypes = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);

        var availableWrapperTypes = typeSymbol.GetAttributes()
            .Where(attr =>
                attr.AttributeClass.MatchFullMetadataName("BuildMagicEditor.UseSerializationWrapperAttribute"))
            .Select(attr => attr.ConstructorArguments[0].Value as ITypeSymbol);

        foreach (var wrapperType in availableWrapperTypes)
        foreach (var attr in wrapperType.GetAttributes().Where(attr =>
                     attr.AttributeClass.MatchFullMetadataName("BuildMagicEditor.SerializationWrapperAttribute")))
        {
            if (attr.ConstructorArguments.Length == 0) continue;
            if (attr.ConstructorArguments[0].Value is not ITypeSymbol targetType) continue;

            wrapperTypes[targetType] = wrapperType;
        }

        // throw new Exception(string.Join(", ", wrapperTypes.Select(t => $"{t.Key}:{t.Value}")));

        var ns = typeSymbol.ContainingNamespace;

        var name = typeSymbol.Name;
        var isObsolete = typeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass.MatchFullMetadataName("System.ObsoleteAttribute"));

        string fullName;
        if (typeSymbol.ContainingType != null)
            fullName =
                $"{typeSymbol.ContainingType.ToDisplayString(new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Included,
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, SymbolDisplayGenericsOptions.IncludeTypeParameters))}.{typeSymbol.Name}";
        else
            fullName = typeSymbol.ToDisplayString(new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Included,
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

        var accessibility = typeSymbol.DeclaredAccessibility.ToDisplayString();

        // select constructor

        var constructors = GetTargetConstructor(typeSymbol);
        if (constructors.Length == 0) return;

        if (constructors.Length != 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("BUILDMAGIC0001", "Multiple constructors with BuildTaskConstructor attribute",
                    "Multiple constructors with BuildTaskConstructor attribute", "Compiler", DiagnosticSeverity.Error,
                    true), task.TargetNode.GetLocation()));
            return;
        }

        var constructor = constructors[0];

        // make generic expressions
        var typeParametersExpression = typeSymbol.ToTypeParametersExpression();
        var typeConstraintsExpression = typeSymbol.ToTypeConstraintsExpression();

        // [MovedFrom] attribute. If the IBuildTask implementation is marked as moved from other name, also mark the generated IBuildConfiguration implementation as moved from. 
        var movedFromAttributes = typeSymbol.GetAttributes().Where(attr =>
            attr.AttributeClass.MatchFullMetadataName("UnityEngine.Scripting.APIUpdating.MovedFromAttribute"));

        StringBuilder sourceBuilder = new();

        if (!ns.IsGlobalNamespace)
        {
            sourceBuilder.AppendLine($"namespace {ns}");
            sourceBuilder.AppendLine("{");
        }

        // containing types
        void AppendContainingTypeScope(INamedTypeSymbol previous)
        {
            var current = previous.ContainingType;
            if (current == null) return;
            AppendContainingTypeScope(current);
            sourceBuilder.AppendLine($"partial class {current.Name}{current.ToTypeParametersExpression()}");
            sourceBuilder.AppendLine("{");
        }

        void AppendContainingTypeScopeCloser(INamedTypeSymbol previous)
        {
            var current = previous.ContainingType;
            if (current == null) return;
            AppendContainingTypeScopeCloser(current);
            sourceBuilder.AppendLine($"}} // partial class {current.Name}{current.ToTypeParametersExpression()}");
        }

        AppendContainingTypeScope(typeSymbol);

        List<(string containerName, IEnumerable<(string? name, string typeExpression)>)> tupleMemberSet = new();
        Dictionary<IParameterSymbol, string> parameterTypeExpressions = new();

        string EmitValueTupleContainer(IEnumerable<(string? name, string typeExpression)> tupleMembers)
        {
            // NOTE: should we change this to more human-readable name?
            var containerName = $"__BUILDMAGIC__AnonymousTupleContainer__{tupleMemberSet.Count}";
            tupleMemberSet.Add((containerName, tupleMembers));
            return containerName;
        }

        foreach (var parameter in constructor.Parameters)
            parameterTypeExpressions[parameter] =
                ToParameterTypeExpression(parameter.Type, wrapperTypes, EmitValueTupleContainer);

        // dedicated parameter type isn't necessary when there is only one parameter and no anonymous tuple container
        var needDedicatedParameterType = constructor.Parameters.Length > 1 || tupleMemberSet.Count > 0;

        var parameterTypeExpression = needDedicatedParameterType
            ? $"{fullName}Parameters{typeParametersExpression}"
            : parameterTypeExpressions.FirstOrDefault().Value ?? "global::BuildMagicEditor.EmptyParameter";

        if (generationTargets.HasFlag(BuildTaskAccessories.Configuration))
        {
            if (!string.IsNullOrEmpty(displayName) || !string.IsNullOrEmpty(propertyName))
            {
                sourceBuilder.Append($"[global::BuildMagicEditor.BuildConfiguration(");

                bool hasDisplayName = !string.IsNullOrEmpty(displayName); 
                if (hasDisplayName)
                {
                    sourceBuilder.Append($"DisplayName = @\"{displayName}\"");
                }

                if (!string.IsNullOrEmpty(propertyName))
                {
                    if(hasDisplayName) sourceBuilder.Append($", ");
                    sourceBuilder.Append($"PropertyName = @\"{propertyName}\"");
                }

                sourceBuilder.AppendLine($")]");
            }

            // [MovedFrom]
            if (movedFromAttributes.Any())
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    $$"""
                      [{{string.Join(", ", movedFromAttributes.Select(attr => attr.ConstructorArguments).Select(args => $"global::UnityEngine.Scripting.APIUpdating.MovedFrom({string.Join(", ", args.Select((a, index) => {
                          if (index == 3 && a.Value != null) return $"\"{a.Value as string}Configuration\"";
                          return a.ToCSharpString();
                      }))})"))}}]
                      """);

            if (isObsolete)
                sourceBuilder.AppendLine("[global::System.Obsolete]");

            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                [global::System.Serializable]
                {{accessibility}} partial class {{name}}Configuration{{typeParametersExpression}} : global::BuildMagicEditor.BuildConfigurationBase<{{fullName}}{{typeParametersExpression}}, {{parameterTypeExpression}}> {{typeConstraintsExpression}}
                {
                """);

            sourceBuilder.AppendLine(
/*  lang=c# */"""}""");
        }

        if (generationTargets.HasFlag(BuildTaskAccessories.Parameters) && needDedicatedParameterType)
        {
            if (isObsolete)
                sourceBuilder.AppendLine("[global::System.Obsolete]");

            sourceBuilder.AppendLine(
/*  lang=c# */$$"""

                [global::System.Serializable]
                {{accessibility}} class {{name}}Parameters{{typeParametersExpression}} {{typeConstraintsExpression}}
                {
                """);

            foreach (var kvp in parameterTypeExpressions)
            {
                var parameter = kvp.Key;
                var typeExp = kvp.Value;
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    $$"""
                          public {{typeExp}} {{parameter.Name}};
                      """);
            }

            // emit tuple containers

            foreach (var (containerName, tupleMembers) in tupleMemberSet)
                EmitValueTupleContainerTypeDefinition(sourceBuilder, tupleMembers, containerName);

            sourceBuilder.AppendLine(
/*  lang=c# */"""}""");
        }

        if (generationTargets.HasFlag(BuildTaskAccessories.Builder))
        {
            if (isObsolete)
                sourceBuilder.AppendLine("[global::System.Obsolete]");

            if (typeSymbol.ContainingAssembly.Name != "BuildMagic.Editor")
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    $$"""
                      [global::BuildMagicEditor.BuildTaskBuilder(typeof({{fullName}}{{typeParametersExpression}}), typeof({{parameterTypeExpression}}))]
                      """);


            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                {{accessibility}} class {{name}}Builder{{typeParametersExpression}} : global::BuildMagicEditor.BuildTaskBuilderBase<{{fullName}}{{typeParametersExpression}}, {{parameterTypeExpression}}> {{typeConstraintsExpression}}
                {
                    public override {{fullName}}{{typeParametersExpression}} Build({{parameterTypeExpression}} value)
                    {
                """);

            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        return new {{fullName}}{{typeParametersExpression}}({{string.Join(", ", constructor.Parameters.Select(p => ToParameterReferenceExpression(p.Type, needDedicatedParameterType ? $"value.{p.Name}" : "value", wrapperTypes, out _)))}});
                    }
                }
                """);
        }

        AppendContainingTypeScopeCloser(typeSymbol);

        if (!ns.IsGlobalNamespace) sourceBuilder.AppendLine($"}} // {ns}");

        var filename = $"{typeSymbol.ToFullMetadataName()}.g.cs";

        context.AddSource(filename, SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    private static void EmitValueTupleContainerTypeDefinition(StringBuilder builder,
        IEnumerable<(string? name, string typeExpression)> members, string uniqueName)
    {
        builder.AppendLine(
/*  lang=c# */$$"""
                    [global::System.Serializable]
                    public struct {{uniqueName}}
                    {
                """);
        var index = 0;
        foreach (var (name, typeExp) in members)
        {
            index++;
            builder.AppendLine(
/*  lang=c# */$$"""
                        public {{typeExp}} {{name ?? $"Item{index}"}};
                """);
        }

        // conversions
        builder.AppendLine(
/*  lang=c# */$$"""
                        public static implicit operator ({{string.Join(", ", members.Select(m => $"{m.typeExpression} {m.name ?? ""}"))}})({{uniqueName}} source)
                        {
                            return ({{string.Join(", ", members.Select((m, i) => m.name ?? $"Item{i + 1}").Select(s => $"source.{s}"))}});
                        }
                        
                        public static implicit operator {{uniqueName}}(global::System.ValueTuple<{{string.Join(", ", members.Select(m => m.typeExpression))}}> source)
                        {
                            var result = new {{uniqueName}}();
                            ({{string.Join(", ", members.Select((m, i) => m.name ?? $"Item{i + 1}").Select(s => $"result.{s}"))}}) = ({{string.Join(", ", members.Select((m, i) => $"source.Item{i + 1}"))}});
                            return result;
                        }
                        
                """);

        builder.AppendLine(
/*  lang=c# */"""    }""");
    }

    private string ToParameterTypeExpression(ITypeSymbol parameterType,
        IReadOnlyDictionary<ITypeSymbol, ITypeSymbol> wrappers,
        Func<IEnumerable<(string? name, string typeExpression)>, string> emitValueTupleContainer)
    {
        if (parameterType is INamedTypeSymbol named &&
            named.MatchFullMetadataName("System.Collections.Generic.IReadOnlyDictionary`2"))
        {
            var tKey = named.TypeArguments[0];
            var tValue = named.TypeArguments[1];

            return
                $"global::BuildMagicEditor.SerializableDictionary<{ToParameterTypeExpression(tKey, wrappers, emitValueTupleContainer)}, {ToParameterTypeExpression(tValue, wrappers, emitValueTupleContainer)}>";
        }

        if (ValueTupleInfo.TryCreate(parameterType, out var tupleInfo))
        {
            // extract tuple as struct
            var tupleContainerSymbol = emitValueTupleContainer(tupleInfo.GetElements().Select(info =>
                    (info.Name,
                        ToParameterTypeExpression(info.Type, wrappers, emitValueTupleContainer)))
                .ToArray());
            return tupleContainerSymbol;
        }

        if (wrappers.TryGetValue(parameterType, out var wrapperType))
            return wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (parameterType.BuiltinSerializationWrapperExists(out var wrapperTypeExpression))
            return wrapperTypeExpression;

        return parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private string ToParameterReferenceExpression(ITypeSymbol parameterType, string sourceValueExpression,
        IReadOnlyDictionary<ITypeSymbol, ITypeSymbol> wrappers, out bool needTransformation)
    {
        if (parameterType is INamedTypeSymbol named &&
            named.MatchFullMetadataName("System.Collections.Generic.IReadOnlyDictionary`2"))
        {
            var tKey = ToParameterReferenceExpression(named.TypeArguments[0], "kvp.Key", wrappers,
                out var keyNeedTransformation);
            var tValue = ToParameterReferenceExpression(named.TypeArguments[1], "kvp.Value", wrappers,
                out var valueNeedTransformation);
            needTransformation = true;

            if (!keyNeedTransformation && !valueNeedTransformation) return $"{sourceValueExpression}.ToDictionary()";

            // parameter should be serialized as SerializableDictionary
            return
                $"(global::System.Collections.Generic.IReadOnlyDictionary<{named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {named.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>)global::System.Linq.Enumerable.ToDictionary({sourceValueExpression}.ToDictionary(), kvp => {tKey}, kvp => {tValue})";
        }

        if (ValueTupleInfo.TryCreate(parameterType, out var tupleInfo))
        {
            needTransformation = true;
            return $"({string.Join(", ",
                tupleInfo.GetElements().Select((elementInfo, index) => ToParameterReferenceExpression(elementInfo.Type,
                    $"{sourceValueExpression}.{elementInfo.Name ?? $"Item{index + 1}"}", wrappers, out _)))})";
        }

        if (wrappers.TryGetValue(parameterType, out _))
        {
            // unwrap
            needTransformation = true;
            return
                $"({parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){sourceValueExpression}";
        }

        if (parameterType.BuiltinSerializationWrapperExists(out _))
        {
            // unwrap
            needTransformation = true;
            return
                $"({parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){sourceValueExpression}";
        }

        needTransformation = false;

        return sourceValueExpression;
    }
}
