// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Reflection;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildMagicEditor.BuiltIn
{
    public class LogBuildResultTask : BuildTaskBase<IPostBuildContext>
    {
        public override void Run(IPostBuildContext context)
        {
            var report = context.BuildReport;

            var resultStr = string.Format("Build completed with a result of '{0:g}' in {1} seconds ({2} ms)",
                report.summary.result,
                Convert.ToInt32(report.summary.totalTime.TotalSeconds),
                Convert.ToInt32(report.summary.totalTime.TotalMilliseconds));

            switch (report.summary.result)
            {
                case BuildResult.Unknown:
                    Debug.LogWarning(resultStr);
                    break;
                case BuildResult.Failed:
                {
#if UNITY_2023_1_OR_NEWER
                    var summarizedErrors = report.SummarizeErrors();
#else
                    var summarizeErrors = report.GetType()
                        .GetMethod("SummarizeErrors", BindingFlags.NonPublic | BindingFlags.Instance);
                    var summarizedErrors = (string)summarizeErrors?.Invoke(report, Array.Empty<object>()) ?? "";
#endif
                    Debug.LogError($"{resultStr}\n{summarizedErrors}");
                    break;
                }
                default:
                    Debug.Log(resultStr);
                    break;
            }
        }
    }
}
