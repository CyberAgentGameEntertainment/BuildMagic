// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     A list of APIs by category
/// </summary>
public class ApiCategory
{
    public List<ApiData> Apis { get; set; } = new();
}
