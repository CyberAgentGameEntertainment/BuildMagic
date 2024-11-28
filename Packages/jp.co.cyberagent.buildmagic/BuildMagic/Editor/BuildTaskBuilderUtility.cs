// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace BuildMagicEditor
{
    internal static class BuildTaskBuilderUtility
    {
        public static IBuildTask<TContext>[] CreateBuildTasks<TContext>(IEnumerable<IBuildConfiguration> configurations)
            where TContext : IBuildContext
        {
            var provider = BuildTaskBuilderProvider.CreateDefault();
            return configurations
                   .Select(c =>
                   {
                       var property = c.GatherProperty();
                       var builder = provider.GetBuilder(c.TaskType, property.ValueType);
                       return builder.Build(property.Value);
                   })
                   .OfType<IBuildTask<TContext>>()
                   .ToArray();
        }
    }
}
