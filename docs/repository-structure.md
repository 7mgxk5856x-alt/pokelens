# リポジトリ構造定義書 (Repository Structure Document)

## プロジェクト構造

```
pokelens/
├── README.md                       # プロジェクト概要・セットアップ手順
├── src/                            # JavaScript フロントエンド ソースコード
│   ├── main.js                     # エントリーポイント（UIコンポーネント初期化・DataLoader起動）
│   ├── styles.css                  # 全 UI のスタイルシート（main.js が import、Vite がバンドル）
│   ├── data/                       # データ読み込みレイヤー
│   ├── logic/                      # ロジックレイヤー（純粋関数）
│   └── ui/                         # UIレイヤー（DOM操作）
├── tests/                          # テストコード（JS）
│   ├── unit/                       # ユニットテスト（Vitest）
│   └── e2e/                        # E2E テスト（Playwright）
├── tools/                          # C# データ準備ツール
│   ├── PokelensTools/
│   │   ├── PokelensTools.csproj
│   │   ├── Program.cs              # エントリーポイント
│   │   ├── Fetchers/               # HTTP取得（Showdown / PokéAPI）
│   │   ├── Pipeline/               # 増分判定・パッチ適用・マージ変換
│   │   ├── Models/                 # データ型（ChecksumSet 等）
│   │   ├── Common/                 # 共通ヘルパー
│   │   └── Patches/                # 手動管理データ（JSON）
│   └── PokelensTools.Tests/        # xUnit テストプロジェクト
│       └── PokelensTools.Tests.csproj
├── cache/                          # C# ツールの中間データ（gitignore対象）
├── data/                           # データファイル（JSONのみ）
├── docs/                           # プロジェクトドキュメント
│   ├── product-requirements.md     # PRD
│   ├── functional-design.md        # 機能設計
│   ├── architecture.md             # アーキテクチャ設計
│   ├── repository-structure.md     # 本書
│   ├── development-guidelines.md   # 開発ガイドライン
│   ├── glossary.md                 # 用語集
│   ├── ideas/                      # 初期アイデア・要件メモ（参照専用）
│   └── testing/                    # テスト派生ドキュメント（テストレベル別）
│       ├── unit/automated-test-cases.md           # 単体テストのケース一覧（自動）
│       ├── integration/automated-test-cases.md    # 統合テストのケース一覧（自動・C#パイプライン）
│       └── e2e/
│           ├── automated-test-cases.md            # E2E 自動テストのケース一覧（Playwright）
│           └── manual-test-cases.md               # E2E 手動テストの仕様書（環境セットアップ系のみ）
├── .claude/                        # Claude Code 設定
├── .devcontainer/                  # 開発コンテナ設定
├── .steering/                      # 作業タスク管理（gitignore済み）
├── index.html                      # アプリエントリーポイント（DOM 構造のみ。CSS は src/styles.css）
├── vite.config.js                  # Vite 設定
├── vitest.config.js                # Vitest 設定
├── playwright.config.js            # Playwright（E2E）設定
├── eslint.config.js                # ESLint 設定
└── package.json                    # Prettier 設定はデフォルトを使用（個別の .prettierrc 等は持たない）
```

---

## ディレクトリ詳細

### src/main.js (エントリーポイント)

**役割**: アプリ起動時に DataLoader を初期化し、各 UI コンポーネントをマウントする

**依存関係**:
- 依存可能: `src/ui/`、`src/data/`、`src/styles.css`（スタイルシートを `import './styles.css'` で取り込み、Vite に CSS バンドル処理を委ねる）
- 直接 import 禁止: `src/logic/`（`src/logic/` の関数は `src/ui/` が import して使用する。`main.js` が直接 import しないことで、ロジック呼び出しの起点を UI レイヤーに統一する）

---

### src/styles.css (スタイルシート)

**役割**: 全 UI コンポーネントの CSS を一元管理する。`main.js` が `import './styles.css'` で取り込み、Vite がバンドル時に処理する（dev サーバー時は HMR が効く）

