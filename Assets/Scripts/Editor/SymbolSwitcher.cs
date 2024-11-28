// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SymbolSwitcher : EditorWindow
{
    private const int MinColumnSize = 120;

    private static readonly SymbolData[] Symbols;

    private static readonly Dictionary<(TargetPlatform, SymbolData), bool> SymbolStatus = new();

    private static Vector2 ScrollPos;

    static SymbolSwitcher()
    {
        Symbols = new SymbolData[]
        {
            new("BUILDMAGIC_DEVELOPER", "BUILDMAGIC_DEVELOPER")
        };
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos, GUILayout.Height(position.height));

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        // プラットフォーム列
        using (new GUILayout.VerticalScope())
        {
            var layoutOption = GUILayout.MinWidth(MinColumnSize);
            EditorGUILayout.LabelField("", layoutOption);
            foreach (var targetPlatform in TargetPlatform.GetValues())
                EditorGUILayout.LabelField(targetPlatform.Name, layoutOption);
        }

        // チェックボックス列
        foreach (var symbol in Symbols)
            using (new GUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(symbol.DisplayName, GUILayout.MinWidth(MinColumnSize));
                foreach (var targetPlatform in TargetPlatform.GetValues())
                    SymbolStatus[(targetPlatform, symbol)] =
                        EditorGUILayout.Toggle(SymbolStatus[(targetPlatform, symbol)]);
            }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("Apply"))
        {
            ApplySymbols();
            Close();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    [MenuItem("BuildMagic(Dev)/Open Symbol Switcher")]
    public static void Open()
    {
        GetWindow<SymbolSwitcher>(true, "Symbol Switcher");
    }

    private void OnEnable()
    {
        SymbolStatus.Clear();
        foreach (var targetPlatform in TargetPlatform.GetValues())
        foreach (var symbol in Symbols)
        {
            var enabled = PlayerSettings
                          .GetScriptingDefineSymbolsForGroup(targetPlatform.BuildTargetGroup)
                          .Split(';')
                          .Any(s => s == symbol.Value);

            SymbolStatus[(targetPlatform, symbol)] = enabled;
        }

        // プラットフォーム列 + チェックボックス列 + 余白
        maxSize = new Vector2(MinColumnSize + MinColumnSize * Symbols.Length + MinColumnSize,
                              300);
        minSize = new Vector2(MinColumnSize + MinColumnSize * Symbols.Length + MinColumnSize,
                              300);
    }

    private static void ApplySymbols()
    {
        foreach (var targetPlatform in TargetPlatform.GetValues())
        {
            var defineSymbols = PlayerSettings
                                .GetScriptingDefineSymbolsForGroup(targetPlatform.BuildTargetGroup)
                                .Split(';')
                                .ToList();

            foreach (var symbolData in Symbols)
            {
                defineSymbols.Remove(symbolData.Value);
                if (SymbolStatus[(targetPlatform, symbolData)])
                    defineSymbols.Add(symbolData.Value);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetPlatform.BuildTargetGroup,
                                                             string.Join(";", defineSymbols));
        }
    }

    private class SymbolData
    {
        public SymbolData(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }

        public string DisplayName { get; }
        public string Value { get; }
    }

    private class TargetPlatform : CachedEnumeration<TargetPlatform>
    {
        public static readonly TargetPlatform Ios = new(0, nameof(Ios), BuildTargetGroup.iOS);
        public static readonly TargetPlatform Android = new(1, nameof(Android), BuildTargetGroup.Android);
        public static readonly TargetPlatform Standalone = new(2, nameof(Standalone), BuildTargetGroup.Standalone);

        private TargetPlatform(int id, string name, BuildTargetGroup buildTargetGroup) : base(id, name)
        {
            BuildTargetGroup = buildTargetGroup;
        }

        public BuildTargetGroup BuildTargetGroup { get; }
    }
}
