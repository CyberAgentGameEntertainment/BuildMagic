// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     An entry of the data of each API in the AnalysisResult
/// </summary>
/// <param name="expectedName"></param>
/// <param name="isObsolete"></param>
/// <param name="setterExpression"></param>
public class ApiData(
    string expectedName,
    string propertyName,
    string displayName,
    bool isObsolete,
    UnityUpgradableData? unityUpgradableData,
    string setterExpression,
    string? getterExpression)
{
    /// <summary>
    ///     Expected name of the task
    /// </summary>
    public string ExpectedName { get; set; } = expectedName;

    /// <summary>
    ///     Display name
    /// </summary>
    public string PropertyName { get; set; } = propertyName;

    public string DisplayName { get; set; } = displayName;

    /// <summary>
    ///     Whether it is obsolete (not listed at all if the error flag is set）
    /// </summary>
    public bool IsObsolete { get; set; } = isObsolete;

    /// <summary>
    ///     Directive for API Updater if it is obsolete
    /// </summary>
    public UnityUpgradableData? UnityUpgradableData { get; set; } = unityUpgradableData;

    /// <summary>
    ///     C# expression to set the value
    ///     It will be interpolated with arguments like {0}, {1}...
    /// </summary>
    public string SetterExpression { get; set; } = setterExpression;

    /// <summary>
    ///     C# expression to get the value
    ///     {0} will be interpolated as the destination of assignation
    /// </summary>
    public string? GetterExpression { get; set; } = getterExpression;

    public List<ParameterData> Parameters { get; set; } = new();
}