**配置方針**:
- 単一ファイルで運用（〜500 行規模）。コンポーネント別の CSS Modules / 別ファイル分割は採用しない
- セレクタは BEM 等の規約には従わず、コンポーネントクラス名（`.detail-stats-grid` / `.speed-patterns` 等）と ID（`#own-detail` / `#opponent-detail` 等）を直接使う
- 色・余白・フォントの値はファイル内で直書き（CSS 変数は現状未導入。トークン化は規模が増えた時点で検討）

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
├── loader.js
└── mega-evolutions.json   # メガシンカマッピング（親 → stones / megaForms。手動管理）
```

---

### src/logic/ (ロジックレイヤー)

**役割**: 計算・検索・データ変換の純粋関数を実装する。DOM・副作用を持たない

**配置ファイル**:
- `power-index-calc.js`: 火力指数計算
- `speed-calc.js`: 素早さ4パターン計算
- `endurance-index-calc.js`: 耐久指数計算（物理 = HP × 防御、特殊 = HP × 特防）
- `name-search.js`: ひらがな/カタカナ正規化・前方一致検索
- `calc-actual-stats.js`: 実数値計算
- `resolve-modifier.js`: 特性・持ち物の補正条件解決
- `constants.js`: ドメイン区分値の定数集約（`MODIFIER_KIND` 等の純粋エクスポートのみ）

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
├── resolve-modifier.js
└── constants.js
```

---

### src/ui/ (UIレイヤー)

**役割**: DOM 操作・イベント処理・画面表示を担う。ロジックレイヤーを呼び出す

**配置ファイル**:
- `own-party-panel.js` (`OwnPartyPanel`): 自分パーティ6匹のカード一覧表示
- `own-pokemon-detail.js` (`OwnPokemonDetail`): 自分ポケモン詳細パネル（実数値・技・火力指数・耐久指数）
- `opponent-party-panel.js` (`OpponentPartyPanel`): 相手パーティ6スロットの管理
- `opponent-pokemon-detail.js` (`OpponentPokemonDetail`): 相手ポケモン詳細パネル（種族値・素早さ4パターン・耐久指数4パターン）
- `search-input.js` (`SearchInput`): ポケモン名サジェスト入力
- `dom-utils.js`: 共通 DOM 操作ヘルパー（`el()` 等）
- `stat-labels.js`: 種族値・実数値の表示ラベル定義と整形ヘルパー（`STAT_LABELS` / `formatBaseStats`）

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
├── search-input.js
├── dom-utils.js
└── stat-labels.js
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

#### tests/e2e/

**役割**: Playwright による自動 E2E テスト。Vite dev サーバー上で実ブラウザ（Chromium）を起動し、`page.route()` で `data/party.json` のみを mock 注入して UI 結合・画面表示を検証する。マスターデータ（pokedex/moves/items/abilities/types/move-categories/natures）は実ファイルを使用する

**構造**:
```
tests/e2e/
├── helpers/
│   ├── mock-party.js              # page.route で party.json を mock するヘルパー
│   ├── party-fixtures.js          # 各テスト用の party 入力データ
│   └── selectors.js               # UI セレクタ定数（DOM 構造変更耐性）
├── own-party-display.spec.js      # 自分パーティの表示・選択・状態遷移
├── own-pokemon-detail.spec.js     # 自分ポケモン詳細・火力指数・性格・技一覧
├── opponent-suggest.spec.js       # 相手パーティ入力・サジェスト検索・XSS 耐性
├── opponent-pokemon-detail.spec.js # 相手ポケモン詳細・素早さ 4 パターン
└── error-handling.spec.js         # party.json 構文不正・必須フィールド欠落
```

**命名規則**: `[機能グループ].spec.js`

