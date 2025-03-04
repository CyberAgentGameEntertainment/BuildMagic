# Changelog

## [1.7.0]

### Added

- Make internal prepare phase visible with an option.
- `-override` now supports numbers other than `int` and `float`.

### Fixed

- Fix API updater runs on built-in task sources and causes compilation errors.

## [1.6.0]

### Added

- Build scheme can be inherited in multiple levels.
- Add a task to copy Firebase configuration file.
- Boolean parameters are now supported by `-override`.

### Changed

- Build report is now saved by default.
- English comments and READMEs.

### Fixed

- Fixed equal sign `=` in `-override` parameter is not correctly parsed.
- Fixed output path isn't set correctly when built through Build Settings window

## [1.5.1]

- エディタがビルド時にクラッシュする場合があるのを回避するため、`BuildOptions.DetailedBuildReport` を `BUILDMAGIC_NO_DETAILED_BUILD_REPORT` シンボルで無効化できるようにしました。
- 不要な README.md.meta を削除しました。

## [1.5.0]

- Build Settings / Build Profiles ウィンドウからのビルド時にビルドタスクを実行できるようになりました
- CLIパラメータによるビルドタスクの追加が可能になりました
- strict モード（ビルド中に1件以上のエラーログが発生すると、ビルド自体の成否に関わらず、BuildMagicを失敗扱いとするモード）を追加しました
- ビルドスキームの継承機能を追加しました
- Internal Prepareフェーズで実行されるAndroidのカスタム署名タスクを追加しました
- ビルドスキームのサンプルを追加しました
- iOSAddCapabilityTaskのInspectorが正しく表示されない問題を修正しました

## [1.4.0]

- 実験的な diff ビューワーを追加しました
- Unity標準と同等なビルド結果ログを出力するようにしました
- Managed Code Stripping のレポート出力を有効化する EnableLinkerReportTask を追加しました
- Managed Code Stripping のレポートを指定したディレクトリにコピーする SaveLinkerReportTask を追加しました
- ウィンドウを開いたままUnityを再起動すると、unsaved changes detected ダイアログが出てしまう問題を修正しました

## [1.3.0]

- ビルドコンフィグレーション選択のドロップダウンの見た目を調整しました
- 重複したビルドコンフィグレーションの登録の禁止するように挙動を修正しました
- 自動生成されたタスクの表示名を手動で上書きする機能の実装しました
- Unity起動時に選択中のビルドスキームのプレビルド処理を適用するように修正しました
- 見た目の調整を行い、ビルドコンフィグレーションの可読性を向上させました
- iOSモジュールがインストールされてない状態でコンパイルエラーが発生する不具合の修正しました

## [1.2.1]

- カスタムタスクのコンフィグレーションをビルドスキームに設定したとき、プレビルドやビルドが失敗する不具合を修正しました。

## [1.2.0]

- CLIでのビルド成果物の出力先を、`-output` で指定する方式に変更しました。
- BuildMagic Windowでのビルド時の成果物の出力先を、ダイアログで指定する方式に変更しました。
- ビルトインタスクに、特定のUPMパッケージの依存を削除するプレビルドタスクを追加しました。
- ビルトインタスクに、特定のアセットやディレクトリを削除するプレビルドタスクを追加しました。
- ビルトインタスクに、`EditorBuildSettings` の各種プロパティを設定できるプレビルドタスクを追加しました。
- ビルトインタスクに、iOSのCapabilityを設定するポストビルドタスクを追加しました。
- ビルドに含めるシーンを設定するタスクを、プレビルドタスクに変更しました。

## [1.1.0]

- ビルドの出力先が指定されてない場合、デフォルトパスに出力するようにしました。
- ビルドフェーズを調整しました。
- Unity起動時に、自動的に選択中のビルドスキーマを適用するようにしました
- ビルドレポートを出力するポストビルドタスクをビルトインタスクとして用意しました。
- ビルドレポートがエラーを出力する場合にCLIのExitCodeが`1`を返すように修正しました。

## [1.0.3] - 2024-06-08

- CLI経由でのビルドが失敗する不具合を修正しました。
- `BuildPlayerOptionsScenesPreBuildConfiguration` の保持するシーンが正しく保存されない不具合を修正しました。

## [1.0.2] - 2024-06-06

- `BuildPlayerOptionsScenesPreBuildConfiguration` の保持するシーン一覧を、シーン名ではなくシーンアセットの参照に変更しました。
- Configurationのクラスを登録した状態で、該当クラスの名前や名前空間の変更（もしくは削除もおそらく）したら、SchemeからConfigurationをRemoveできなくなる不具合を修正しました。

## [1.0.1] - 2024-05-24

- 自動生成されたビルトインタスクのうち、ディクショナリのキーに変換が必要な型が使われているものが例外をスローして実行に失敗する問題を修正しました。

## [1.0.0] - 2024-05-22

- 初回リリース
