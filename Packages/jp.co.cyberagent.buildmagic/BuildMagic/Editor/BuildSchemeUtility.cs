// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildMagicEditor
{
    internal static class BuildSchemeUtility
    {
        /// <summary>
        ///     Loads the base schemes in leaf-to-root order.
        /// </summary>
        /// <param name="scheme"></param>
        /// <returns></returns>
        /// <exception cref="CommandLineArgumentException"></exception>
        public static IEnumerable<BuildScheme> EnumerateSchemeTreeFromLeafToRoot(BuildScheme scheme,
            IEnumerable<BuildScheme> allSchemes)
        {
            static IEnumerable<BuildScheme> LoadBaseSchemesCore(BuildScheme scheme, IEnumerable<BuildScheme> allSchemes,
                HashSet<BuildScheme> visited)
            {
                if (!visited.Add(scheme)) throw new InvalidOperationException("Circular inheritance detected!");

                yield return scheme;

                if (string.IsNullOrEmpty(scheme.BaseSchemeName)) yield break;

                var baseScheme = allSchemes.FirstOrDefault(s => s.Name == scheme.BaseSchemeName);
                if (baseScheme == null)
                    throw new InvalidOperationException($"No such base scheme found: {scheme.BaseSchemeName}");

                foreach (var ancestor in LoadBaseSchemesCore(baseScheme, allSchemes, visited))
                    yield return ancestor;
            }

            return LoadBaseSchemesCore(scheme, allSchemes, new HashSet<BuildScheme>());
        }

        public static IEnumerable<IBuildConfiguration> EnumerateComposedConfigurations<TContext>(
            BuildScheme scheme,
            IEnumerable<BuildScheme> allSchemes) where TContext : IBuildContext
        {
            return EnumerateComposedConfigurations<TContext>(EnumerateSchemeTreeFromLeafToRoot(scheme, allSchemes));
        }

        public static IEnumerable<IBuildConfiguration> EnumerateComposedConfigurations<TContext>(
            IEnumerable<IBuildScheme> treeFromLeafToRoot) where TContext : IBuildContext
        {
            HashSet<Type> taskTypes = new();

            foreach (var scheme in treeFromLeafToRoot)
            {
                if (typeof(TContext).IsAssignableFrom(typeof(IPreBuildContext)))
                    foreach (var configuration in scheme.PreBuildConfigurations)
                        if (taskTypes.Add(configuration.TaskType))
                            yield return configuration;

                if (typeof(TContext).IsAssignableFrom(typeof(IInternalPrepareContext)))
                    foreach (var configuration in scheme.InternalPrepareConfigurations)
                        if (taskTypes.Add(configuration.TaskType))
                            yield return configuration;

                if (typeof(TContext).IsAssignableFrom(typeof(IPostBuildContext)))
                    foreach (var configuration in scheme.PostBuildConfigurations)
                        if (taskTypes.Add(configuration.TaskType))
                            yield return configuration;
            }
        }
    }
}
