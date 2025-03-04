// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagic.BuiltinTaskGenerator;

public class UnityUpgradableData(string? assemblyName, string memberName)
{
    public string? AssemblyName { get; set; } = assemblyName;
    public string MemberName { get; set; } = memberName;
}
