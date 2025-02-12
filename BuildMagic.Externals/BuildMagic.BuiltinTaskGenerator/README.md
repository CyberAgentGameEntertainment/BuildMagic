# Builtin Task Generator

Japanese: [README.ja.md](./README.ja.md)

Buildin Task Generator is a tool that generates `IBuildTask` implementations that set settings for `PlayerSettings`, `EditorUserBuildSettings`, etc. based on the source code of [UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference).

When a new Unity version is released and added as a tag to [UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference), running this tool will analyze the new version and update the generated code.

<!-- TOC -->
* [Builtin Task Generator](#builtin-task-generator)
  * [Basic Usage](#basic-usage)
  * [Caching of Analysis Results](#caching-of-analysis-results)
  * [Unity Versions](#unity-versions)
  * [Processed Targets](#processed-targets)
  * [Getting Current Settings](#getting-current-settings)
  * [Generation Rules](#generation-rules)
    * [Rules for Processed Targets](#rules-for-processed-targets)
    * [Rules for Version Compatibility](#rules-for-version-compatibility)
    * [Rules for `IProjectSettingApplier` Implementation](#rules-for-iprojectsettingapplier-implementation)
    * [Rules for Task Parameter Types](#rules-for-task-parameter-types)
      * [Exceptional Behavior for Unserializable Types](#exceptional-behavior-for-unserializable-types)
  * [Configuration](#configuration)
    * [`Apis`](#apis)
      * [`Ignored`](#ignored)
      * [`OverrideDisplayName`](#overridedisplayname)
    * [`DictionaryKeyTypes`](#dictionarykeytypes)
<!-- TOC -->

## Basic Usage

Use the `generate` subcommand. Specify the output directory for the generated files (`.cs`) with the `-o` option.

```shell
dotnet run -- generate -o ../../Packages/jp.co.cyberagent.buildmagic/BuildMagic/Editor/BuiltIn/Generated
```

## Caching of Analysis Results

This tool analyzes each release version of UnityCsReference and generates code to maintain compatibility between versions as much as possible.  
The analysis results for each version are cached locally and reused in subsequent analyses to speed up the process.  
The cache is stored in the following directory (macOS).

```
~/Library/Application Support/BuildMagic.BuiltinTaskGenerator/library
```

> [!NOTE]
> This path is based on `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`.

To ignore the cache and perform analysis on all versions, specify the `-f` option.

## Target Unity Versions

This tool targets releases (`f` versions) of Unity 2022.3.0f1 and later.

## Processed Targets

The classes currently being processed are as follows. (The classes used for editor label analysis are in parentheses.)

- `UnityEngine.PlayerSettings` (`UnityEditor.PlayerSettingsEditor`)
- `UnityEditor.EditorUserBuildSettings`

## Getting Current Settings

If the current settings of the project can be obtained, an implementation of `BuildMagicEditor.IProjectSettingApplier` is generated for the BuildConfiguration type.

## Generation Rules

### Rules for Processed Targets

- Only static, public, and non-generic methods and properties are processed.
- Properties without a `set` accessor are ignored.
- Methods whose names do not start with `Set` are ignored.
- Items with parameter types that cannot be serialized are generally ignored.
    - [Some types that cannot be serialized are handled exceptionally.](#exceptional-behavior-for-unserializable-types)
- If the item is `[Obsolete]`, the generated task type will also have the `[Obsolete]` attribute.
    - However, items with `[Obsolete(IsError: true)]` are ignored.
- If there are nested types, the members of those nested types are also processed.
- Items specified in `appsettings.json` under `Ignored` are ignored.

### Rules for Version Compatibility

- Items that are `[Obsolete]` at the time they first appear in the Unity version being processed are ignored.
- If there are multiple items with the same name that are not `[Obsolete]` (e.g., overloads), the item that appeared earlier in a previous version is given priority.
    - If there is still no item that can be prioritized, processing for that item is skipped.

### Rules for `IProjectSettingApplier` Implementation

- If there is a `get` accessor in the property, it is used.
- If there is a `GetHoge()` method for the `SetHoge()` method, it is used.
    - Matching is done based on the types of the arguments and return values. If no match is found, it is skipped.

### Rules for Task Parameter Types

- The arguments of properties and methods are treated as parameters.
- If there are multiple parameters, they are treated as a tuple.
- In the `IProjectSettingApplier` getter to be used, the parameters to be entered are treated as dictionary keys in the task parameter type (`IReadOnlyDictionary<,>`) and the other parameters are treated as values.
    - If there are multiple key parameters, the keys are selected in the order specified in `DictionaryKeyTypes` in `appsettings.json`.
    - At runtime, values are set for each key.
    - If there are multiple value parameters, they are treated as a tuple.

#### Exceptional Behavior for Unserializable Types

- For some types that cannot be serialized directly but can be converted to and from other serializable types, the type is treated as a parameter type.
    - The pairs of convertible types are hardcoded in `BuiltinSerializationWrapperRegistry.cs`.


## Configuration

By editing `appsettings.json`, you can make detailed settings for code generation.

```json
{
  "Apis": {
    "UnityEditor.PlayerSettings.SetScriptingDefineSymbols({0}, {1});": {
      "ParameterTypes": [
        "global::UnityEditor.Build.NamedBuildTarget",
        "global::System.String"
      ],
      "Ignored": true
    },
    /* ... */
  },
  "DictionaryKeyTypes": [
    "global::UnityEditor.Build.NamedBuildTarget",
    "global::UnityEditor.BuildTarget",
    "global::UnityEditor.BuildTargetGroup",
    /* ... */
  ]
} 
```

### `Apis`

Here, you can configure each API.

The key is "SetterExpression" of the [analysis result cache](#caching-of-analysis-results) JSON file.  
To uniquely determine overloads, you need to specify the full names of each parameter type in `ParameterTypes`. The correct type representation can also be obtained from the analysis result cache.

> [!IMPORTANT]
> `SetterExpression` must be specified without the leading `global::` for implementation reasons.

```json
{
    "Apis": {
        "UnityEditor.PlayerSettings.SetIl2CppCompilerConfiguration({0}, {1});": {
            "ParameterTypes": [
                "global::UnityEditor.Build.NamedBuildTarget",
                "global::UnityEditor.Il2CppCompilerConfiguration"
            ],
            "Ignored": true,
            "OverrideDisplayName": "PlayerSettings: C++ Compiler Configuration (IL2CPP)"
        }
    }
}
```

#### `Ignored`

If `true` is specified, the target is excluded from generation.

#### `OverrideDisplayName`

Overrides the display name of the task type generated for the API.

### `DictionaryKeyTypes`

This specifies the priority of types to be treated as dictionary keys among the parameter types.

For example, the type structure of task parameters for an API like `PlayerSettings.SetIcons(NamedBuildTarget, Texture2D[], IconKind)` would be as follows.

```ts
type PlayerSettingsSetIconsParameters = {
    [buildTarget: NamedBuildTarget]: {
        [kind: IconKind]: {
            icons: Texture2D[];
        }
    }
};
```

You can change the order of nesting by specifying `DictionaryKeyTypes`.

```json
{
    "DictionaryKeyTypes": [
        "global::UnityEditor.IconKind",
        "global::UnityEditor.Build.NamedBuildTarget"
    ]
}
```

```ts
type PlayerSettingsSetIconsParameters = {
    [kind: IconKind]: {
        [buildTarget: NamedBuildTarget]: {
            icons: Texture2D[];
        }
    }
};
```
