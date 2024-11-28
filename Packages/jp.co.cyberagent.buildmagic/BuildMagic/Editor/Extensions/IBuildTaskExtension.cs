// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using UnityEditor;

namespace BuildMagicEditor.Extensions
{
    internal static class BuildTaskExtension
    {
        internal static BuildPlayerOptions GenerateBuildPlayerOptions(
            this IReadOnlyList<IBuildTask<IInternalPrepareContext>> internalPrepareTasks)
        {
            var optionBuilder = BuildPlayerOptionsBuilder.Create();

            foreach (var task in internalPrepareTasks)
                task.Run(InternalPrepareContext.Create(optionBuilder));

            return optionBuilder.Build();
        }
    }
}
