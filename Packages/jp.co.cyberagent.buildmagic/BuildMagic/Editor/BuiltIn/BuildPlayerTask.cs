// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor.Build.Reporting;

namespace BuildMagicEditor.BuiltIn
{
    /// <summary>
    ///     Execute build player.
    /// </summary>
    [GenerateBuildTaskAccessories]
    internal class BuildPlayerTask : BuildTaskBase<IBuildPlayerContext>
    {
        [ThreadStatic] private static int depth;

        // Determine if the current build is triggered by BuildMagic
        public static bool IsCurrentThreadBuildingPlayer => depth > 0;

        public override void Run(IBuildPlayerContext context)
        {
            BuildReport result;
            depth++;
            try
            {
                result = UnityEditor.BuildPipeline.BuildPlayer(context.BuildPlayerOptions);
            }
            finally
            {
                depth--;
            }

            context.SetResult(result);
        }
    }
}
