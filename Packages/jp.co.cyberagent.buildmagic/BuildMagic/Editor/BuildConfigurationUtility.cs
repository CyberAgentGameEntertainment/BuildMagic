// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;

namespace BuildMagicEditor
{
    internal static class BuildConfigurationUtility
    {
        internal static IList<IBuildConfiguration> ResolveConfigurations(
            IEnumerable<IBuildConfiguration> baseSchemeConfigurations,
            IEnumerable<IBuildConfiguration> derivedSchemeConfigurations)
        {
            var configurations = new List<IBuildConfiguration>(baseSchemeConfigurations);
            foreach (var derivedConfiguration in derivedSchemeConfigurations)
            {
                var index = configurations.FindIndex(c => c.TaskType == derivedConfiguration.TaskType);
                if (index >= 0)
                    configurations[index] = derivedConfiguration;
                else
                    configurations.Add(derivedConfiguration);
            }

            return configurations;
        }
    }
}
