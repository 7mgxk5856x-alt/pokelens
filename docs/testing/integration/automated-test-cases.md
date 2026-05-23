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
| [PipelineIntegrationTests.cs](#pipelineintegrationtestscs) | パイプライン全体（`MergeConverter.Convert` / `PatchApplicator.Apply`） | 7 |
| **合計** | | **7** |

---

## PipelineIntegrationTests.cs

**テスト対象**: パイプライン全体（`MergeConverter.Convert` / `PatchApplicator.Apply`）の統合。各 Step の入力 JSON を一時ディレクトリに用意し、出力 JSON が正しく生成されるかを通しで検証する。

### パイプライン統合シナリオ

> 入力ファイル群（Showdown 各データ・翻訳・modifiers・各パッチ）を実ファイルとして書き出し、変換・パッチ適用を実行して出力ファイルの内容を検証する。**種別はすべて統合テスト。**

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PIT-001 | 統合 | 変換パイプラインが正しい出力を生成（modifier 内容まで担保） | showdown 各 JSON ＋ 翻訳 ＋ modifiers ＋ 空パッチ群を一時ディレクトリに用意し `MergeConverter.Convert` | `pokedex.json`（`name='ピカチュウ'`, `hp=35`, abilities `['せいでんき','ひらいしん']`）／ `moves.json`（`'10まんボルト'` power=90, type=Electric）／ `items.json`（`'こだわりスカーフ'`, `modifier.spe=1.5`）／ `abilities.json`（`'てつのこぶし'`, `modifier.condition='isPunch'`） | 全出力を一括検証。`moves.json` の `type` は英語のまま（フロントエンドが `types.json` で日本語化するアーキテクチャ） |
| PIT-002 | 統合 | champions-patch が種族値を上書き（pokedex セクション） | `showdown-pokedex.json` + `showdown-moves.json`(空 `{}`) + `champions-patch.json`(`{pokedex:{pikachu:{baseStats:{atk:99}}}}`) のみ用意し `PatchApplicator.Apply()` 単独呼出（`MergeConverter.Convert()` は呼ばない） | `showdown-pokedex.json` の `pikachu.baseStats.atk=99`、`hp=35` は据え置き | Step3 単独検証 |
| PIT-003 | 統合 | `moves-power-patch.json` が威力不定技の power を補完しフルパイプライン後の出力に反映 | `showdown-moves.json` に `return`（`basePower:0` の威力不定技）＋ `moves-power-patch.json`（`{return:{power:102}}`）＋ 翻訳 `{moves:{return:'おんがえし'}}` ＋ `showdown-pokedex.json`(空 `{}`) / `showdown-items.json`(空 `{}`) / `showdown-abilities.json`(空 `{}`) で `MergeConverter.Convert()` | `moves.json` の `'おんがえし'.power===102` | PRD 機能6（威力不定技のパッチ補完）。Step4 経由でも反映される連鎖を担保 |
| PIT-004 | 統合 | champions-patch の moves セクションが basePower を上書き | `showdown-moves.json`(`thunder` basePower=110, accuracy=70) ＋ `showdown-pokedex.json`(空) ＋ `champions-patch.json`(`{moves:{thunder:{basePower:120}}}`) で `PatchApplicator.Apply()` | `showdown-moves.json` の `thunder.basePower===120`、`accuracy===70` / `type==='Electric'` 据え置き | PRD 機能6（moves セクションパッチ）。Step3 単独検証 |
| PIT-005 | 統合 | `pokemon-name-patch.json` の日本語名上書きが最終出力 pokedex.json に反映 | showdown 各 JSON ＋ 翻訳（`pikachu`→`'ピカチュウ'`）＋ `pokemon-name-patch.json`（`{pikachu:'パートナーピカチュウ'}`）で `MergeConverter.Convert()` | `pokedex.json` の `pikachu.name==='パートナーピカチュウ'`（翻訳由来名を上書き） | 機能設計（ポケモン名パッチ） |
| PIT-006 | 統合 | `item-name-patch.json` が翻訳辞書に無い持ち物の日本語名を補完 | `pokeapi-translations.json` の `items` に `choicescarf` を含めない ＋ `items-modifiers.json`（`choicescarf:{modifier:{spe:1.5}}`）＋ `item-name-patch.json`（`{choicescarf:'こだわりスカーフ'}`）で `MergeConverter.Convert()` | `items.json` に `'こだわりスカーフ'` キーが存在 | 機能設計（持ち物名パッチ・PokéAPI 翻訳バグ補正） |
| PIT-008 | 統合・異常系 | 翻訳辞書に未登録のポケモンは最終出力 pokedex.json から除外される | `showdown-pokedex.json` に `pikachu`（翻訳あり）と `unknownpoke`（翻訳なし）を定義し `MergeConverter.Convert()` | `pokedex.json` に `'pikachu'` は含まれ、`'unknownpoke'` は含まれない | 統合層での除外動作の担保（MCT-005 は単体層） |
