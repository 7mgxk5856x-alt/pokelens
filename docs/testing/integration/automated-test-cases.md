# 統合テストケース一覧（自動テスト）

pokelens で実装済みの**自動・統合テスト**を、テストファイル・シナリオ単位でまとめたドキュメント。複数コンポーネントを通したデータフロー全体が正しく動作するかを俯瞰するための参照資料。

> 単体テストは [../unit/automated-test-cases.md](../unit/automated-test-cases.md) に分離。手動（E2E）テストの仕様書は [../e2e/manual-test-cases.md](../e2e/manual-test-cases.md) を参照。

- **テストフレームワーク**: xUnit（.NET 8）
- **対象**: `tools/PokelensTools.Tests/PipelineIntegrationTests.cs`
- **実行コマンド**: `dotnet test tools/PokelensTools.Tests`

> このドキュメントはテストコードから手動で抽出したスナップショットです。テスト追加・変更時は併せて更新してください。

## 表の項目

| 項目 | 説明 |
|---|---|
| ケースID | 一意な識別子。`<ファイル接頭辞>-<連番>` 形式 |
| 種別 | ここではすべて統合テスト |
| テスト観点 | 何を確認したいか |
| 入力値 | 実際に用意する入力（一時ディレクトリ上のファイル群） |
| 期待結果 | 生成される出力ファイルの内容 |
| 備考 | 補足 |

**ファイル接頭辞**: PIT=PipelineIntegrationTests

## サマリ

| テストファイル | テスト対象 | ケース数 |
|---|---|---:|
| [PipelineIntegrationTests.cs](#pipelineintegrationtestscs) | パイプライン全体（`MergeConverter.Convert` / `PatchApplicator.Apply`） | 2 |
| **合計** | | **2** |

---

## PipelineIntegrationTests.cs

**テスト対象**: パイプライン全体（`MergeConverter.Convert` / `PatchApplicator.Apply`）の統合。各 Step の入力 JSON を一時ディレクトリに用意し、出力 JSON が正しく生成されるかを通しで検証する。

### パイプライン統合シナリオ

> 入力ファイル群（Showdown 各データ・翻訳・modifiers・各パッチ）を実ファイルとして書き出し、変換・パッチ適用を実行して出力ファイルの内容を検証する。**種別はすべて統合テスト。**

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PIT-001 | 統合 | 変換パイプラインが正しい出力を生成 | showdown 各 JSON ＋ 翻訳 ＋ modifiers を一時ディレクトリに用意し `MergeConverter.Convert` | `pokedex.json`（`name='ピカチュウ'`, `hp=35`, abilities `['せいでんき','ひらいしん']`）／ `moves.json`（`'10まんボルト'` power=90, type=Electric）／ `items.json`（`'こだわりスカーフ'`）／ `abilities.json`（`'てつのこぶし'`） | 全出力を一括検証 |
| PIT-002 | 統合 | champions-patch が種族値を上書き | pokedex ＋ champions-patch `{pikachu.baseStats.atk:99}` で `PatchApplicator.Apply` | `atk=99`、`hp=35` 据え置き | パッチ統合 |
