// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     バージョンごとの解析結果を保持する
/// </summary>
public class AnalysisResult
{
    public static readonly int CurrentSerializedVersion = 4;

    public int SerializedVersion { get; set; } = CurrentSerializedVersion;
    public Dictionary<string, ApiCategory> Categories { get; set; } = new();
}
