// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildMagicEditor
{
    public static class BuildSchemeLoader
    {
        private const string Extension = "json";
        private static readonly string BuildMagicDirectory = Path.Combine("ProjectSettings", "BuildMagic");

        public static void Save<T>(T scheme) where T : IBuildScheme
        {
            if (Directory.Exists(BuildMagicDirectory) == false)
                Directory.CreateDirectory(BuildMagicDirectory);

            var json = BuildSchemeSerializer.Serialize(scheme);
            var path = Path.Combine(BuildMagicDirectory, CreateFileName(scheme));
            File.WriteAllText(path, json);
        }

        internal static BuildScheme LoadTemplate()
        {
            var path = $"Packages/jp.co.cyberagent.buildmagic/BuildMagic.Window/Editor/Assets/Templates/BuiltinTemplate.json";
            if (File.Exists(path) == false)
                throw new FileNotFoundException("BuiltinTemplate.json is not found.");
            
            var json = File.ReadAllText(path);
            return BuildSchemeSerializer.Deserialize<BuildScheme>(json);
        }

        public static T Load<T>(string name) where T : IBuildScheme
        {
            var fileName = CreateFileName(name);
            var path = Path.Combine(BuildMagicDirectory, fileName);

            if (Directory.Exists(BuildMagicDirectory) == false || File.Exists(path) == false)
                throw new FileNotFoundException($"{fileName} is not found.");

            var json = File.ReadAllText(path);
            return BuildSchemeSerializer.Deserialize<T>(json);
        }

        public static IEnumerable<T> LoadAll<T>() where T : IBuildScheme
        {
            if (Directory.Exists(BuildMagicDirectory) == false)
                return Enumerable.Empty<T>();

            var files = Directory.GetFiles(BuildMagicDirectory, $"*.{Extension}");
            return files.Select(file =>
            {
                var json = File.ReadAllText(file);
                return BuildSchemeSerializer.Deserialize<T>(json);
            });
        }

        public static void Remove(BuildScheme scheme)
        {
            Remove(scheme.Name);
        }

        public static void Remove(string name)
        {
            var path = Path.Combine(BuildMagicDirectory, CreateFileName(name));
            File.Delete(path);
        }

        private static string CreateFileName(IBuildScheme scheme)
        {
            return CreateFileName(scheme.Name);
        }

        private static string CreateFileName(string name) => $"{name}.{Extension}";
    }
}
