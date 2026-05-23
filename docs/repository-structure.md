# リポジトリ構造定義書 (Repository Structure Document)

## プロジェクト構造

> このツリーは目標構造を示す。`†` のついたファイル・ディレクトリは未作成（セットアップ時に追加）。

```
pokelens/
├── README.md                       # プロジェクト概要・セットアップ手順
├── src/ †                          # JavaScript フロントエンド ソースコード
│   ├── main.js †                   # エントリーポイント（UIコンポーネント初期化・DataLoader起動）
│   ├── data/ †                     # データ読み込みレイヤー
│   ├── logic/ †                    # ロジックレイヤー（純粋関数）
│   └── ui/ †                       # UIレイヤー（DOM操作）
├── tests/ †                        # テストコード
│   ├── unit/ †                     # ユニットテスト
│   └── integration/ †              # 統合テスト
├── tools/ †                        # C# データ準備ツール
│   ├── PokelensTools/ †
│   │   ├── PokelensTools.csproj †
│   │   ├── Program.cs †            # エントリーポイント
│   │   ├── Fetchers/ †             # HTTP取得（Showdown / PokéAPI）
│   │   ├── Pipeline/ †             # 増分判定・パッチ適用・マージ変換
│   │   ├── Models/ †               # データ型（ChecksumSet 等）
│   │   ├── Common/ †               # 共通ヘルパー
│   │   └── Patches/ †              # 手動管理データ（JSON）
│   └── PokelensTools.Tests/ †      # xUnit テストプロジェクト
│       └── PokelensTools.Tests.csproj †
├── cache/ †                        # C# ツールの中間データ（gitignore対象）
├── data/ †                         # データファイル（JSONのみ）
├── docs/                           # プロジェクトドキュメント
│   ├── ideas/                      # 初期アイデア・要件メモ（参照専用）
│   └── testing/                    # テスト派生ドキュメント（テストレベル別）
│       ├── unit/                   # 単体テストのケース一覧（自動）
│       ├── integration/            # 統合テストのケース一覧（自動）
│       └── e2e/                    # E2E テストの仕様書（手動）
├── .claude/                        # Claude Code 設定
├── .devcontainer/                  # 開発コンテナ設定
├── .husky/                         # コミット前フック（husky）
├── .steering/                      # 作業タスク管理（gitignore済み）
├── index.html †                    # アプリエントリーポイント
├── vite.config.js †                # Vite 設定
├── vitest.config.js †              # Vitest 設定
├── eslint.config.js †              # ESLint 設定
├── .prettierrc †                   # Prettier 設定
└── package.json
```

---

## ディレクトリ詳細

### src/main.js (エントリーポイント)

**役割**: アプリ起動時に DataLoader を初期化し、各 UI コンポーネントをマウントする

**依存関係**:
- 依存可能: `src/ui/`、`src/data/`
- 直接 import 禁止: `src/logic/`（`src/logic/` の関数は `src/ui/` が import して使用する。`main.js` が直接 import しないことで、ロジック呼び出しの起点を UI レイヤーに統一する）

---

### src/data/ (データ読み込みレイヤー)

**役割**: JSON ファイルの fetch・キャッシュ・アクセス手段を提供する

**配置ファイル**:
- `loader.js`: `pokedex.json` / `moves.json` / `items.json` / `abilities.json` / `types.json` / `move-categories.json` / `natures.json` / `party.json` を fetch し、メモリにキャッシュする

**命名規則**: kebab-case（他レイヤーと統一）

**依存関係**:
- 依存可能: なし（外部ファイルのみ参照）
- 依存禁止: `src/ui/`、`src/logic/`

**例**:
```
src/data/
└── loader.js
```

---

### src/logic/ (ロジックレイヤー)

**役割**: 計算・検索・データ変換の純粋関数を実装する。DOM・副作用を持たない

**配置ファイル**:
- `power-index-calc.js`: 火力指数計算
- `speed-calc.js`: 素早さ6パターン計算
- `name-search.js`: ひらがな/カタカナ正規化・前方一致検索
- `calc-actual-stats.js`: 実数値計算
- `resolve-modifier.js`: 特性・持ち物の補正条件解決

**命名規則**: kebab-case（複数単語はハイフン区切り）

