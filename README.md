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

# 2. C# ツールでマスターデータを生成（pokedex / moves / items / abilities）
dotnet run --project tools/PokelensTools

# 3. data/party.json を自分のパーティで編集

# 4. 開発サーバー起動（必ず Vite 経由で開く。file:// 直接起動は非対応）
npm run dev
```

ブラウザで Vite が表示する URL（既定 http://127.0.0.1:5173）を開く。

## よく使うコマンド

```bash
# フロントエンド
npm run dev           # 開発サーバー起動（Vite）
npm test              # ユニットテスト（Vitest）
npm run test:watch    # ウォッチモード
npm run test:coverage # カバレッジ計測
npm run lint          # ESLint
npm run format        # Prettier

# C# データ準備ツール
dotnet run --project tools/PokelensTools         # マスターデータ生成
dotnet test tools/PokelensTools.Tests            # C# ユニット/統合テスト
```

単一ファイルのテスト実行例: `npx vitest run tests/unit/power-index-calc.test.js`

コミット前に husky + lint-staged が `.js` ファイルへ ESLint と Prettier を自動適用します。

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

data/                    # C# ツールが生成するマスターデータ + 手書き設定 + party.json
tools/PokelensTools/     # C# データ準備パイプライン
docs/                    # 永続ドキュメント（PRD / 機能設計 / アーキテクチャ等）
.steering/               # 機能追加ごとのタスク管理（gitignore）
```

## ドキュメント

開発者向けの詳細は `docs/` を参照:

- [プロダクト要求定義書 (PRD)](docs/product-requirements.md)
- [機能設計書](docs/functional-design.md)
- [アーキテクチャ設計書](docs/architecture.md)
- [リポジトリ構造定義書](docs/repository-structure.md)
- [開発ガイドライン](docs/development-guidelines.md)
- [用語集](docs/glossary.md)

## ライセンス

個人利用ツール（非公開想定）。
