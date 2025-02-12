# Builtin Task Generator

English: [README.md](./README.md)

これは、[UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference)のソースをもとに、`PlayerSettings`や`EditorUserBuildSettings`などに対して設定を行う`IBuildTask`実装を事前生成するツールです。

新しいUnityバージョンがリリースされ、[UnityCsReferenceにタグとして追加されたら](https://github.com/Unity-Technologies/UnityCsReference/tags)、本ツールを実行することで新しいバージョンに対する解析を行い、生成コードを更新します。

<!-- TOC -->
* [Builtin Task Generator](#builtin-task-generator)
  * [基本的な使い方](#基本的な使い方)
  * [解析結果のキャッシュ](#解析結果のキャッシュ)
  * [Unityバージョン](#unityバージョン)
  * [処理対象](#処理対象)
  * [現在の設定値の取得](#現在の設定値の取得)
  * [生成ルール](#生成ルール)
    * [処理対象に関するルール](#処理対象に関するルール)
    * [バージョン互換性に関するルール](#バージョン互換性に関するルール)
    * [`IProjectSettingApplier`実装に関するルール](#iprojectsettingapplier実装に関するルール)
    * [タスクパラメータ型に関するルール](#タスクパラメータ型に関するルール)
      * [シリアライズできない型の例外的対応](#シリアライズできない型の例外的対応)
  * [設定](#設定)
    * [`Apis`](#apis)
      * [`Ignored`](#ignored)
      * [`OverrideDisplayName`](#overridedisplayname)
    * [`DictionaryKeyTypes`](#dictionarykeytypes)
<!-- TOC -->

## 基本的な使い方

`generate`サブコマンドを使用します。`-o`オプションで生成ファイル (`.cs`) の出力先ディレクトリを指定します。

```shell
dotnet run -- generate -o ../../Packages/jp.co.cyberagent.buildmagic/BuildMagic/Editor/BuiltIn/Generated
```

## 解析結果のキャッシュ

本ツールは、UnityCsReferenceの各リリースバージョンに対して解析を行い、バージョン間の互換性をできるだけ維持できるようにコードを生成します。  
一度解析を行ったバージョンの解析結果は、ローカルにキャッシュし次回以降の解析で再利用することで高速化を図ります。  
キャッシュは以下のディレクトリに保存されます（macOSの場合）。

```
~/Library/Application Support/BuildMagic.BuiltinTaskGenerator/library
```

> [!NOTE]
> このパスは`Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`に基づきます。
  
キャッシュを無視してすべてのバージョンの解析を行う場合は、`-f`オプションを指定してください。

## Unityバージョン

Unity 2022.3.0f1以降のリリース(`f`)バージョンを処理対象としています。

## 処理対象
現時点での処理対象クラスは以下の通りです。 (かっこ内はエディタラベル解析に使用するクラス)

- `UnityEngine.PlayerSettings` (`UnityEditor.PlayerSettingsEditor`)
- `UnityEditor.EditorUserBuildSettings`

## 現在の設定値の取得

プロジェクトの現在の設定値が取得できる場合は、Configuration型に対し`BuildMagicEditor.IProjectSettingApplier`の実装を生成します。

## 生成ルール

### 処理対象に関するルール

- `static`かつ`public`かつジェネリックでないメソッド・プロパティのみを処理します。
- `set`アクセサのないプロパティは無視します。
- メソッド名の先頭が`Set`でないメソッドは無視します。
- パラメータ型にシリアライズできない型が含まれる項目は原則として無視します。
    - [一部、例外的に対応している型があります。](#シリアライズできない型の例外的対応)
- その項目が`[Obsolete]`である場合は、生成されるタスク型にも`[Obsolete]`属性が付与されます。
    - ただし、`[Obsolete(IsError: true)]`の項目は無視します。
- ネスト型がある場合は、そのネスト型のメンバーも処理します。
- `appsettings.json`で`Ignored`に指定されている項目は無視します。

### バージョン互換性に関するルール

- 処理対象のUnityバージョンのうち、その項目が初めて登場したバージョンの時点で`[Obsolete]`な項目は無視します。
- 同じ名前で`[Obsolete]`ではない項目が複数存在する場合（オーバーロードなど）は、それ以前のバージョンですでに登場している項目を優先します。
    - それでも優先できる項目がなければ、その名前の項目に対する処理はスキップします。

### `IProjectSettingApplier`実装に関するルール

- プロパティに`get`アクセサがある場合は、それを使用します。
- メソッド`SetHoge()`に対して`GetHoge()`が存在する場合は、それを使用します。
    - 引数の型と戻り値の型をみてオーバーロードのマッチングを行います。マッチングできない場合はスキップします。

### タスクパラメータ型に関するルール

- プロパティ・メソッドの引数をパラメータとして扱います。
- パラメータが複数存在する場合は、タプルとして扱います。
- `IProjectSettingApplier`で使用するゲッターにおいて入力すべきパラメータは、タスクパラメータ型においてディクショナリ (`IReadOnlyDictionary<,>`) のキーとして扱い、その他のパラメータをバリューとして扱います。
  - キーとなるパラメータが複数存在する場合は、`appsettings.json`の[`DictionaryKeyTypes`](#dictionarykeytypes)に指定された順番にキーを選択します。
  - タスク実行時には、それぞれのキーに対して値を設定します。
  - バリューとなるパラメータが複数存在する場合は、タプルとして扱います。

#### シリアライズできない型の例外的対応

- 一部、直接的にはシリアライズできないが、他のシリアライズ可能な型との相互型変換が行える型については、その型をパラメータ型として扱います。
    - 変換が可能な型のペアは`BuiltinSerializationWrapperRegistry.cs`にハードコードされています。


## 設定
`appsettings.json`を編集して、コード生成の詳細な設定を行えます。

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

各APIごとの設定を行います。

キーには[解析結果のキャッシュ](#解析結果のキャッシュ)のJSONファイルに記載されている`SetterExpression`を指定します。  
また、オーバーロードを一意に定めるため、`ParameterTypes`に各パラメータ型のフルネームを指定する必要があります。こちらも同じく解析結果のキャッシュから正しい型表現が得られます。

> [!IMPORTANT]
> `SetterExpression`は、実装上の都合により、先頭の`global::`を除いたものを指定する必要があります。

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

`true`を指定すると、生成対象から除外されます。

#### `OverrideDisplayName`

生成されるタスク型の表示名を上書きします。

### `DictionaryKeyTypes`

これは、パラメータ型のうち、ディクショナリのキーとして扱う型の優先順位を指定します。

例えば、`PlayerSettings.SetIcons(NamedBuildTarget, Texture2D[], IconKind)`のようなAPIでは、タスクパラメータの型は次のような構造になります。

```ts
type PlayerSettingsSetIconsParameters = {
    [buildTarget: NamedBuildTarget]: {
        [kind: IconKind]: {
            icons: Texture2D[];
        }
    }
};
```

これに対し、`DictionaryKeyTypes`を指定することで、ネストの順番を入れ替えられます。

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
