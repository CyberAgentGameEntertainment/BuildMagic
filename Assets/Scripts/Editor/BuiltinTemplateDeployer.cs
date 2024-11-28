// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.IO;
using BuildMagicEditor;
using UnityEditor;
using UnityEngine.Assertions;

public static class BuiltinTemplate
{
    private const string Name = "BuiltinTemplate";
    
    [MenuItem("BuildMagic(Dev)/Deploy Builtin Template")]
    public static void DeployBuiltinTemplate()
    {
        var template = BuildSchemeLoader.Load<BuildScheme>(Name);
        Assert.IsNotNull(template);

        var json = BuildSchemeSerializer.Serialize(template);
        var path = $"Packages/jp.co.cyberagent.buildmagic/BuildMagic.Window/Editor/Assets/Templates/{Name}.json";
        File.WriteAllText(path, json);
    }
}