**依存関係**:
- 依存可能: `src/logic/` 内の他の純粋関数（依存サイクルを作らないこと。例: `speed-calc.js` が `calc-actual-stats.js` の `calcStat` を import）
- 依存禁止: `src/ui/`、`src/data/`
- ※ データは UI レイヤーが `DataLoader` から取得し、引数として渡す設計。`src/logic/` は `src/data/` を直接 import しない

**例**:
```
src/logic/
├── power-index-calc.js
├── speed-calc.js
├── name-search.js
├── calc-actual-stats.js
└── resolve-modifier.js
```

---

### src/ui/ (UIレイヤー)

**役割**: DOM 操作・イベント処理・画面表示を担う。ロジックレイヤーを呼び出す

**配置ファイル**:
- `own-party-panel.js` (`OwnPartyPanel`): 自分パーティ6匹のカード一覧表示
- `own-pokemon-detail.js` (`OwnPokemonDetail`): 自分ポケモン詳細パネル（実数値・技・火力指数）
- `opponent-party-panel.js` (`OpponentPartyPanel`): 相手パーティ6スロットの管理
- `opponent-pokemon-detail.js` (`OpponentPokemonDetail`): 相手ポケモン詳細パネル（種族値・素早さ6パターン）
- `search-input.js` (`SearchInput`): ポケモン名サジェスト入力

**命名規則**: kebab-case（コンポーネント名はUIの役割を表す）

**依存関係**:
- 依存可能: `src/logic/`、`src/data/`
- 直接 import 禁止: 他の `src/ui/` ファイル（コンポーネント間の連携はコンストラクタへのコールバック注入で行う。`main.js` がコールバックを生成して各コンポーネントに渡す）

**例**:
```
src/ui/
├── own-party-panel.js
├── own-pokemon-detail.js
├── opponent-party-panel.js
├── opponent-pokemon-detail.js
└── search-input.js
```

---

### tests/ (テストディレクトリ)

#### tests/unit/

**役割**: `src/logic/` の純粋関数および `src/data/loader.js` をユニットテストする

**構造**:
```
tests/unit/
├── power-index-calc.test.js
├── speed-calc.test.js
├── name-search.test.js
├── calc-actual-stats.test.js
├── resolve-modifier.test.js
└── loader.test.js          # DataLoader の正常系・ファイル不存在時のエラー
```

**命名規則**: `[対象ファイル名].test.js`

#### tests/integration/

**役割**: DataLoader → UIコンポーネントへのデータ供給フロー全体をテストする

**構造**:
```
tests/integration/
└── data-flow.test.js
```

---

### vitest.config.js（ルートファイル）

vitest のデフォルト検索パターン（`src/**/*.test.js`）を上書きし、`tests/` ディレクトリを対象にする設定が必要:

```js
// vitest.config.js
export default {
  test: {
    include: ['tests/**/*.test.js'],
  },
};
```

---

### tools/PokelensTools/ (C# データ準備ツール)

**役割**: Pokémon Showdown / PokéAPI からマスターデータを取得し、`data/` 配下の JSON に変換・出力する

**構造**:
```
tools/
├── PokelensTools/
│   ├── PokelensTools.csproj
│   ├── Program.cs                  # エントリーポイント・増分実行制御
│   ├── AssemblyInfo.cs             # InternalsVisibleTo（テストへ internal 公開）
│   ├── Fetchers/
│   │   ├── ShowdownFetcher.cs      # Showdown HTTP取得
│   │   └── PokeAPIFetcher.cs       # PokéAPI HTTP取得
│   ├── Pipeline/
│   │   ├── IncrementalRunner.cs    # 増分実行判定（チェックサム比較）
│   │   ├── PatchApplicator.cs      # champions-patch 適用
│   │   └── MergeConverter.cs       # JSON変換・マージ・正規化
│   ├── Models/
│   │   └── ChecksumSet.cs          # チェックサムの型
│   ├── Common/
│   │   ├── JsonHelpers.cs          # JSON出力ヘルパー
│   │   ├── CacheFileName.cs        # cache/ 中間ファイル名の共有定数
│   │   ├── Endpoints.cs            # 外部HTTPエンドポイント（Showdown / PokéAPI）
│   │   ├── ShowdownKey.cs          # Showdown データ JSON のキー定数
│   │   ├── PokeApiKey.cs           # PokéAPI レスポンス JSON のキー定数
│   │   ├── PokeApiSlug.cs          # Showdown 名 → PokéAPI slug 変換
│   │   ├── PokeApiName.cs          # PokéAPI レスポンスからの和名・フォルム抽出
│   │   ├── PatchKey.cs             # 手動パッチ JSON の構造キー定数
│   │   └── TranslationKey.cs       # 翻訳辞書 JSON の構造キー定数
│   └── Patches/                    # 手動管理データ（git管理対象）
│       ├── champions-patch.json    # Champions差分パッチ
│       ├── moves-power-patch.json  # 威力不定技の最大威力定義
│       ├── items-modifiers.json    # 持ち物補正値定義
│       ├── abilities-modifiers.json # 特性補正値定義
│       ├── pokemon-name-patch.json # ポケモン日本語名の上書き（フォルム一意化用）
│       └── item-name-patch.json    # 持ち物日本語名の上書き（PokéAPI欠落補完用）
└── PokelensTools.Tests/            # xUnit テストプロジェクト
    └── PokelensTools.Tests.csproj
```

