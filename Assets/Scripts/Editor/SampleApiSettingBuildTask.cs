// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using BuildMagicEditor;
using UnityEditor;

// required to generate a build configuration, etc.
[GenerateBuildTaskAccessories(
    // display name in the UI
    "Sample Api Setting",
    // property name used for the key of CLI override
    PropertyName = "SampleApiSetting")]
[BuildConfiguration("SampleApiSetting")]
public class SampleApiSettingBuildTask : BuildTaskBase<IPreBuildContext>
{
    private readonly string _url;
    private readonly int _port;

    // All required values for updating with this setting are taken as constructor arguments
    public SampleApiSettingBuildTask(string url, int port)
    {
        _url = url;
        _port = port;
    }

    public override void Run(IPreBuildContext context)
    {
        // Implement the process of reflecting the settings you have on the project in Run
        var setting = AssetDatabase.LoadAssetAtPath<SampleApiSetting>("Assets/Settings/SampleApiSettings.asset");
        if (setting != null)
        {
            setting.Url = _url;
            setting.Port = _port;
            EditorUtility.SetDirty(setting);
        }
    }
}

public partial class SampleApiSettingBuildTaskConfiguration : IProjectSettingApplier
{
    void IProjectSettingApplier.ApplyProjectSetting()
    {
        var setting = AssetDatabase.LoadAssetAtPath<SampleApiSetting>("Assets/Settings/SampleApiSettings.asset");
        if (setting != null)
            Value = new SampleApiSettingBuildTaskParameters
            {
                port = setting.Port,
                url = setting.Url
            };
    }
}
