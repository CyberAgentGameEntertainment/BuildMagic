// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     AnalysisResultにおける各APIのデータ
/// </summary>
/// <param name="expectedName"></param>
/// <param name="isObsolete"></param>
/// <param name="setterExpression"></param>
public class ApiData(
    string expectedName,
    string propertyName,
    string displayName,
    bool isObsolete,
    string setterExpression,
    string? getterExpression)
{
    /// <summary>
    ///     Task名として期待される名前
    /// </summary>
    public string ExpectedName { get; set; } = expectedName;

    /// <summary>
    ///     表示名
    /// </summary>
    public string PropertyName { get; set; } = propertyName;

    public string DisplayName { get; set; } = displayName;

    /// <summary>
    ///     Obsoleteかどうか（errorフラグが立っている場合はそもそもリストしない）
    /// </summary>
    public bool IsObsolete { get; set; } = isObsolete;

    /// <summary>
    ///     値をセットするC#コードの文
    ///     {0}, {1}... の形で各引数が補間される
    /// </summary>
    public string SetterExpression { get; set; } = setterExpression;

    /// <summary>
    ///     値を取得するC#コードの文
    ///     代入先を{0}で示すと補間される
    /// </summary>
    public string? GetterExpression { get; set; } = getterExpression;

    public List<ParameterData> Parameters { get; set; } = new();
}