**命名規則**: PascalCase（C# 標準）

**依存関係**: `data/` ディレクトリへの書き込みのみ

---

### cache/ (C# ツール中間データ・gitignore対象)

**役割**: C# ツールがデータ取得・変換の各ステップで生成する中間ファイルを格納する。git 管理対象外のため `cache/` ディレクトリごと `.gitignore` に追加する。

**配置ファイル**:
- `showdown-pokedex.json`: Showdown から取得した英語ポケモンデータ（変換なし）
- `showdown-moves.json`: Showdown から取得した英語技データ（変換なし）
- `showdown-items.json`: Showdown から取得した英語持ち物データ（変換なし）
- `showdown-abilities.json`: Showdown から取得した英語特性データ（変換なし）
- `pokeapi-translations.json`: PokéAPI から取得した日本語翻訳データ
- `checksums.json`: 増分実行用ハッシュ値（`showdown-*.json` / `pokeapi-translations.json` / `champions-patch.json` / `moves-power-patch.json` / `items-modifiers.json` / `abilities-modifiers.json` / `pokemon-name-patch.json` / `item-name-patch.json` の前回実行時ハッシュを保持）

```
cache/
├── showdown-pokedex.json
├── showdown-moves.json
├── showdown-items.json
├── showdown-abilities.json
├── pokeapi-translations.json
└── checksums.json
```

---

### data/ (データファイル)

**役割**: アプリが参照する JSON ファイルを格納する

**配置ファイル**:
- `pokedex.json`: C# ツールが生成（git 管理対象外）
- `moves.json`: C# ツールが生成（git 管理対象外）
- `items.json`: C# ツールが生成（git 管理対象外）
- `abilities.json`: C# ツールが生成（git 管理対象外）
- `types.json`: 手書き管理・タイプ名日本語変換マップ（git 管理対象）
- `move-categories.json`: 手書き管理・技分類日本語変換マップ（git 管理対象）
- `natures.json`: 手書き管理・性格補正倍率マップ（git 管理対象）
- `party.json`: ユーザーが手編集する自分パーティ（git 管理対象）

```
data/
├── pokedex.json           # C# ツールが生成
├── moves.json             # C# ツールが生成
├── items.json             # C# ツールが生成
├── abilities.json         # C# ツールが生成
├── types.json             # 手書き管理
├── move-categories.json   # 手書き管理
├── natures.json           # 手書き管理
└── party.json             # ユーザーが手編集
```

---

### docs/ (ドキュメント)

**配置ドキュメント**:
- `product-requirements.md`: PRD
- `functional-design.md`: 機能設計書
- `architecture.md`: アーキテクチャ設計書
- `repository-structure.md`: 本ドキュメント
- `development-guidelines.md`: 開発ガイドライン
- `glossary.md`: 用語集

**サブディレクトリ**:
- `ideas/`: プロジェクト初期アイデア・要件メモ（参照専用）
  - `initial-requirements.md`: `/setup-project` コマンドの入力として使用した初期アイデア・要件メモ。作成後は読み取り専用で参照のみ（git 管理対象）
  - `initial-requirements-sample.md`: 記述フォーマットのサンプル（gitignore 対象）
