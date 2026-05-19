// --------------------------------------------------------------
// Smoke test for BuildMagic.BuiltinTasks.Generators
// Runs the SG against a synthetic UnityEditor.PlayerSettings / EditorUserBuildSettings
// inside a "BuildMagic.Editor" assembly and dumps the generated source.
// --------------------------------------------------------------

using System.Collections.Generic;
using BuildMagic.BuiltinTasks.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

const string fakeUnitySource = """
namespace UnityEngine
{
    public class Object { }
    public class Texture2D : Object { }
    namespace Rendering { public enum GraphicsDeviceType { Vulkan, Metal } }
}

namespace UnityEditor
{
    public enum BuildTarget { StandaloneWindows, iOS, Android }
    public enum IconKind { Application, Settings, Notification }
    public enum AndroidSdkVersions { AndroidApiLevel29 = 29 }
    public enum Il2CppCompilerConfiguration { Debug, Master, Release }

    namespace Build
    {
        public struct NamedBuildTarget { public string Name { get; } }
    }

    public static class PlayerSettings
    {
        public static string companyName { get; set; } = "";
        public static string productName { get; set; } = "";
        public static int defaultScreenWidth { get; set; }
        public static bool runInBackground { get; set; }

        [System.Obsolete]
        public static bool legacyFlag { get; set; }

        // error:true obsolete must be skipped entirely (compiler refuses to bind).
        [System.Obsolete("gone", true)]
        public static bool fatallyObsoleteFlag { get; set; }

        public static void SetDefaultShaderChunkCount(int chunkCount) { }

        // Multi-param with key — should weave into IReadOnlyDictionary<BuildTarget, GraphicsDeviceType[]>
        public static void SetGraphicsAPIs(UnityEditor.BuildTarget platform, UnityEngine.Rendering.GraphicsDeviceType[] graphicsAPIs) { }
        public static UnityEngine.Rendering.GraphicsDeviceType[] GetGraphicsAPIs(UnityEditor.BuildTarget platform) => null!;

        // Triple-param with two keys — should weave into nested dictionary
        public static void SetIcons(UnityEditor.Build.NamedBuildTarget buildTarget, UnityEngine.Texture2D[] icons, UnityEditor.IconKind kind) { }
        public static UnityEngine.Texture2D[] GetIcons(UnityEditor.Build.NamedBuildTarget buildTarget, UnityEditor.IconKind kind) => null!;

        // Both overloads carry NamedBuildTarget — the (string) variant is Ignored by override,
        // so the (string[]) variant should be the sole surviving candidate after filtering and
        // therefore the one emitted.
        public static void SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget buildTarget, string defines) { }
        public static void SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget buildTarget, string[] defines) { }

        // Override displayname
        public static void SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget buildTarget, UnityEditor.Il2CppCompilerConfiguration configuration) { }
        public static UnityEditor.Il2CppCompilerConfiguration GetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget buildTarget) => default;

        public static class Android
        {
            public static bool disableDepthAndStencilBuffers { get; set; }
            public static UnityEditor.AndroidSdkVersions minSdkVersion { get; set; }
        }
    }

    public static class EditorUserBuildSettings
    {
        public static bool development { get; set; }
        public static bool allowDebugging { get; set; }

        // Overloaded — should be skipped (no NamedBuildTarget overload).
        public static void SetPlatformSettings(string platform, string name, string value) { }
        public static void SetPlatformSettings(string buildTargetGroup, string platform, string name, string value) { }

        // Overloaded with no lockfile entry — under strict mode the SG cannot decide which
        // overload is the historically-prior one, so it must skip emission entirely. The
        // previous NamedBuildTarget heuristic is gone.
        public static void SetThing(string name, string value) { }
        public static void SetThing(UnityEditor.Build.NamedBuildTarget buildTarget, string value) { }

        // Overloaded — lockfile pins the (NamedBuildTarget, string, int) variant below. This is
        // the only way the SG can pick a winner among same-NBT overloads.
        public static void SetLockedThing(UnityEditor.Build.NamedBuildTarget buildTarget, string value) { }
        public static void SetLockedThing(UnityEditor.Build.NamedBuildTarget buildTarget, string value, int extra) { }

        // Dictionary-of-tuple regression (mirror of PlayerSettings.XboxOne.SetSocketDefinition):
        // string key + tuple-of-primitives value. The serialized form is an anonymous container
        // struct, which only has a scalar implicit conversion to its real ValueTuple — at
        // dictionary value level the cast Dictionary<K, Struct> → IReadOnlyDictionary<K, (...)>
        // fails. WeavedTuple.ToBuildExpression must always emit the explicit tuple expression
        // so the enclosing dictionary takes the element-by-element ToDictionary path.
        public static void SetSocketDef(string name, int port, int retries) { }
        public static int GetSocketDef(string name, out int retries) { retries = 0; return 0; }
    }
}

namespace BuildMagicEditor
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateBuildTaskAccessoriesAttribute : Attribute
    {
        public GenerateBuildTaskAccessoriesAttribute(string displayName) { }
        public string PropertyName { get; set; } = "";
    }

    public interface IPreBuildContext { }
    public abstract class BuildTaskBase<T> { public abstract void Run(T context); }
    public interface IProjectSettingApplier { void ApplyProjectSetting(); }
    public struct NamedBuildTargetSerializationWrapper
    {
        public static implicit operator UnityEditor.Build.NamedBuildTarget(NamedBuildTargetSerializationWrapper w) => default;
        public static implicit operator NamedBuildTargetSerializationWrapper(UnityEditor.Build.NamedBuildTarget t) => default;
    }
}
""";

var syntaxTree = CSharpSyntaxTree.ParseText(fakeUnitySource);

var references = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
    .Select(a => MetadataReference.CreateFromFile(a.Location))
    .Cast<MetadataReference>()
    .ToArray();

var compilation = CSharpCompilation.Create(
    assemblyName: "BuildMagic.Editor",
    syntaxTrees: new[] { syntaxTree },
    references: references,
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

// Inject a lockfile entry pinning the 3-parameter SetLockedThing overload, so the SG
// can't fall back to the NamedBuildTarget heuristic (both overloads have it).
ApiSignatureLock.Entries = new Dictionary<string, string[]>
{
    ["EditorUserBuildSettingsSetLockedThingTask"] = new[]
    {
        "global::UnityEditor.Build.NamedBuildTarget",
        "global::System.String",
        "global::System.Int32",
    },
};

var generator = new BuiltInTasksGenerator();
var driver = CSharpGeneratorDriver.Create(generator);
driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

var result = driver.GetRunResult();
foreach (var diag in result.Diagnostics)
    Console.Error.WriteLine($"DIAG: {diag}");

foreach (var generatorResult in result.Results)
{
    Console.WriteLine($"=== Generator: {generatorResult.Generator.GetGeneratorType().Name} ===");
    foreach (var src in generatorResult.GeneratedSources)
    {
        Console.WriteLine($"--- {src.HintName} ---");
        Console.WriteLine(src.SourceText);
    }
}
