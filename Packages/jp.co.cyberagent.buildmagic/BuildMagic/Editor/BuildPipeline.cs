// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using BuildMagicEditor.BuiltIn;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Build magic pipeline.
    /// </summary>
    public static class BuildPipeline
    {
        public static void PreBuild(IReadOnlyList<IBuildTask<IPreBuildContext>> preBuildTasks)
        {
            RunTasks(preBuildTasks, PreBuildContext.Create());
        }

        public static void Build(
            BuildPlayerOptions buildPlayerOptions,
            IReadOnlyList<IBuildTask<IPostBuildContext>> postBuildTasks,
            bool strictMode = true)
        {
            var buildPlayerTask = new BuildPlayerTask();
            var buildPlayerContext = BuildPlayerContext.Create(buildPlayerOptions) as IBuildPlayerContext;
            RunTasks(new IBuildTask[] { buildPlayerTask }, buildPlayerContext);

            if (buildPlayerContext.TryGetResult(out var buildReport))
                RunTasks(postBuildTasks, PostBuildContext.Create(buildReport));

            if (buildReport.summary.result != BuildResult.Succeeded ||
                strictMode && buildReport.summary.totalErrors > 0)
                throw new BuildFailedException(buildReport);
        }

        private static void RunTasks(IReadOnlyList<IBuildTask> tasks, IBuildContext context)
        {
            foreach (var task in tasks)
                task.Run(context);
        }
    }
}
