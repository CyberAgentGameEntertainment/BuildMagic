// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Reflection;
using UnityEngine;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Preferences: Enable Linker Report")]
    public class EnableLinkerReportTask : BuildTaskBase<IPreBuildContext>
    {
        private const string EnableReport = "--enable-report";
        private const string EnableSnapshot = "--enable-snapshot";
        private readonly bool _isEnabled;

        public EnableLinkerReportTask(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public override void Run(IPreBuildContext context)
        {
            object diagnosticSwitch = typeof(Debug)
                .GetMethod("GetDiagnosticSwitch", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { "VMUnityLinkerAdditionalArgs" });

            var type = diagnosticSwitch.GetType();

            var prop = type.GetProperty("persistentValue", BindingFlags.Public | BindingFlags.Instance);

            var value = (string)prop.GetValue(diagnosticSwitch);

            value = SetFlag(value, EnableReport, _isEnabled);
            value = SetFlag(value, EnableSnapshot, _isEnabled);

            prop.SetValue(diagnosticSwitch, value);
        }

        private static string SetFlag(string value, string flag, bool isEnabled)
        {
            for (var cursor = 0; cursor < value.Length; cursor++)
            {
                int index = value.IndexOf(flag, cursor, StringComparison.Ordinal);

                if (index == -1) break;

                cursor = index + 1;

                var existing = index >= 0 &&
                               (index == 0 || value[index - 1] == ' ') &&
                               (index == value.Length - flag.Length || value[index + flag.Length] == ' ');

                if (existing && isEnabled) return value;

                if (isEnabled || !existing) continue;

                // remove
                var before = value.AsSpan()[..index];
                var after = value.AsSpan()[(index + flag.Length)..];

                if (before.Length >= 1 && before[^1] == ' ') before = before[..^1];
                if (after.Length >= 1 && after[0] == ' ') after = after[1..];

                value = $"{before.ToString()} {after.ToString()}";
            }

            if (isEnabled)
            {
                if (value.Length >= 1 && value[^1] == ' ') value += flag;
                else value += $" {flag}";
            }

            return value;
        }
    }
}
