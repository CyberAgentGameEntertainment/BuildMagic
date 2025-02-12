// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     Obtains nice label names from editor code
/// </summary>
public class UnityEditorGuiAnalyzer
{
    private readonly Compilation _compilation;
    private readonly Dictionary<IFieldSymbol, IOperation> _fieldAssignments = new();
    private readonly Dictionary<string, string> _loweredPropertyNameAndLabel = new();
    private readonly HashSet<IMethodSymbol> _seenMethods = new();

    public UnityEditorGuiAnalyzer(Compilation compilation, ITypeSymbol editorType)
    {
        _compilation = compilation;

        var onEnableMethod = editorType
            .GetMembers("OnEnable")
            .OfType<IMethodSymbol>()
            .First();

        var onInspectorGuiMethod = editorType
            .GetMembers("OnInspectorGUI")
            .OfType<IMethodSymbol>()
            .First();

        AnalyzeMethod(onEnableMethod);
        AnalyzeMethod(onInspectorGuiMethod);
    }

    public IReadOnlyDictionary<string, string> LoweredPropertyNameAndLabel => _loweredPropertyNameAndLabel;

    private IOperation? GetInitialValue(IFieldSymbol field)
    {
        // get the initial value of the field
        var syntax = field
            .DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .SelectMany<SyntaxNode, VariableDeclaratorSyntax?>(s =>
            {
                if (s is FieldDeclarationSyntax fieldDecl) return fieldDecl.Declaration.Variables;
                if (s is VariableDeclarationSyntax declaration) return declaration.Variables;
                if (s is VariableDeclaratorSyntax declarator) return new[] { declarator };
                return null;
            })
            .Select(d => d?.Initializer?.Value).FirstOrDefault();

        if (syntax == null || !SymbolEqualityComparer.Default.Equals(field.ContainingAssembly, _compilation.Assembly))
            return null;

        var semanticModel = _compilation.GetSemanticModel(syntax.SyntaxTree);

        var root = semanticModel.GetOperation(syntax);

        return root;
    }

    private void AnalyzeMethod(IMethodSymbol method)
    {
        // avoid infinite recursion
        if (!_seenMethods.Add(method)) return;

        var syntax = method
            .DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(s => s.Body != null);

        if (syntax == null)
            return;

        if (!SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, _compilation.Assembly))
            return;

        var semanticModel = _compilation.GetSemanticModel(syntax.SyntaxTree);

        var root = semanticModel.GetOperation(syntax) as IMethodBodyOperation;

        if (root == null) return;

        AnalyzeOperation(method, root);
    }

    private IOperation ResolveStoredValue(IOperation operation)
    {
        // get the value stored in the field
        switch (operation)
        {
            case IFieldReferenceOperation fieldReference:
            {
                var field = fieldReference.Field;

                if (_fieldAssignments.TryGetValue(field, out var value)) return value;

                return GetInitialValue(field) ?? operation;
            }
        }

        return operation;
    }

    private void AnalyzeOperation(IMethodSymbol currentMethod, IOperation operation)
    {
        // record field assignments
        if (operation is IAssignmentOperation assignment)
            if (assignment.Target is IFieldReferenceOperation fieldReference)
            {
                var field = fieldReference.Field;
                _fieldAssignments[field] = assignment.Value;
            }

        if (operation is IInvocationOperation invocation)
        {
            var method = invocation.TargetMethod;

            if (method.Name == "BuildEnumPopup" ||
                method.ContainingType.ContainingNamespace.ToString() is "UnityEditor" &&
                method.ContainingType.Name is "EditorGUILayout" or "EditorGUI")
            {
                // EditorGUI.~~Field()、EditorGUILayout.~~Field()
                if (!method.Name.EndsWith("Field", StringComparison.Ordinal)) return;

                // get parameters

                if (!TryGetParameter(invocation, "property", out var propertyArgument) &&
                    !TryGetParameter(invocation, "prop", out propertyArgument))
                    return;

                _ = !TryGetParameter(invocation, "label", out var labelArgument) &&
                    !TryGetParameter(invocation, "uiString", out labelArgument);

                // if the parameter is from a fiels, resolve it

                propertyArgument = ResolveStoredValue(propertyArgument);

                labelArgument = labelArgument != null ? ResolveStoredValue(labelArgument) : null;

                // if the label is localized, EditorGUI.TrTextContent() is called, so expand the arguments

                if (labelArgument is IInvocationOperation { TargetMethod: { Name: "TrTextContent" } } labelInvocation)
                {
                    var args = labelInvocation.Arguments;
                    if (args.Length >= 1) labelArgument = labelInvocation.Arguments[0].Value;
                }

                // get the property path from the argument of FindPropertyAssert()

                string? propertyName = null;
                if (propertyArgument is IInvocationOperation
                    {
                        Instance: IInstanceReferenceOperation, TargetMethod: { Name: "FindPropertyAssert" }
                    } propertyInvocation)
                {
                    var args = propertyInvocation.Arguments;
                    if (args.Length >= 1 && propertyInvocation.Arguments[0].Value is ILiteralOperation
                        {
                            Type: { SpecialType: SpecialType.System_String }
                        } literal)
                        propertyName = literal.ConstantValue.Value as string;
                }

                string? labelValue = null;
                if (labelArgument is ILiteralOperation
                    {
                        Type: { SpecialType: SpecialType.System_String }
                    } labelLiteral)
                    labelValue = labelLiteral.ConstantValue.Value as string;

                // associate the property path with the label

                if (propertyName != null)
                {
                    labelValue ??= Utils.ToNiceLabelName(propertyName);

                    _loweredPropertyNameAndLabel[propertyName.ToLowerInvariant()] = labelValue;
                }
            }
            else
            {
                // Recurse instance method call with the same instance
                if (invocation.Instance is IInstanceReferenceOperation ||
                    method.IsStatic && method.ContainingType == currentMethod.ContainingType)
                    AnalyzeMethod(method);
            }

            return;
        }

        foreach (var childOperation in operation.ChildOperations) AnalyzeOperation(currentMethod, childOperation);
    }

    private bool TryGetParameter(IInvocationOperation invocation, string name,
        [NotNullWhen(true)] out IOperation? operation)
    {
        var method = invocation.TargetMethod;
        var parameters = method.Parameters
            .Select((p, i) => (p, i));
        var labelParameterSet =
            parameters.FirstOrDefault(p => p.p.Name == name);

        if (labelParameterSet.p == null)
        {
            operation = null;
            return false;
        }

        var labelParameterIndex = labelParameterSet.i;

        // get label argument
        operation = invocation.Arguments[labelParameterIndex].Value;
        return true;
    }
}
