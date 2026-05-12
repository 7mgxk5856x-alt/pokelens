# リポジトリ構造定義書 (Repository Structure Document)

## プロジェクト構造

```
pokelens/
├── src/                        # JavaScript フロントエンド ソースコード
│   ├── data/                   # データ読み込みレイヤー
│   ├── logic/                  # ロジックレイヤー（純粋関数）
│   └── ui/                     # UIレイヤー（DOM操作）
├── tests/                      # テストコード
│   ├── unit/                   # ユニットテスト
│   └── integration/            # 統合テスト
├── tools/                      # C# データ準備ツール
│   └── ShowdownFetcher/
├── data/                       # データファイル（JSONのみ）
├── docs/                       # プロジェクトドキュメント
├── .claude/                    # Claude Code 設定
├── .steering/                  # 作業タスク管理（gitignore済み）
├── index.html                  # アプリエントリーポイント
├── vite.config.js              # Vite 設定
├── vitest.config.js            # Vitest 設定
├── eslint.config.js            # ESLint 設定
├── .prettierrc                 # Prettier 設定
└── package.json
```

---

## ディレクトリ詳細

### src/data/ (データ読み込みレイヤー)

**役割**: JSON ファイルの fetch・キャッシュ・アクセス手段を提供する

**配置ファイル**:
- `loader.js`: `pokemon-data.json` と `party.json` を fetch し、メモリにキャッシュする

**命名規則**: camelCase（関数ファイル）

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
- `power-index.js`: 火力指数計算
- `speed-calc.js`: 素早さ4パターン計算
- `name-search.js`: ひらがな/カタカナ正規化・前方一致検索

**命名規則**: kebab-case（複数単語はハイフン区切り）

**依存関係**:
- 依存可能: なし（外部依存を持たない純粋関数）
- 依存禁止: `src/ui/`、`src/data/`

**例**:
```
src/logic/
├── power-index.js
├── speed-calc.js
└── name-search.js
```

---

### src/ui/ (UIレイヤー)

**役割**: DOM 操作・イベント処理・画面表示を担う。ロジックレイヤーを呼び出す

**配置ファイル**:
- `party-panel.js`: 自分パーティ6匹のカード一覧表示
- `own-detail.js`: 自分ポケモン詳細パネル（実数値・技・火力指数）
- `opponent-panel.js`: 相手パーティ6スロットの管理
- `opponent-detail.js`: 相手ポケモン詳細パネル（種族値・素早さ4パターン）
- `search-input.js`: ポケモン名サジェスト入力

**命名規則**: kebab-case（コンポーネント名はUIの役割を表す）

**依存関係**:
- 依存可能: `src/logic/`、`src/data/`
- 依存禁止: 他の `src/ui/` ファイルへの直接依存（イベント経由で連携）

**例**:
```
src/ui/
├── party-panel.js
├── own-detail.js
├── opponent-panel.js
├── opponent-detail.js
└── search-input.js
```

---

### tests/ (テストディレクトリ)

#### tests/unit/

**役割**: `src/logic/` の純粋関数をユニットテストする

**構造**:
```
tests/unit/
├── power-index.test.js
├── speed-calc.test.js
└── name-search.test.js
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

### tools/ShowdownFetcher/ (C# データ準備ツール)

**役割**: Pokémon Showdown からマスターデータを取得し、`data/pokemon-data.json` に変換・出力する

**構造**:
```
tools/
└── ShowdownFetcher/
    ├── ShowdownFetcher.csproj
    ├── Program.cs              # エントリーポイント
    ├── Fetcher.cs              # HTTP取得
    └── Converter.cs            # JSON変換・正規化
```

**命名規則**: PascalCase（C# 標準）

**依存関係**: `data/` ディレクトリへの書き込みのみ

---

### data/ (データファイル)

**役割**: アプリが参照する JSON ファイルを格納する

**配置ファイル**:
- `pokemon-data.json`: C# ツールが生成するマスターデータ（git 管理対象外も可）
- `party.json`: ユーザーが手編集する自分パーティ（git 管理対象）

```
data/
├── pokemon-data.json
└── party.json
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

---

## ファイル配置規則

### ソースファイル

| ファイル種別 | 配置先 | 命名規則 | 例 |
|------------|--------|---------|-----|
| データ読み込み | `src/data/` | camelCase | `loader.js` |
| 計算・検索関数 | `src/logic/` | kebab-case | `power-index.js` |
| UIコンポーネント | `src/ui/` | kebab-case | `own-detail.js` |
| C# クラス | `tools/ShowdownFetcher/` | PascalCase | `Converter.cs` |
| データファイル | `data/` | kebab-case | `pokemon-data.json` |

### テストファイル

| テスト種別 | 配置先 | 命名規則 | 例 |
|-----------|--------|---------|-----|
| ユニットテスト | `tests/unit/` | `[対象].test.js` | `power-index.test.js` |
| 統合テスト | `tests/integration/` | `[フロー].test.js` | `data-flow.test.js` |

---

## 命名規則

### JavaScript ファイル
- **複数単語**: `kebab-case`（例: `power-index.js`, `name-search.js`）
- **テストファイル**: `[対象].test.js`

### C# ファイル
- **クラスファイル**: `PascalCase`（例: `Fetcher.cs`, `Converter.cs`）

### JSON ファイル
- `kebab-case`（例: `pokemon-data.json`, `party.json`）

### ディレクトリ
- `kebab-case`（例: `src/`, `tools/`, `ShowdownFetcher/` は C# プロジェクト名として例外）

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
- `src/logic/` → `src/ui/`（ロジックがDOMに依存してはならない）
- `src/logic/` → `src/data/`（ロジックはデータ読み込みに依存しない）
- `src/data/` → `src/logic/` または `src/ui/`

---

## 除外設定

### .gitignore に追加すべきもの

```
node_modules/
dist/
.steering/*
!.steering/.gitkeep
.claude/settings.local.json
data/pokemon-data.json      # C# ツールで再生成可能なため
coverage/
```

`data/party.json` は git 管理対象とする（パーティ定義はユーザー資産）。

---

## スケーリング戦略

### P1 機能追加時（対面評価）

P1の「対面選択・素早さ判定・ダメージ計算」は既存構造に収まる:
- `src/logic/damage-calc.js` を追加
- `src/ui/matchup-panel.js` を追加

### ファイルサイズ管理

- 1ファイル300行以下を推奨
- 超過時は責務単位で分割（例: `name-search.js` が肥大化した場合は `name-normalize.js` を分離）
