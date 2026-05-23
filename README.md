# pokelens

ポケモン対戦支援ツール。選出画面・対戦中に、自分パーティの火力指数・相手ポケモンの種族値・素早さ6パターンなどを即参照できるローカルWebツール。

- **対象**: 個人利用、PCブラウザ（Chrome 最新版）、ローカル環境
- **データソース**: Pokémon Showdown + PokéAPI（日本語名）
- **対応バージョン**: Pokémon Champions

## 必要環境

| ツール | バージョン |
|---|---|
| Node.js | LTS |
| .NET SDK | 8.0 以上 |

## セットアップ

```bash
# 1. 依存関係のインストール
npm install

# 2. （E2E テストを実行する場合）Playwright Chromium バイナリ取得（〜100MB、初回のみ）
npx playwright install chromium

# 3. C# ツールでマスターデータを生成（pokedex / moves / items / abilities）
dotnet run --project tools/PokelensTools

# 4. data/party.json を自分のパーティで編集
#    （.gitignore 済み。開発者ごとにローカル編集する。スキーマは docs/product-requirements.md 参照）

# 5. 開発サーバー起動（必ず Vite 経由で開く。file:// 直接起動は非対応）
npm run dev
```

ブラウザで Vite が表示する URL（既定 http://127.0.0.1:5173）を開く。

## よく使うコマンド

```bash
# フロントエンド開発
npm run dev              # 開発サーバー起動（Vite）
npm run lint             # ESLint
npm run format           # Prettier

# JS テスト
npm test                 # ユニットテスト（Vitest）
npm run test:watch       # ウォッチモード
npm run test:coverage    # カバレッジ計測

# E2E テスト（Playwright・Chromium）
npm run test:e2e         # 通常実行
npm run test:e2e:ui      # Playwright UI モード
npm run test:e2e:headed  # ヘッド付きブラウザで実行（デバッグ用）

# C# データ準備ツール
dotnet run --project tools/PokelensTools      # マスターデータ生成
dotnet test tools/PokelensTools.Tests         # C# ユニット/統合テスト
```

単一ファイルのテスト実行例: `npx vitest run tests/unit/power-index-calc.test.js`

lint / format はコミットフックでは走りません（lint-staged / husky 等は未設定）。コミット前に必要に応じて手動で実行してください。

## Claude Code 開発環境（任意）

このリポジトリは Claude Code でのスペック駆動開発を前提にしており、`.claude/` にコマンド・サブエージェント・スキルがコミットされています。これらはクローンするだけで利用できます。

ただし `.claude/settings.json` で有効化している **claude-mem プラグインは本体がリポジトリに含まれない**ため、クローンした各自が手動でインストールする必要があります（未インストールだと有効化フラグだけ ON の状態になり機能しません）。

claude-mem はセッションをまたいでコンテキストを永続化するサードパーティ製プラグインです。Claude Code 内で以下を実行してください:

```text
/plugin marketplace add thedotmack/claude-mem
/plugin install claude-mem@thedotmack
```

インストール後に Claude Code を再起動すると有効になります。claude-mem を使わない場合は `.claude/settings.json` の `enabledPlugins` から該当エントリを削除してください（リポジトリ全体に影響するため、変更の際はチームで合意のうえで）。

## 主な機能

- **自分パーティ**: 6匹のカード一覧。選択でポケモン詳細（特性・持ち物・性格・種族値・実数値・技ごとの火力指数）を表示
- **相手パーティ**: 6スロットのカード入力。ポケモン名サジェスト検索（ひらがな/カタカナ/半角カナ/ローマ字対応）で素早く登録、選択で詳細（種族値・特性候補・素早さ6パターン）を表示
- **火力指数**: 威力 × 攻撃実数値 × タイプ一致補正 × 特性補正 × 持ち物補正。複数回攻撃技は最大総威力、威力不定技はパッチで定義した最大威力で計算

## 主要ディレクトリ

```
src/
├── main.js              # エントリーポイント
├── data/loader.js       # JSON 読み込み
├── logic/               # 純粋関数（火力指数 / 素早さ / 実数値 / 補正条件 / 名前検索）
└── ui/                  # DOM コンポーネント（自分/相手パーティ、詳細パネル、検索入力）

tests/
├── unit/                # Vitest ユニットテスト
└── e2e/                 # Playwright E2E テスト（page.route で party.json を mock）

data/                    # C# ツールが生成するマスターデータ + 手書き設定（party.json は gitignore 済み）
tools/PokelensTools/     # C# データ準備パイプライン
├── Patches/             # 手動管理パッチ JSON（champions / moves-power / *-modifiers / *-name-patch）
└── ...
docs/                    # 永続ドキュメント（PRD / 機能設計 / アーキテクチャ / リポジトリ構造 / 開発ガイドライン / 用語集）
└── testing/             # テスト仕様書（unit / integration / e2e）
.steering/               # 機能追加ごとのタスク管理（gitignore）
```

## ドキュメント

開発者向けの詳細は `docs/` を参照:

**永続ドキュメント:**
- [プロダクト要求定義書 (PRD)](docs/product-requirements.md)
- [機能設計書](docs/functional-design.md)
- [アーキテクチャ設計書](docs/architecture.md)
- [リポジトリ構造定義書](docs/repository-structure.md)
- [開発ガイドライン](docs/development-guidelines.md)
- [用語集](docs/glossary.md)

**テスト仕様書:**
- [単体テストケース一覧（Vitest / xUnit）](docs/testing/unit/automated-test-cases.md)
- [統合テストケース一覧（C# パイプライン）](docs/testing/integration/automated-test-cases.md)
- [E2E 自動テストケース一覧（Playwright）](docs/testing/e2e/automated-test-cases.md)
- [E2E 手動テストケース一覧（環境セットアップ）](docs/testing/e2e/manual-test-cases.md)

## ライセンス

[MIT License](LICENSE)
