// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     AnalysisResult / ApiDataにおけるAPIのパラメータ
/// </summary>
/// <param name="typeExpression"></param>
/// <param name="name"></param>
/// <param name="isOutput"></param>
public class ParameterData(string typeExpression, string name, bool isOutput)
{
    /// <summary>
    ///     型を表すC#コードの式
    /// </summary>
    public string TypeExpression { get; } = typeExpression;

    public string Name { get; } = name;

    /// <summary>
    ///     Getterが返す値
    ///     IsOutput=falseのパラメータは、ディクショナリのキーとしてシリアライズする
    /// </summary>
    public bool IsOutput { get; } = isOutput;
}
