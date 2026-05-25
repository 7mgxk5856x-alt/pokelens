# 統合テストケース一覧（自動テスト）

pokelens で実装済みの**自動・統合テスト**を、テストファイル・シナリオ単位でまとめたドキュメント。複数コンポーネントを通したデータフロー全体が正しく動作するかを俯瞰するための参照資料。

> 単体テストは [../unit/automated-test-cases.md](../unit/automated-test-cases.md) に分離。手動（E2E）テストの仕様書は [../e2e/manual-test-cases.md](../e2e/manual-test-cases.md) を参照。

- **テストフレームワーク**: xUnit（.NET 8）
- **対象**: `tools/PokelensMasterDataBuilder.Tests/PipelineIntegrationTests.cs`
- **実行コマンド**: `dotnet test tools/PokelensMasterDataBuilder.Tests`

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
| [PipelineIntegrationTests.cs](#pipelineintegrationtestscs) | パイプライン全体（`MergeConverter.Convert`（`NestMegaForms` 含む）/ `PatchApplicator.Apply`） | 14 |
| **合計** | | **14** |

---

## PipelineIntegrationTests.cs

**テスト対象**: パイプライン全体（`MergeConverter.Convert`（メガシンカネスト処理 `NestMegaForms` を含む）/ `PatchApplicator.Apply`）の統合。各 Step の入力 JSON を一時ディレクトリに用意し、出力 JSON が正しく生成されるかを通しで検証する。

> **PIT-010 スコープ判断**: PRD 269「対応メガシンカ用アイテム名はマスターデータに含まれる」は `data/pokedex.json` の `megaForms[].item` フィールド（PIT-009 で担保）で充足される。`data/items.json` は `items-modifiers.json` 登録ありのアイテムのみ出力する設計のため、メガストーン（補正値なし）は `items.json` に出力されない。したがって `data/items.json` 側のメガストーン出力検証ケース（PIT-010）は不要と判断した。

### パイプライン統合シナリオ

> 入力ファイル群（Showdown 各データ・翻訳・modifiers・各パッチ）を実ファイルとして書き出し、変換・パッチ適用を実行して出力ファイルの内容を検証する。**種別はすべて統合テスト。**
>
> **パイプラインステップ関係**: `MergeConverter.Convert()` は Step 1 相当の入力読み込み（cache/showdown-*.json + pokeapi-translations.json）→ Step 4 相当の変換出力（data/*.json）を一体で実行し、その中で `NestMegaForms` (機能 7) を呼ぶ。`PatchApplicator.Apply()` は Step 3 相当の champions-patch.json 適用を単独で実行し、cache を上書きする。両者を独立に呼ぶケース（PIT-002/004）と、`MergeConverter.Convert()` のみ呼ぶケース（PIT-001/003/005-015）で経路が異なる。
>
> **共通前提（入力ファイル）**: `MergeConverter.Convert()` を呼ぶケースでは `showdown-pokedex.json` / `showdown-moves.json` / `showdown-items.json` / `showdown-abilities.json` / `pokeapi-translations.json` の 5 ファイルが必須。テスト観点と無関係なものは空 `{}` で書き込む。
>
> **PIT-010 欠番**: スコープ判断（冒頭ブロック参照）により対象外として欠番化済み。番号は将来用途まで保留する。

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PIT-001 | 統合 | 変換パイプラインが正しい出力を生成（modifier 内容まで担保） | showdown 各 JSON ＋ 翻訳 ＋ modifiers ＋ 空パッチ群を一時ディレクトリに用意し `MergeConverter.Convert` | `pokedex.json`（`name='ピカチュウ'`, `hp=35`, abilities `['せいでんき','ひらいしん']`）／ `moves.json`（`'10まんボルト'` power=90, `type==='Electric'`（英語のまま））／ `items.json`（`'こだわりスカーフ'`, `modifier.spe=1.5`）／ `abilities.json`（`'てつのこぶし'`, `modifier.condition='isPunch'`） | 全出力を一括検証。`moves.json` の `type` を英語のまま保持するのはフロントエンドが `types.json` で日本語化するアーキテクチャのため |
| PIT-002 | 統合 | champions-patch が種族値を上書き（pokedex セクション） | `showdown-pokedex.json` + `showdown-moves.json`(空 `{}`) + `champions-patch.json`(`{pokedex:{pikachu:{baseStats:{atk:99}}}}`) のみ用意し `PatchApplicator.Apply()` 単独呼出（`MergeConverter.Convert()` は呼ばない） | `showdown-pokedex.json` の `pikachu.baseStats.atk=99`、`hp=35` は据え置き | Step3 単独検証 |
| PIT-003 | 統合 | `moves-power-patch.json` が威力不定技の power を補完しフルパイプライン後の出力に反映 | `showdown-moves.json` に `return`（`basePower:0` の威力不定技）＋ `moves-power-patch.json`（`{return:{power:102}}`）＋ 翻訳 `{moves:{return:'おんがえし'}}` ＋ `showdown-pokedex.json`(空 `{}`) / `showdown-items.json`(空 `{}`) / `showdown-abilities.json`(空 `{}`) で `MergeConverter.Convert()` | `moves.json` の `'おんがえし'.power===102` | PRD 機能6（威力不定技のパッチ補完）。Step4 経由でも反映される連鎖を担保。<br>※ optional パッチ群（`items-modifiers.json` / `abilities-modifiers.json` / `pokemon-name-patch.json` / `item-name-patch.json`）はファイル不在で OK（`ReadOptionalJson` が空オブジェクト相当として扱う） |
| PIT-004 | 統合 | champions-patch の moves セクションが basePower を上書き | `showdown-moves.json`(`thunder` basePower=110, accuracy=70) ＋ `showdown-pokedex.json`(空) ＋ `champions-patch.json`(`{moves:{thunder:{basePower:120}}}`) で `PatchApplicator.Apply()` | `showdown-moves.json` の `thunder.basePower===120`、`accuracy===70` / `type==='Electric'` 据え置き | PRD 機能6（moves セクションパッチ）。Step3 単独検証 |
| PIT-005 | 統合 | `pokemon-name-patch.json` の日本語名上書きが最終出力 pokedex.json に反映 | showdown 各 JSON ＋ 翻訳（`pikachu`→`'ピカチュウ'`）＋ `pokemon-name-patch.json`（`{pikachu:'パートナーピカチュウ'}`）で `MergeConverter.Convert()` | `pokedex.json` の `pikachu.name==='パートナーピカチュウ'`（翻訳由来名を上書き） | 機能設計（ポケモン名パッチ） |
| PIT-006 | 統合 | `item-name-patch.json` が翻訳辞書に無い持ち物の日本語名を補完 | `pokeapi-translations.json` の `items` に `choicescarf` を含めない ＋ `items-modifiers.json`（`choicescarf:{modifier:{spe:1.5}}`）＋ `item-name-patch.json`（`{choicescarf:'こだわりスカーフ'}`）で `MergeConverter.Convert()` | `items.json` に `'こだわりスカーフ'` キーが存在 | 機能設計（持ち物名パッチ・PokéAPI 翻訳バグ補正） |
| PIT-007 | 統合 | 連続技（multihit）の最大総威力 `basePower×multihit[1]` が最終出力 `moves.json` に反映される | `showdown-moves.json` に `doubleslap`（`basePower:15, multihit:[2,5]`）＋ 翻訳 `{moves:{doubleslap:'ダブルビンタ'}}` ＋ 他必須 showdown 入力（空 `{}`）で `MergeConverter.Convert()` | `moves.json` の `'ダブルビンタ'.power===75`（=15×5） | PRD 機能6（multihit 最大総威力）。MCT-009/010 は変換層単体のため、Step4 経由でも最終出力へ反映される連鎖を担保 |
| PIT-008 | 統合・異常系 | 翻訳辞書に未登録のポケモンは最終出力 pokedex.json から除外される | `showdown-pokedex.json` に `pikachu`（翻訳あり）と `unknownpoke`（翻訳なし）を定義し `MergeConverter.Convert()` | `pokedex.json` に `'pikachu'` は含まれ、`'unknownpoke'` は含まれない | 統合層での除外動作の担保（MCT-005 は単体層） |
| PIT-009 | 統合 | メガフォームが親エントリの `megaForms[]` にネストされ、トップレベルから削除される（D-1） | `showdown-pokedex.json` に `venusaur`（翻訳あり）＋ `venusaurmega`（翻訳あり、`forme:'Mega'`）。`showdown-moves.json`（空 `{}`）＋ `showdown-items.json` に `venusaurite`（`megaStone:{Venusaur:'Venusaur-Mega'}`）＋ `showdown-abilities.json`（空 `{}`）。翻訳に `items.venusaurite:'フシギバナイト'` を含めて `MergeConverter.Convert()` | `pokedex.json` の `venusaur.megaForms[0]` が `{key:'venusaurmega', name:'メガフシギバナ', item:'フシギバナイト', baseStats.atk:100, abilities:['あついしぼう']}`、`venusaurmega` がトップレベルに存在しない | 機能 7 / D-1 の Convert() 統合担保。MCT-024/025 は NestMegaForms 単体のため統合経路の連鎖を補完 |
| PIT-010 | — | スコープ対象外（冒頭スコープ判断参照） | — | — | 欠番。`items.json` にメガストーンは出力されない設計のため不要 |
| PIT-011 | 統合 | メガ名・メガストーン名の半角 X/Y → 全角 Ｘ/Ｙ 正規化が Convert() 経由で最終出力に反映される（D-8） | `showdown-pokedex.json` に `charizardmegax`/`charizardmegay`（`forme:'Mega-X'`/`'Mega-Y'`）＋ `showdown-moves.json`（空 `{}`）＋ `showdown-items.json` に `charizarditex`/`charizarditey`（megaStone 対応）＋ `showdown-abilities.json`（空 `{}`）。翻訳辞書に**半角** X/Y で `'メガリザードンX'`/`'メガリザードンY'`/`'リザードナイトX'`/`'リザードナイトY'` を含めて `MergeConverter.Convert()` | 出力 `megaForms[].name` / `.item` がいずれも全角 Ｘ/Ｙ で、半角 X/Y を含まない | D-8 全角正規化の Convert() 統合担保。MCT-027/028 は単体層 |
| PIT-012 | 統合 | 複数メガ形態を持つポケモンは `megaForms[]` に全形態を格納する（D-1 配列形式維持） | `showdown-pokedex.json` に `charizard` + `charizardmegax`/`charizardmegay` ＋ `showdown-moves.json`（空 `{}`）＋ `showdown-items.json` に 2 つの charizardite ＋ `showdown-abilities.json`（空 `{}`）。翻訳は**全角** Ｘ/Ｙ で記述（PIT-011 と異なり配列構造検証に集中するため、全角正規化は別ケースに委ねる）。`MergeConverter.Convert()` | `charizard.megaForms` が長さ 2 で `key='charizardmegax'` と `key='charizardmegay'` を両方含む。両メガ独立エントリはトップレベルから削除されている | D-1 複数メガの連鎖担保。MCT-026 は単体層。翻訳に全角入力を使うのは「配列構造（複数件・キー一致）の検証」と「全角正規化」を独立した観点として分けるため（全角正規化検証は PIT-011 に委譲） |
| PIT-013 | 統合 | `ItemlessMegas` のメガ（メガレックウザ）が最終 `pokedex.json` で `item: null` のメガフォームとして親 `rayquaza.megaForms[]` にネストされる（D-4） | `showdown-pokedex.json` に `rayquaza` + `rayquazamega`（`forme:'Mega'`）＋ `showdown-moves.json`（空 `{}`）＋ `showdown-items.json`（空 `{}`、rayquazamega に対応する megaStone 無し）＋ `showdown-abilities.json`（空 `{}`）で `MergeConverter.Convert()` | `pokedex.json` に `rayquazamega` キー無し。`rayquaza.megaForms[0]` が `{key:'rayquazamega', name:'メガレックウザ', item:null, baseStats.atk=180, abilities=['デルタストリーム']}` を含む | D-4 簡略仕様の Convert() 統合担保。MCT-034 は単体層。技習得チェックは行わない |
| PIT-014 | 統合・境界値 | メガ独立エントリのみ存在して親が pokedex に欠落しているケースでも例外を throw せず、親生成なし・メガ独立エントリも結果として残らない | `showdown-pokedex.json` に `venusaurmega` のみ（`forme:'Mega'`、親 `venusaur` 無し）＋ `showdown-moves.json`（空）＋ `showdown-items.json` に `venusaurite`（megaStone 持ち）＋ `showdown-abilities.json`（空）。翻訳に `venusaurmega:'メガフシギバナ'` のみで `MergeConverter.Convert()` | (1) 例外を throw しないこと（xUnit のパス自体が暗黙的に担保。`Convert()` を try-catch なしで呼び出すため、例外が出れば xUnit がテスト FAIL を出す）。(2) `pokedex.json` に `venusaur` キー無し（勝手に生成しない）。(3) `venusaurmega` キーも無し（孤立メガとして除去） | データ不整合への防御。MCT-031 の IT 版。エラー推測 |
| PIT-015 | 統合・境界値 | 単一メガでも `megaForms` は常に配列形式（JsonArray、長さ 1）で出力される（D-1 配列形式維持） | PIT-009 と同等の単一メガ入力（venusaur + venusaurmega + venusaurite）で `MergeConverter.Convert()`。**ただし内容の正確性は PIT-009 が網羅検証するため、本ケースは型確認に必要な最小入力のみ用意する（特性翻訳の `chlorophyll` 等は省略）** | `venusaur.megaForms` が `JsonArray` 型、`Count === 1` | D-1「単一メガでも常に配列を維持」の不変条件を統合層で担保。MCT-024 は単体層。**型確認のみが本ケースの責務であり、`megaForms[0]` のフィールド内容検証は PIT-009 に委譲（重複検証を避け、観点を独立化）** |
