// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     Represents a result of analysis for a specific version
/// </summary>
public class AnalysisResult
{
    public static readonly int CurrentSerializedVersion = 5;

    public int SerializedVersion { get; set; } = CurrentSerializedVersion;
    public Dictionary<string, ApiCategory> Categories { get; set; } = new();
}
