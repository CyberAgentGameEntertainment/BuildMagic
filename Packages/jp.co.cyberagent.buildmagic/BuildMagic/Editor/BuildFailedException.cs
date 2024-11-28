// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor.Build.Reporting;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Exception thrown when build failed.
    /// </summary>
    public class BuildFailedException : Exception
    {
        private BuildReport Report { get; }

        public BuildFailedException(BuildReport report) : base(report.summary.ToString())
        {
            Report = report;
        }
    }
}
