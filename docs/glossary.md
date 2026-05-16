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

**説明**: pokelens では自分パーティの実数値は `data/party.json` の能力ポイント・性格・マスターデータの種族値から計算する。相手ポケモンの素早さ6パターンも同じ式で算出する。詳細は機能設計書の「実数値計算式」セクションを参照。

**英語表記**: Actual Stat

**実装**: `src/logic/calc-actual-stats.js`

**関連用語**: [種族値](#種族値)、[能力ポイント](#能力ポイント)

---

### 種族値

**定義**: ポケモンの種族ごとに固定された能力の基礎値。同じ種族なら個体差によらず一定。

**説明**: 相手ポケモンの情報表示に使用する。Pokémon Showdown データから取得。

**英語表記**: Base Stat (BST: Base Stat Total は合計値)

**関連用語**: [実数値](#実数値)、[素早さ6パターン](#素早さ6パターン)

---

### 素早さ6パターン

**定義**: 相手ポケモンが取りうる素早さ実数値の代表的な6通り。先手・後手の判断基準として使用する。

**各パターン（Pokémon Champions 計算式）**:

| パターン名 | 能力ポイント | 性格補正 | 持ち物補正 | 説明 |
|-----------|------------|---------|-----------|------|
| 最速スカーフ | 32 | 1.1 | 1.5 | こだわりスカーフ＋最速 |
| 準速スカーフ | 32 | 1.0 | 1.5 | こだわりスカーフ＋準速 |
| 最速 | 32 | 1.1 | — | 素早さに最大投資＋プラス性格 |
| 準速 | 32 | 1.0 | — | 素早さに最大投資・性格補正なし |
| 無振り | 0 | 1.0 | — | 素早さ無投資・性格補正なし |
| 最遅 | 0 | 0.9 | — | 素早さ最小（トリックルーム用想定） |

**計算式**:
- スカーフなし: `floor((種族値 + 能力ポイント + 20) × 性格補正)`
- スカーフあり: `floor(スカーフなし実数値 × 1.5)`

**関連用語**: [種族値](#種族値)、[能力ポイント](#能力ポイント)

**実装**: `src/logic/speed-calc.js`

---

### タイプ一致補正（STAB）

**定義**: 技のタイプとポケモンのタイプが一致する場合に火力指数に乗算される係数（1.5）。一致しない場合は1.0。

**英語表記**: STAB（Same Type Attack Bonus）

**関連用語**: [火力指数](#火力指数)

---

### 特性補正

**定義**: ポケモンの特性によって火力指数に乗算される係数。例: ちからもち（atk×2.0）、てつのこぶし（物理技 atk×1.2）。

**説明**: 未定義の特性（pokelens に未登録）は1.0として計算する。HP依存の条件付き補正（もうか等）は P0 スコープ外。補正値はマスターデータ（`data/abilities.json`）から参照する。

**condition 種類**（条件付き補正。詳細仕様は[機能設計書](./functional-design.md)のエンティティ定義を参照）:

| condition | 判定内容 | 対象例 |
|-----------|---------|--------|
| `isType` | 技タイプが指定タイプと一致 | シルクのスカーフ・もくたん |
| `isPunch` | パンチ技（`move.tags` に `"isPunch"`） | てつのこぶし・パンチグローブ |
| `isPulse` | 波動技（`move.tags` に `"isPulse"`） | メガランチャー |
| `isBite` | かみつき技（`move.tags` に `"isBite"`） | つよいあご |
| `isRecoil` | 反動技（`move.tags` に `"isRecoil"`） | すてみ |
| `isSlice` | 切断技（`move.tags` に `"isSlice"`） | きれあじ |
| `powerMax60` | 技の威力が60以下 | テクニシャン |
| `isStab` | タイプ一致技（ポケモンのタイプと比較） | てきおうりょく（STAB倍率を2.0に上書き） |

> `isContact`（接触技）は `move.tags` に格納される。P0スコープの補正計算（火力指数）では直接参照しないが、Showdownの全フラグを格納する方針のため含まれる。

**関連用語**: [火力指数](#火力指数)

---

### 持ち物補正

**定義**: ポケモンが持つ道具によって火力指数に乗算される係数。例: いのちのたま1.3。

**説明**: 未定義の持ち物は1.0として計算する。補正値はマスターデータ（`data/items.json`）から参照する。持ち物で使用される `condition` は `isType`（タイプ一致）と `isPunch`（パンチ技）のみ。特性で使われる `isPulse` / `isBite` / `isRecoil` / `isSlice` / `powerMax60` / `isStab` は持ち物には存在しない。詳細仕様は[機能設計書](./functional-design.md)のエンティティ定義を参照。

**関連用語**: [火力指数](#火力指数)

---

### 能力ポイント

**定義**: Pokémon Champions における、ポケモンの各能力値に割り振れる成長ボーナス。0〜32の範囲で指定する。

**説明**: メインシリーズには存在しない独自仕様。素早さ計算式 `floor((種族値 + 能力ポイント + 20) × 性格補正)` で使用する。最大値は32（最速・準速パターンに相当）。`party.json` の `abilityPoints` フィールドに各ステータス分を入力する。

**関連用語**: [素早さ6パターン](#素早さ6パターン)、[実数値](#実数値)

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

**説明**: `cache/checksums.json` に `showdown-*.json` / `pokeapi-translations.json` / `champions-patch.json` / `moves-power-patch.json` / `items-modifiers.json` / `abilities-modifiers.json` のハッシュ値を保存し、次回起動時と比較する。変化したファイルの種類に応じて実行を開始するステップが決まり、変化なしの場合は全ステップをスキップする。スキップ条件の詳細は[機能設計書](./functional-design.md)の「増分実行の仕組み（ハッシュ比較）」および[アーキテクチャ設計書](./architecture.md)を参照。

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### champions-patch.json

**定義**: Pokémon Champions 独自のポケモンデータ（名称変更・新規追加など）を Showdown データに差分適用するための手書き管理 JSON ファイル。

**配置**: `tools/PokelensTools/champions-patch.json`

**説明**: C# データ準備ツール実行時にマージされ、最終的な `data/pokedex.json` / `data/moves.json` に反映される。変更時は[開発ガイドライン](./development-guidelines.md)の C# コーディング規約内の管理ルールに従うこと。

**関連ドキュメント**: [アーキテクチャ設計書](./architecture.md)

---

### moves-power-patch.json

**定義**: Pokémon Showdown で `power: null`（威力不定）となる技に最大威力を定義するための手書き管理 JSON ファイル。

**配置**: `tools/PokelensTools/moves-power-patch.json`

**説明**: `champions-patch.json`（Showdown 語彙・Step3 適用）とは異なり、最終出力語彙（`moves.json` の `power` フィールド）でStep4 に適用される。複数回攻撃技は C# ツールが `basePower × multihit[1]` で自動計算するため本ファイルの対象外。パッチ未定義の威力不定技は UI 上「−」表示となる。

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### items-modifiers.json / abilities-modifiers.json

**定義**: 持ち物・特性ごとの補正値（倍率・適用条件）を定義する手書き管理 JSON ファイル。C# データ準備ツールが `data/items.json` / `data/abilities.json` を生成する際の入力として使用する。

**配置**: `tools/PokelensTools/items-modifiers.json`、`tools/PokelensTools/abilities-modifiers.json`

**データ構造（例）**:
```json
{
  "lifeorb":    { "modifier": { "atk": 1.3, "spa": 1.3 } },
  "choiceband": { "modifier": { "atk": 1.5 } },
  "adaptability": { "modifier": { "condition": "isStab", "stab": 2.0 } }
}
```

**説明**: キーは Showdown 英語キー。`condition` は省略可能（省略時は全技に無条件適用）。`"isStab"` / `"isPunch"` 等の場合は技の条件を満たす場合のみ適用。UI 上での補正計算（`calcPowerIndex`）では `DataLoader.getItemModifier()` / `DataLoader.getAbilityModifier()` 経由で参照される。

**関連ドキュメント**: [機能設計書](./functional-design.md)

---

### ポケモンWiki

**定義**: ポケモンに関する情報を集積した日本語 Wiki サイト（https://wiki.pokemonwiki.com/wiki/Pok%C3%A9mon_Champions）。

**本プロジェクトでの用途**: Pokémon Champions における種族値変更・技威力変更などの差分を手動調査するために参照する。調査結果は `tools/PokelensTools/champions-patch.json` に静的パッチとして記録する。ランタイムへの影響はない（オフライン参照のみ）。

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

**本プロジェクトでの用途**: `src/logic/` 配下の純粋関数のユニットテストに使用。

**バージョン**: ^2.0.0

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

**実装**: `src/logic/`（`power-index-calc.js`, `speed-calc.js`, `name-search.js`, `calc-actual-stats.js`）

**依存関係**: `src/ui/` からのみ呼び出し可能。`src/data/` を直接 import しない。計算に必要なデータは UIレイヤーが DataLoader から取得して引数として渡す設計。

---

### UIレイヤー

**定義**: DOM操作・イベント処理・画面表示を担うモジュール群。

**実装**: `src/ui/`（`own-party-panel.js`, `own-pokemon-detail.js`, `opponent-party-panel.js`, `opponent-pokemon-detail.js`, `search-input.js`）

**依存関係**: `src/logic/` と `src/data/` を呼び出す。

---

### データアクセスレイヤー

**定義**: JSON ファイルの読み込みと解析を担うモジュール。UIレイヤーにデータを提供する。

**実装**: `src/data/`（`loader.js`）— `DataLoader` クラスとして実装される。

**DataLoader の主なメソッド**: `load()`, `getPokemonByName()`, `searchByName()`, `getItemModifier()`, `getAbilityModifier()`, `getTypeName()`, `getMoveCategory()`, `getNatureModifiers()`。詳細は[機能設計書](./functional-design.md)の DataLoader セクションを参照。

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

**能力値フィールド名**（`party.json` の `abilityPoints` キーおよび「実数値（H-A-B-C-D-S）」表記との対応）:

| フィールド名 | 英語 | 日本語 |
|------------|------|-------|
| `hp` | Hit Points | HP |
| `atk` | Attack | 攻撃 |
| `def` | Defense | 防御 |
| `spa` | Special Attack | 特攻 |
| `spd` | Special Defense | 特防 |
| `spe` | Speed | 素早さ |

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
- [素早さ6パターン](#素早さ6パターン)
- [ステアリングファイル](#ステアリングファイル)
- [増分実行](#増分実行)

### た行
- [タイプ一致補正（STAB）](#タイプ一致補正stab)
- [データアクセスレイヤー](#データアクセスレイヤー)
- [特性補正](#特性補正)

### な行
- [名前検索](#名前検索)
- [能力ポイント](#能力ポイント)

### は行
- [ポケモンWiki](#ポケモンwiki)

### ま行
- [マスターデータ](#マスターデータ)
- [持ち物補正](#持ち物補正)

### ら行
- [ロジックレイヤー](#ロジックレイヤー)

### A-Z
- [C# データ準備ツール](#c-データ準備ツール)
- [champions-patch.json](#champions-patchjson)
- [moves-power-patch.json](#moves-power-patchjson)
- [items-modifiers.json / abilities-modifiers.json](#items-modifiersjson--abilities-modifiersjson)
- [PokéAPI](#pokéapi)
- [Pokémon Showdown データ](#pokémon-showdown-データ)
- [UIレイヤー](#uiレイヤー)
- [Vite](#vite)
- [Vitest](#vitest)
