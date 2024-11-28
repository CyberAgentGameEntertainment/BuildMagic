// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///      カテゴリごとのAPIリスト
/// </summary>
public class ApiCategory
{
    public List<ApiData> Apis { get; set; } = new();
}
