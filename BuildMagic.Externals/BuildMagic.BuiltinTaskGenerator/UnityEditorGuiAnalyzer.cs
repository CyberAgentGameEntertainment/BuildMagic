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
///     エディタコードからラベル等を取得する
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
        // フィールドの初期値を取得する
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
        // 再帰ループ防止
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
        // フィールドに代入された値を取得する
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
        // フィールドへの代入を記録
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
                // EditorGUI.~~Field()、EditorGUILayout.~~Field()に注目
                if (!method.Name.EndsWith("Field", StringComparison.Ordinal)) return;

                // 引数を取得

                if (!TryGetParameter(invocation, "property", out var propertyArgument) &&
                    !TryGetParameter(invocation, "prop", out propertyArgument))
                    return;

                _ = !TryGetParameter(invocation, "label", out var labelArgument) &&
                    !TryGetParameter(invocation, "uiString", out labelArgument);

                // 引数がフィールドに由来しているなら、それを解決

                propertyArgument = ResolveStoredValue(propertyArgument);

                labelArgument = labelArgument != null ? ResolveStoredValue(labelArgument) : null;

                // ラベルがローカライズされている場合は EditorGUI.TrTextContent() が呼ばれているので、その引数を展開

                if (labelArgument is IInvocationOperation { TargetMethod: { Name: "TrTextContent" } } labelInvocation)
                {
                    var args = labelInvocation.Arguments;
                    if (args.Length >= 1) labelArgument = labelInvocation.Arguments[0].Value;
                }

                // プロパティはFindPropertyAssert()で取得されているので、引数を展開してプロパティパスを取得

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

                // プロパティパスとラベルを対応づける

                if (propertyName != null)
                {
                    labelValue ??= Utils.ToNiceLabelName(propertyName);

                    _loweredPropertyNameAndLabel[propertyName.ToLowerInvariant()] = labelValue;
                }
            }
            else
            {
                // 同じインスタンスのメソッドを呼んでいる場合は再帰的に解析
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