- `testing/`: テストに関する派生・参照ドキュメント（永続ドキュメントとは別管理）。**テストレベル別**にディレクトリを分け、ファイル名で実行手段（`automated-` / `manual-`）を区別する
  - `unit/`: 単体テストのケース一覧
    - `automated-test-cases.md`: 単体テスト（フロントエンド Vitest ＋ C# xUnit）のケース一覧。テストコードから手動で抽出したスナップショット（テスト追加・変更時に更新）。単体テストの手動版は作成しない
  - `integration/`: 統合テストのケース一覧
    - `automated-test-cases.md`: 統合テスト（C# パイプライン）のケース一覧。テストコードから抽出したスナップショット
  - `e2e/`: E2E（エンドツーエンド）テストの仕様書
    - `manual-test-cases.md`: 手動・E2E テストのケース一覧（ブラウザ操作で P0 機能を検証）。PRD の受け入れ条件から起こす

---

## ファイル配置規則

### ソースファイル

| ファイル種別 | 配置先 | 命名規則 | 例 |
|------------|--------|---------|-----|
| エントリーポイント | `src/` | kebab-case | `main.js` |
| データ読み込み | `src/data/` | kebab-case | `loader.js` |
| 計算・検索関数 | `src/logic/` | kebab-case | `power-index-calc.js` |
| UIコンポーネント | `src/ui/` | kebab-case | `own-pokemon-detail.js` |
| C# クラス | `tools/PokelensTools/` | PascalCase | `MergeConverter.cs` |
| 手書き管理 JSON（C# ツール用） | `tools/PokelensTools/` | kebab-case | `champions-patch.json` |
| データファイル | `data/` | kebab-case | `pokedex.json` |

### テストファイル

| テスト種別 | 配置先 | 命名規則 | 例 |
|-----------|--------|---------|-----|
| ユニットテスト | `tests/unit/` | `[対象].test.js` | `power-index-calc.test.js` |
| 統合テスト | `tests/integration/` | `[フロー].test.js` | `data-flow.test.js` |

---

## 命名規則

### JavaScript ファイル
- **複数単語**: `kebab-case`（例: `power-index-calc.js`, `name-search.js`）
- **テストファイル**: `[対象].test.js`

### C# ファイル
- **クラスファイル**: `PascalCase`（例: `ShowdownFetcher.cs`, `MergeConverter.cs`）

### JSON ファイル
- `kebab-case`（例: `pokedex.json`, `move-categories.json`, `party.json`）

### ディレクトリ
- `kebab-case`（例: `src/`, `tools/`, `PokelensTools/` は C# プロジェクト名として例外）

---

## 依存関係ルール

```
UIレイヤー (src/ui/)
    ↓ (OK)
ロジックレイヤー (src/logic/)

UIレイヤー (src/ui/)
    ↓ (OK)
データレイヤー (src/data/)

ロジックレイヤー (src/logic/)
    ↓ 依存禁止
データレイヤー (src/data/)

C# ツール (tools/)
    ↓ (OK: 書き込みのみ)
データファイル (data/)
```

**禁止される依存**:
- `src/main.js` → `src/logic/`（ロジック呼び出しの起点は `src/ui/` に統一する。`main.js` からの直接 import は禁止）
- `src/logic/` → `src/ui/`（ロジックがDOMに依存してはならない）
- `src/logic/` → `src/data/`（ロジックはデータ読み込みに依存しない）
- `src/data/` → `src/logic/` または `src/ui/`

---

## 除外設定

### .gitignore 設定

```
docs/ideas/initial-requirements-sample.md  # サンプルファイル（再生成可能）
node_modules/*
dist/
.steering/*
!.steering/.gitkeep
.claude/settings.local.json
cache/                      # C# ツールの中間データ（再生成可能）
data/pokedex.json           # C# ツールで再生成可能
data/moves.json             # C# ツールで再生成可能
data/items.json             # C# ツールで再生成可能
data/abilities.json         # C# ツールで再生成可能
coverage/
```

`data/types.json` / `data/move-categories.json` / `data/natures.json` / `data/party.json` は git 管理対象とする（手書き管理またはユーザー資産）。

---

## スケーリング戦略

### P1 機能追加時（対面評価）

P1の「対面選択・素早さ判定・ダメージ計算」は既存構造に収まる:
- `src/logic/damage-calc.js` を追加
- `src/ui/matchup-panel.js` を追加

### ファイルサイズ管理

- 1ファイル300行以下を推奨
- 超過時は責務単位で分割（例: `name-search.js` が肥大化した場合は `name-normalize.js` を分離）