**初回セットアップ**: `npx playwright install chromium`（〜100MB のブラウザバイナリを取得）

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
│   │   ├── ShowdownKey.cs          # Showdown データ JSON のキー定数
│   │   ├── PokeAPIFetcher.cs       # PokéAPI HTTP取得
│   │   ├── PokeApiKey.cs           # PokéAPI レスポンス JSON のキー定数
│   │   ├── PokeApiSlug.cs          # Showdown 名 → PokéAPI slug 変換
│   │   ├── PokeApiName.cs          # PokéAPI レスポンスからの和名・フォルム抽出
│   │   └── TranslationKey.cs       # 翻訳辞書 JSON の構造キー定数
│   ├── Pipeline/
│   │   ├── IncrementalRunner.cs    # 増分実行判定（チェックサム比較）
│   │   ├── PatchApplicator.cs      # champions-patch 適用
│   │   ├── PatchKey.cs             # 手動パッチ JSON の構造キー定数
│   │   ├── MergeConverter.cs       # JSON変換・マージ・正規化
│   │   ├── MasterKey.cs            # マスタ出力 JSON の構造キー定数
│   │   └── MasterTag.cs            # マスタ出力 JSON のタグ値定数
│   ├── Models/
│   │   └── ChecksumSet.cs          # チェックサムの型
│   ├── Common/
│   │   ├── JsonHelpers.cs          # JSON出力ヘルパー
│   │   ├── DataPaths.cs            # ファイル・ディレクトリのパス（cache/data/patch 別）
│   │   └── Endpoints.cs            # 外部HTTPエンドポイント（Showdown / PokéAPI）
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
- `party.json`: ユーザーが手編集する自分パーティ（**git 管理対象外**。開発者ごとに異なるサンプルのため `.gitignore` で除外。各環境でローカル編集）

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
    - `automated-test-cases.md`: Playwright による自動 E2E テストのケース一覧（`tests/e2e/*.spec.js` に対応）。テストコードから手動で抽出したスナップショット
    - `manual-test-cases.md`: 手動 E2E テストのケース一覧（環境セットアップ・C# データ生成・dev サーバー起動の 3 件のみ。それ以外の機能挙動は自動 E2E が担保）

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
| ユニットテスト（JS） | `tests/unit/` | `[対象].test.js` | `power-index-calc.test.js` |
| E2E テスト（JS） | `tests/e2e/` | `[機能グループ].spec.js` | `own-party-display.spec.js` |
| ユニットテスト（C#） | `tools/PokelensTools.Tests/` | `[対象]Tests.cs` | `MergeConverterTests.cs` |
| 統合テスト（C#） | `tools/PokelensTools.Tests/` | `[対象]IntegrationTests.cs` | `PipelineIntegrationTests.cs` |

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
.claude/tmp/                # slash command の一時出力
cache/                      # C# ツールの中間データ（再生成可能）
data/pokedex.json           # C# ツールで再生成可能
data/moves.json             # C# ツールで再生成可能
data/items.json             # C# ツールで再生成可能
data/abilities.json         # C# ツールで再生成可能
data/party.json             # 開発者ごとのローカル専用サンプルパーティ
coverage/                   # Vitest カバレッジレポート
playwright-report/          # Playwright テストレポート
test-results/               # Playwright テスト実行結果
playwright/.cache/          # Playwright ブラウザキャッシュ
tools/**/bin/               # .NET ビルド出力
tools/**/obj/
```

`data/types.json` / `data/move-categories.json` / `data/natures.json` は git 管理対象とする（手書き管理データ）。`data/party.json` は開発者ごとに異なるサンプルパーティのため git 管理対象外（上記の通り `.gitignore` 済み）。

---

## スケーリング戦略

### P1 機能追加時（対面評価）

P1の「対面選択・素早さ判定・ダメージ計算」は既存構造に収まる:
- `src/logic/damage-calc.js` を追加
- `src/ui/matchup-panel.js` を追加

### ファイルサイズ管理

- 1ファイル300行以下を推奨
- 超過時は責務単位で分割（例: `name-search.js` が肥大化した場合は `name-normalize.js` を分離）
