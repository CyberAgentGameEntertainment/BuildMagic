// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     Represents a parameter of an API in the AnalysisResult / ApiData
/// </summary>
/// <param name="typeExpression"></param>
/// <param name="name"></param>
/// <param name="isOutput"></param>
public class ParameterData(string typeExpression, string name, bool isOutput)
{
    /// <summary>
    ///     C# expression representing the type
    /// </summary>
    public string TypeExpression { get; } = typeExpression;

    public string Name { get; } = name;

    /// <summary>
    ///     "out" parameter of a method or value parameter of a proprety 
    ///     ParameterData with IsOutput=false will be serialized as a key in the dictionary
    /// </summary>
    public bool IsOutput { get; } = isOutput;
}
