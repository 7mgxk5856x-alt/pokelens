# プロジェクト用語集 (Glossary)

**更新日**: 2026-05-13

---

## ドメイン用語

### 火力指数

**定義**: 自分ポケモンの各技が持つ攻撃力を相対比較するための指標値。実際のダメージ量ではない。

**計算式**:
- 物理技: `威力 × 攻撃実数値 × タイプ一致補正 × 特性補正 × 持ち物補正`
- 特殊技: `威力 × 特攻実数値 × タイプ一致補正 × 特性補正 × 持ち物補正`
- 変化技: 計算しない（表示は「−」）

**英語表記**: Power Index

**関連用語**: [タイプ一致補正](#タイプ一致補正)、[特性補正](#特性補正)、[持ち物補正](#持ち物補正)

**実装**: `src/logic/power-index-calc.js`

---

### 実数値

**定義**: ポケモンの各能力値（HP・攻撃・防御・特攻・特防・素早さ）の実際の数値。種族値・能力ポイント・性格などを基にゲーム内で算出される。

**計算式** (Pokémon Champions):
- 最大HP: `種族値 + 能力ポイント + 75`
- 攻撃・防御・特攻・特防・素早さ: `floor((種族値 + 能力ポイント + 20) × 性格補正)`

**説明**: pokelens では自分パーティの実数値は `data/party.json` の能力ポイント・性格・マスターデータの種族値から計算する。相手ポケモンの素早さ4パターンも同じ式で算出する。詳細は機能設計書の「実数値計算式」セクションを参照。

**英語表記**: Actual Stat

**実装**: `src/logic/calc-actual-stats.js`

**関連用語**: [種族値](#種族値)、[能力ポイント](#能力ポイント)

---

### 種族値

**定義**: ポケモンの種族ごとに固定された能力の基礎値。同じ種族なら個体差によらず一定。

**説明**: 相手ポケモンの情報表示に使用する。Pokémon Showdown データから取得。

**英語表記**: Base Stat (BST: Base Stat Total は合計値)

**関連用語**: [実数値](#実数値)、[素早さ4パターン](#素早さ4パターン)

---

### 素早さ4パターン

**定義**: 相手ポケモンが取りうる素早さ実数値の代表的な4通り。先手・後手の判断基準として使用する。

**各パターン（Pokémon Champions 計算式）**:

| パターン名 | 能力ポイント | 性格補正 | 説明 |
|-----------|------------|---------|------|
| 最速 | 32 | 1.1 | 素早さに最大投資＋プラス性格 |
| 準速 | 32 | 1.0 | 素早さに最大投資・性格補正なし |
| 無振り | 0 | 1.0 | 素早さ無投資・性格補正なし |
| 最遅 | 0 | 0.9 | 素早さ最小（トリックルーム用想定） |

**計算式**: `floor((種族値 + 能力ポイント + 20) × 性格補正)`

**こだわりスカーフ補正の扱い**: 機能 15（P0.5）で 6 パターン → 4 パターンへ簡素化。こだわりスカーフ補正後の値は「最速 × 1.5」「準速 × 1.5」で暗算容易なため一覧から除外し、自分側のスカーフ持ちポケモンについては機能 16（`OwnPokemonDetail`）で素早さ実数値に併記する。素早さ補正倍率は `data/items.json`（マスターデータ・`items-modifiers.json` パッチ由来）の `こだわりスカーフ.modifier.spe` から取得し、`loader.getItemModifier()` 経由で参照する（倍率定数のハードコードを避け、ゲーム仕様の値変更に追従しやすくする）。

**関連用語**: [種族値](#種族値)、[能力ポイント](#能力ポイント)

**実装**: `src/logic/speed-calc.js`

---

### 耐久指数

**定義**: ポケモンの「耐えやすさ」を一つの数値で表現する指標。物理耐久指数と特殊耐久指数の 2 種類がある。

**計算式**:
- 物理耐久指数 = HP 実数値 × 防御実数値
- 特殊耐久指数 = HP 実数値 × 特防実数値

**表示形式**:
- **自分側**（`OwnPokemonDetail`）: 実数値から算出した 2 値を、種族値行・実数値行の右隣に縦に整列して表示する。
- **相手側**（`OpponentPokemonDetail`）: 種族値だけから 4 パターン × 2 種類 = 8 値を網羅表示する（耐久指数4パターン）。

**英語表記**: Endurance Index

**実装**: `src/logic/endurance-index-calc.js`（`calcEnduranceIndex` / `calcEnduranceIndexPatterns`）

**関連用語**: [耐久指数4パターン](#耐久指数4パターン)、[実数値](#実数値)

---

### 耐久指数4パターン

**定義**: 相手ポケモンの耐久指数として、能力ポイントと性格補正の組み合わせ 4 通り × 物理/特殊 2 種類 = 8 値を網羅した代表値。

**各パターン**:

| パターン | 物理耐久指数の前提 | 特殊耐久指数の前提 |
|---|---|---|
| 耐久特化 | H32 / B32、防御↑(×1.1) | H32 / D32、特防↑(×1.1) |
| 耐久極振 | H0 / B32、防御↑(×1.1) | H0 / D32、特防↑(×1.1) |
| H極振 | H32 / B0、性格補正なし | H32 / D0、性格補正なし |
| 無振り | H0 / B0、性格補正なし | H0 / D0、性格補正なし |

> HP は性格補正の対象外（`calcHp` の式に性格補正は含まれない）。

**表示**: 列ヘッダ「耐久特化 / 耐久極振 / H極振 / 無振り」、行ヘッダ「物理耐久指数 / 特殊耐久指数」の表形式。

**関連用語**: [耐久指数](#耐久指数)、[種族値](#種族値)、[能力ポイント](#能力ポイント)

**実装**: `src/logic/endurance-index-calc.js` の `calcEnduranceIndexPatterns(baseStats)`

---

### メガシンカ

**定義**: 戦闘中にメガストーンを持ったポケモンが一時的に強化形態（メガフォーム）に変化する仕様。pokelens では選出補助のため、メガシンカ前後の種族値・タイプ・特性・火力指数・耐久指数を切り替えて参照できる（機能 7）。

**状態モデル**: 通常 / メガシンカ の 2 状態。複数メガを持つポケモン（リザードン X/Y 等）は「通常 → メガ(a) → メガ(b) → 通常」の順に循環する。状態はメモリのみで管理され、リロードで初期化される。

**ボタン表示条件**:
- 自分パーティ: 持ち物が当該ポケモンに対応するメガストーンの場合のみ
- 相手パーティ: メガシンカ可能なポケモンであれば常時（持ち物未知のため）

**関連用語**: [メガストーン](#メガストーン)、[メガフォーム](#メガフォーム)

**実装**: `src/data/mega-evolutions.json`（マッピング）、`src/data/loader.js` の `getMegaInfo` / `isMegaForm` / `getMegaFormData`、`src/ui/own-party-panel.js` / `src/ui/opponent-party-panel.js`（切替 UI）

---

### メガストーン

**定義**: メガシンカの発動条件となる持ち物の総称。日本語では「〜ナイト」（例: フシギバナイト、リザードナイトＸ）として表現される。

**説明**: pokelens では火力指数に直接影響しないため `data/items.json` のパッチ対象外。`src/data/mega-evolutions.json` の親ポケモンエントリの `stones` 配列で「持ち物 → メガシンカ可能か」の判定に使用する。

**関連用語**: [メガシンカ](#メガシンカ)、[メガフォーム](#メガフォーム)

---

### メガフォーム

**定義**: メガシンカ後のポケモン形態。種族値・タイプ・特性が通常状態から変化する。例: 「メガフシギバナ」「メガリザードンＸ」。

**マスターデータ上の扱い**: 現状の `data/pokedex.json` ではメガフォームを独立エントリで保持しているが、機能 7 の論理層では「親に紐づくサブデータ」として扱う:
- `loader.getPokemonByName('メガフシギバナ')` は `null` を返す（`party.json` でメガ名指定は不明扱い）
- 相手ポケモンサジェスト候補にメガ名は含まれない
- メガフォームの種族値・タイプ・特性は `loader.getMegaFormData(メガ名)` で取得する

**関連用語**: [メガシンカ](#メガシンカ)、[メガストーン](#メガストーン)

---

### タイプ一致補正（STAB）

**定義**: 技のタイプとポケモンのタイプが一致する場合に火力指数に乗算される係数（1.5）。一致しない場合は1.0。

**英語表記**: STAB（Same Type Attack Bonus）

**関連用語**: [火力指数](#火力指数)

---

### 特性補正

**定義**: ポケモンの特性によって火力指数に乗算される係数。例: ちからもち（atk×2.0）、てつのこぶし（物理技 atk×1.2）。

**説明**: 未定義の特性（pokelens に未登録）は1.0として計算する。「最大火力指向」のためタイプ・技タグで判定できる条件はマスターデータに登録するが、対戦状況依存（天候・状態異常・ターン数・性別）はマスターデータに含めず補正値1.0として扱う。補正値はマスターデータ（`data/abilities.json`）から参照する。

> ピンチ特性（もうか・しんりょく・げきりゅう・むしのしらせ等）はタイプ条件（`isType`）として登録し、HP条件は無視して常時発動として扱う（最大火力指向ポリシー）。

**condition 種類**（条件付き補正。詳細仕様は[機能設計書](./functional-design.md)のエンティティ定義を参照）:

| condition | 判定内容 | 対象例 |
|-----------|---------|--------|
| `isType` | 技タイプが指定タイプと一致 | シルクのスカーフ・もくたん・もうか（→Fire）・しんりょく（→Grass） |
| `isPunch` | パンチ技（`move.tags` に `"isPunch"`） | てつのこぶし・パンチグローブ |
| `isPulse` | 波動技（`move.tags` に `"isPulse"`） | メガランチャー |
| `isBite` | かみつき技（`move.tags` に `"isBite"`） | がんじょうあご |
| `isRecoil` | 反動技（`move.tags` に `"isRecoil"`） | すてみ |
| `isSlice` | 切断技（`move.tags` に `"isSlice"`） | きれあじ |
| `isContact` | 接触技（`move.tags` に `"isContact"`） | タフクロー |
| `isSound` | 音技（`move.tags` に `"isSound"`） | パンクロック |
| `hasSecondary` | 追加効果あり技（`move.tags` に `"hasSecondary"`） | ちからずく |
| `powerMax60` | 技の威力が60以下 | テクニシャン |
| `isStab` | タイプ一致技（ポケモンのタイプと比較） | てきおうりょく（STAB倍率を2.0に上書き） |
| `convertNormalTo` | ノーマル技のタイプを `convertedType` に変換（STAB 計算に反映） | ピクシレート（→Fairy）・フリーズスキン（→Ice）・スカイスキン（→Flying）・エレキスキン（→Electric） |
| `convertAllTo` | 全技のタイプを `convertedType` に変換（STAB 計算に反映） | ノーマライズ（→Normal） |

**関連用語**: [火力指数](#火力指数)

---

### 持ち物補正

**定義**: ポケモンが持つ道具によって火力指数に乗算される係数。例: いのちのたま1.3。

**説明**: 未定義の持ち物は1.0として計算する。補正値はマスターデータ（`data/items.json`）から参照する。持ち物で使用される `condition` は `isType`（タイプ一致／例: シルクのスカーフ・もくたん）・`isPunch`（パンチ技／例: パンチグローブ）・`isStab`（タイプ一致技／例: たつじんのおび）の 3 種類。特性のみで使われる condition（`isPulse` / `isBite` / `isRecoil` / `isSlice` / `isContact` / `isSound` / `hasSecondary` / `powerMax60` / `convertNormalTo` / `convertAllTo`）は持ち物には存在しない。詳細仕様は[機能設計書](./functional-design.md)のエンティティ定義を参照。

> 素早さ補正のみの持ち物（こだわりスカーフ等）は火力指数に影響しないため、`items-modifiers.json` には含めない（SKIP ポリシー）。

**関連用語**: [火力指数](#火力指数)

---

### 能力ポイント

**定義**: Pokémon Champions における、ポケモンの各能力値に割り振れる成長ボーナス。0〜32の範囲で指定する。

**説明**: メインシリーズには存在しない独自仕様。素早さ計算式 `floor((種族値 + 能力ポイント + 20) × 性格補正)` で使用する。最大値は32（最速・準速パターンに相当）。`party.json` の `abilityPoints` フィールドに各ステータス分を入力する。

**関連用語**: [素早さ4パターン](#素早さ4パターン)、[実数値](#実数値)

---

### 性格補正

**定義**: ポケモンの性格によって実数値計算に乗算される係数。上昇補正 1.1 / 無補正 1.0 / 下降補正 0.9 の3種類。

**説明**: HP には性格補正が適用されない。各性格の補正対象ステータスと倍率は `data/natures.json` に手書きで管理する。補正のない性格（がんばりや等の無補正性格）は全ステータスに 1.0 を適用する。`party.json` の `nature` フィールドに日本語名で入力する。

**関連用語**: [実数値](#実数値)、[能力ポイント](#能力ポイント)

---

### 名前検索

**定義**: ポケモン名のひらがな・カタカナ表記を正規化し、前方一致でポケモンを検索する機能。

**説明**: 入力文字列を以下のルールで正規化してから `data/pokedex.json` を前方一致検索する。
- ひらがな → カタカナ（例: `ぴかちゅう` → `ピカチュウ`）
- 半角カタカナ → 全角カタカナ（例: `ｳｰﾗｵｽ` → `ウーラオス`）
- 長音符（`ー`）・括弧（`（）`）・記号は変換対象外

**英語表記**: Name Search

**実装**: `src/logic/name-search.js`

---

### 自分パーティ

**定義**: ユーザーが対戦で使用する、事前に定義した6匹のポケモンチーム。`data/party.json` に保存され、アプリ起動時に読み込まれる。

**説明**: pokelens では1パーティのみ管理する。変更する場合はJSONファイルを手編集し、ブラウザをリロードする。

**関連用語**: [相手パーティ](#相手パーティ)

---

### 相手パーティ

**定義**: 対戦中に相手が選出してくる、最大6匹のポケモンチーム。`SearchInput`（サジェスト検索入力）でポケモン名を入力して登録し、ページリロードで初期化される（永続化しない）。

**関連用語**: [自分パーティ](#自分パーティ)

**関連ドキュメント**: [機能設計書 - OpponentParty エンティティ定義](./functional-design.md)

---

## 技術用語

### Pokémon Showdown データ

**定義**: ポケモン対戦シミュレーターサービス「Pokémon Showdown」が公開しているポケモン情報のデータセット。種族値・技・特性などを網羅する。

**本プロジェクトでの用途**: C# データ準備ツールがこのデータを取得・変換し、`data/pokedex.json` / `data/moves.json` / `data/items.json` / `data/abilities.json` として出力する。

**関連ドキュメント**: [アーキテクチャ設計書](./architecture.md)

---

### マスターデータ

**定義**: Pokémon Showdown データを pokelens 用に変換した参照用 JSON ファイル群。`data/pokedex.json` / `data/moves.json` / `data/items.json` / `data/abilities.json` の4ファイルで構成される。

**説明**: C# ツールを実行することで生成される。ランタイムに変更されることはない。ポケモン名（カナ・ひらがな）・タイプ・種族値・特性候補・技情報・補正データを含む。`data/types.json`（タイプ名変換）と `data/move-categories.json`（技分類変換）は手書き管理ファイルであり、マスターデータには含まれない。

**関連ドキュメント**: [機能設計書 - データモデル](./functional-design.md#データモデル定義)

---

### C# データ準備ツール

**定義**: Pokémon Showdown からデータを取得し、`data/pokedex.json` / `data/moves.json` / `data/items.json` / `data/abilities.json` を生成するための C# (.NET) 製 CLI ツール。

**実行タイミング**: 対戦前に1回、またはデータ更新が必要な時のみ実行する。ランタイムには不要。

**実装**: `tools/PokelensTools/`

---

### 増分実行

**定義**: C# データ準備ツールが変化のあったファイルに応じて必要な最小ステップのみを再実行する仕組み。

**説明**: `cache/checksums.json` に `showdown-*.json` / `pokeapi-translations.json` および `Patches/` 配下の全パッチ・modifier ファイルのハッシュ値を保存し、次回起動時と比較する。変化したファイルの種類に応じて Step2 以降の起動位置が決まる。Step1（Showdown データ取得）は変化の有無に関わらず毎回必ず実行され、取得後のハッシュ比較結果によって Step2 以降のスキップを決定する。変化なしの場合は Step2 以降を全スキップする（`data/` は最新のまま）。スキップ条件の詳細は[機能設計書](./functional-design.md)の「増分実行の仕組み（ハッシュ比較）」および[アーキテクチャ設計書](./architecture.md)を参照。

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### champions-patch.json

**定義**: Pokémon Champions 独自のポケモンデータ（名称変更・新規追加など）を Showdown データに差分適用するための手書き管理 JSON ファイル。

**配置**: `tools/PokelensTools/Patches/champions-patch.json`

**説明**: C# データ準備ツール実行時にマージされ、最終的な `data/pokedex.json` / `data/moves.json` に反映される。変更時は[開発ガイドライン](./development-guidelines.md)の C# コーディング規約内の管理ルールに従うこと。

**関連ドキュメント**: [アーキテクチャ設計書](./architecture.md)

---

### moves-power-patch.json

**定義**: Pokémon Showdown で `power: null`（威力不定）となる技に最大威力を定義するための手書き管理 JSON ファイル。

**配置**: `tools/PokelensTools/Patches/moves-power-patch.json`

**説明**: `champions-patch.json`（Showdown 語彙・Step3 適用）とは異なり、最終出力語彙（`moves.json` の `power` フィールド）でStep4 に適用される。複数回攻撃技は C# ツールが `basePower × multihit[1]` で自動計算するため本ファイルの対象外。パッチ未定義の威力不定技は UI 上「−」表示となる。

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### pokemon-name-patch.json

**定義**: PokéAPI のフォルム認識ロジックでは一意化できないポケモンの日本語名を手動で上書き定義する JSON ファイル。

**配置**: `tools/PokelensTools/Patches/pokemon-name-patch.json`

**説明**: Showdown キーをキー、上書き後の日本語名を値とする単純な `{ "key": "name" }` 形式。Step4 で MergeConverter が `pokeapi-translations.json` 由来の日本語名を解決した直後に、本ファイルのエントリで上書きする。補正対象は以下の 4 カテゴリ:
- PokéAPI の翻訳データバグ（パルデアケンタロス3種が同名で返る等）
- PokéAPI の `form_names` が空（バトルボンドゲッコウガ等）
- PokéAPI に該当エンドポイントがない（オーガポン Tera フォルム、Showdown 専用偽メガ進化等）
- スラグ不一致による fallback の衝突（メテノ Core/Meteor、ピチュー ギザみみ等）

最終出力 `pokedex.json` 内で同名重複が残らないよう、すべての値が一意であることが望ましい。`_comment` 等 `_` 始まりのキーは Showdown キーと衝突しないためコメント用途に使える。

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### item-name-patch.json

**定義**: PokéAPI が翻訳を欠落／誤訳しているアイテムの日本語名を手動で上書き定義する JSON ファイル。

**配置**: `tools/PokelensTools/Patches/item-name-patch.json`

**説明**: Showdown キーをキー、上書き後の日本語名を値とする単純な `{ "key": "name" }` 形式。MergeConverter は `items-modifiers.json` の各キーに対し、まず本ファイルを参照して上書き値があれば優先採用、なければ `pokeapi-translations.json` をフォールバックとして使用する。補正対象は以下の 3 カテゴリ:
- PokéAPI 翻訳の誤り（ケッサクのちゃわんが PokéAPI ja で "ボンサクのちゃわん" を返す等のデータバグ）
- PokéAPI に該当 slug の翻訳が未登録（メタルアロイ→ふくごうきんぞく等）
- PokéAPI に該当 slug の項目自体が無い（きれいなハネ等）

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### items-modifiers.json / abilities-modifiers.json

**定義**: 持ち物・特性ごとの補正値（倍率・適用条件）を定義する手書き管理 JSON ファイル。C# データ準備ツールが `data/items.json` / `data/abilities.json` を生成する際の入力として使用する。

**配置**: `tools/PokelensTools/Patches/items-modifiers.json`、`tools/PokelensTools/Patches/abilities-modifiers.json`

**データ構造（例）**:

**`items-modifiers.json` の例**（持ち物の補正定義）:
```json
{
  "lifeorb":    { "modifier": { "atk": 1.3, "spa": 1.3 } },
  "choiceband": { "modifier": { "atk": 1.5 } },
  "expertbelt": { "modifier": { "atk": 1.2, "spa": 1.2, "condition": "isStab" } }
}
```

**`abilities-modifiers.json` の例**（特性の補正定義）:
```json
{
  "ironfist":     { "modifier": { "condition": "isPunch", "atk": 1.2, "spa": 1.2 } },
  "adaptability": { "modifier": { "condition": "isStab", "stab": 2.0 } },
  "pixilate":     { "modifier": { "condition": "convertNormalTo", "convertedType": "Fairy", "atk": 1.2, "spa": 1.2 } }
}
```

**説明**: キーは Showdown 英語キー。`condition` は省略可能（省略時は全技に無条件適用）。`"isStab"` / `"isPunch"` 等の場合は技の条件を満たす場合のみ適用。UI 上での補正計算（`calcPowerIndex`）では `DataLoader.getItemModifier()` / `DataLoader.getAbilityModifier()` 経由で参照される。

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### ポケモンWiki

**定義**: ポケモンに関する情報を集積した日本語 Wiki サイト（https://wiki.pokemonwiki.com/wiki/Pok%C3%A9mon_Champions）。

**本プロジェクトでの用途**: Pokémon Champions における種族値変更・技威力変更などの差分を手動調査するために参照する。調査結果は `tools/PokelensTools/Patches/champions-patch.json` に静的パッチとして記録する。ランタイムへの影響はない（オフライン参照のみ）。

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### PokéAPI

**定義**: ポケモンに関するデータを提供するオープンソース REST API。

**本プロジェクトでの用途**: C# データ準備ツール実行時に、ポケモン名の英語→日本語カナ名変換のためにのみ使用する。種族値・技などの本体データは Pokémon Showdown データから取得する。

**制約**: 実行時にネットワーク接続が必要。オフライン環境では C# ツールの日本語名変換が動作しない。

**関連ドキュメント**: [アーキテクチャ設計書](./architecture.md)

---

### Vite

**定義**: フロントエンド向けビルドツール・開発サーバー。ES モジュールをネイティブサポートし、高速な HMR（Hot Module Replacement）を提供する。

**本プロジェクトでの用途**: ローカル開発サーバーとして使用。`file://` プロトコルでは fetch が CORS 制限を受けるため、必ず Vite 経由でアプリを開く。

**バージョン**: ^6.0.0

**関連ドキュメント**: [アーキテクチャ設計書](./architecture.md)

---

### Vitest

**定義**: Vite ベースの JavaScript/TypeScript テストフレームワーク。Jest 互換の API を持つ。

**本プロジェクトでの用途**: `src/logic/` 配下の純粋関数および `src/data/loader.js`（DataLoader）のユニットテストに使用（`tests/unit/`）。実行: `npm test`。

**バージョン**: ^2.0.0

---

### Playwright

**定義**: Microsoft が開発する E2E テスト自動化フレームワーク。実ブラウザ（Chromium / Firefox / WebKit）を駆動して UI 結合動作を検証する。

**本プロジェクトでの用途**: `tests/e2e/` 配下の E2E 自動テストに使用（Chromium のみ）。`playwright.config.js` の `webServer` で `npm run dev` を自動起動し、`page.route('**/data/party.json', ...)` で party.json を mock 注入する設計。実行: `npm run test:e2e`。初回セットアップに `npx playwright install chromium`（〜100MB）が必要。

**バージョン**: ^1.60.0

**関連ドキュメント**: [docs/testing/e2e/automated-test-cases.md](./testing/e2e/automated-test-cases.md)

---

### xUnit

**定義**: .NET 用のユニットテストフレームワーク。`[Fact]` / `[Theory]` 属性でテストケースを宣言する。

**本プロジェクトでの用途**: `tools/PokelensTools.Tests/` の C# テスト（ユニット＋統合）に使用。実行: `dotnet test tools/PokelensTools.Tests`。

---

### テストケース ID 接頭辞

**定義**: 各テストファイル・テスト種別を区別するための ID 接頭辞。`docs/testing/` 配下の仕様書で `<接頭辞>-<連番>` 形式で採番される。

| 接頭辞 | 種別 | 対応ファイル |
|--------|------|------------|
| **AET** | Automated E2E Test（Playwright） | `tests/e2e/*.spec.js`（仕様書: `docs/testing/e2e/automated-test-cases.md`） |
| **MET** | Manual E2E Test（手動セットアップ） | 仕様書: `docs/testing/e2e/manual-test-cases.md` |
| **PIT** | Pipeline Integration Tests（C# 統合） | `tools/PokelensTools.Tests/PipelineIntegrationTests.cs` |
| CAS | calc-actual-stats（JS 単体） | `tests/unit/calc-actual-stats.test.js` |
| PIC | power-index-calc（JS 単体） | `tests/unit/power-index-calc.test.js` |
| SPD | speed-calc（JS 単体） | `tests/unit/speed-calc.test.js` |
| RMOD | resolve-modifier（JS 単体） | `tests/unit/resolve-modifier.test.js` |
| NS | name-search（JS 単体） | `tests/unit/name-search.test.js` |
| LD | loader（JS 単体） | `tests/unit/loader.test.js` |
| MCT | MergeConverterTests（C# 単体） | `tools/PokelensTools.Tests/MergeConverterTests.cs` |
| PAT | PatchApplicatorTests（C# 単体） | `tools/PokelensTools.Tests/PatchApplicatorTests.cs` |
| IRT | IncrementalRunnerTests（C# 単体） | `tools/PokelensTools.Tests/IncrementalRunnerTests.cs` |
| PFT | PokeAPIFetcherTests（C# 単体） | `tools/PokelensTools.Tests/PokeAPIFetcherTests.cs` |
| PNM | PokeApiNameTests（C# 単体） | `tools/PokelensTools.Tests/PokeApiNameTests.cs` |
| PSL | PokeApiSlugTests（C# 単体） | `tools/PokelensTools.Tests/PokeApiSlugTests.cs` |
| SFT | ShowdownFetcherTests（C# 単体） | `tools/PokelensTools.Tests/ShowdownFetcherTests.cs` |

---

## アーキテクチャ用語

### 純粋関数

**定義**: 同じ引数に対して常に同じ結果を返し、外部の状態を変更しない（副作用のない）関数。

**本プロジェクトでの適用**: `src/logic/` 配下の全関数は純粋関数として実装する。DOM・グローバル変数・外部APIに依存しないため、テストが容易。

**メリット**: テストが簡単（入力と出力のみ検証）、デバッグが容易、予測可能な動作

**関連用語**: [ロジックレイヤー](#ロジックレイヤー)

---

### ロジックレイヤー

**定義**: 計算・検索・データ変換を担う純粋関数のモジュール群。DOM操作とデータ読み込みから分離されている。

**実装**: `src/logic/`（`power-index-calc.js`, `speed-calc.js`, `name-search.js`, `calc-actual-stats.js`, `resolve-modifier.js`, `constants.js`）

**依存関係**: `src/ui/` からのみ呼び出し可能。`src/data/` を直接 import しない。計算に必要なデータは UIレイヤーが DataLoader から取得して引数として渡す設計。

---

### UIレイヤー

**定義**: DOM操作・イベント処理・画面表示を担うモジュール群。

**実装**: `src/ui/`（`own-party-panel.js`, `own-pokemon-detail.js`, `opponent-party-panel.js`, `opponent-pokemon-detail.js`, `search-input.js`, `dom-utils.js`, `stat-labels.js`）

**依存関係**: `src/logic/` と `src/data/` を呼び出す。

---

### データアクセスレイヤー

**定義**: JSON ファイルの読み込みと解析を担うモジュール。UIレイヤーにデータを提供する。

**実装**: `src/data/`（`loader.js`）— `DataLoader` クラスとして実装される。

**DataLoader の主なメソッド**: `load()`, `getPokemonByName()`, `searchByName()`, `getMove()`, `getItemModifier()`, `getAbilityModifier()`, `getTypeName()`, `getMoveCategory()`, `getNatureModifiers()`。詳細は[機能設計書](./functional-design.md)の DataLoader セクションを参照。

**依存関係**: `src/ui/` からのみ呼び出される。`src/logic/` には依存しない。

---

### ステアリングファイル

**定義**: `/add-feature` コマンド実行時に `.steering/YYYYMMDD-<機能名>/` 以下に生成される一時的な作業記録ファイル群。

**ファイル構成**:
```
.steering/20260512-party-display/
├── requirements.md   # 今回の作業要件
├── design.md         # 変更内容の設計
└── tasklist.md       # タスク進捗（[ ] / [x]）
```

**永続化**: gitignore 済み。作業完了後は参照のみ。永続ドキュメント（`docs/`）には含まれない。

---

## 略語一覧

**能力値フィールド名**（`party.json` の `abilityPoints` キーおよび「実数値（H-A-B-C-D-S）」表記との対応）。略字は `src/ui/stat-labels.js` の `STAT_LABELS` で定義され、UI 表示順は H → A → B → C → D → S：

| 略字 | フィールド名 | 英語 | 日本語 |
|------|------------|------|-------|
| `H` | `hp` | Hit Points | HP |
| `A` | `atk` | Attack | 攻撃 |
| `B` | `def` | Defense | 防御（B は伝統表記） |
| `C` | `spa` | Special Attack | 特攻（C は伝統表記） |
| `D` | `spd` | Special Defense | 特防 |
| `S` | `spe` | Speed | 素早さ |

**略語一覧**:

| 略語 | 正式名称 | 説明 |
|------|---------|------|
| STAB | Same Type Attack Bonus | タイプ一致補正 |
| BST | Base Stat Total | 種族値合計 |
| ESM | ECMAScript Modules | JavaScript の標準モジュール仕様 |
| HMR | Hot Module Replacement | モジュール変更時にページリロードなしで反映する仕組み |
| MVP | Minimum Viable Product | 最小限の動作可能プロダクト。P0 と同義 |
| P0 | Priority 0 | 最優先スコープ（MVP）。必須機能 |
| P0.5 | Priority 0.5 | P0完了後・P1着手前に対応するサブセット |
| P1 | Priority 1 | 次フェーズの重要機能 |
| P2 | Priority 2 | 将来機能（対戦状況の動的反映） |
| P3 | Priority 3 | 将来機能（UX・開発体験の向上） |
| PRD | Product Requirements Document | プロダクト要求定義書 |

---

## 索引

### あ行
- [相手パーティ](#相手パーティ)

### か行
- [火力指数](#火力指数)

### さ行
- [実数値](#実数値)
- [自分パーティ](#自分パーティ)
- [種族値](#種族値)
- [純粋関数](#純粋関数)
- [性格補正](#性格補正)
- [素早さ4パターン](#素早さ4パターン)
- [ステアリングファイル](#ステアリングファイル)
- [増分実行](#増分実行)

### た行
- [タイプ一致補正（STAB）](#タイプ一致補正stab)
- [耐久指数](#耐久指数)
- [耐久指数4パターン](#耐久指数4パターン)
- [データアクセスレイヤー](#データアクセスレイヤー)
- [特性補正](#特性補正)

### な行
- [名前検索](#名前検索)
- [能力ポイント](#能力ポイント)

### は行
- [ポケモンWiki](#ポケモンwiki)

### ま行
- [マスターデータ](#マスターデータ)
- [メガシンカ](#メガシンカ)
- [メガストーン](#メガストーン)
- [メガフォーム](#メガフォーム)
- [持ち物補正](#持ち物補正)

### ら行
- [ロジックレイヤー](#ロジックレイヤー)

### A-Z
- [C# データ準備ツール](#c-データ準備ツール)
- [champions-patch.json](#champions-patchjson)
- [moves-power-patch.json](#moves-power-patchjson)
- [pokemon-name-patch.json](#pokemon-name-patchjson)
- [item-name-patch.json](#item-name-patchjson)
- [items-modifiers.json / abilities-modifiers.json](#items-modifiersjson--abilities-modifiersjson)
- [PokéAPI](#pokéapi)
- [Pokémon Showdown データ](#pokémon-showdown-データ)
- [Playwright](#playwright)
- [テストケース ID 接頭辞](#テストケース-id-接頭辞)
- [UIレイヤー](#uiレイヤー)
- [Vite](#vite)
- [Vitest](#vitest)
- [xUnit](#xunit)
