# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

**pokelens** はポケモン対戦支援ツール。選出画面・対戦中に、自分パーティの火力指数・相手ポケモンの種族値・素早さ6パターンなどを即参照できるローカルWebツール。データソース: Pokémon Showdown。対象: 個人利用、PCブラウザ、ローカル環境。

## コマンド

```bash
# フロントエンド
npm run dev           # Vite開発サーバー起動（必ずこれ経由で開く。file://不可）
npm run lint          # ESLint
npm run format        # Prettier
npm test              # テスト実行（vitest）
npm run test:watch    # vitestウォッチモード
npm run test:coverage

# C# データ準備ツール
dotnet run --project tools/PokelensTools      # マスターデータ生成
dotnet test tools/PokelensTools.Tests         # C# テスト実行
```

単一ファイルのテスト実行: `npx vitest run tests/unit/speed-calc.test.js`

lint / format はコミットフックでは走らない。必要に応じて `npm run lint` / `npm run format` を手動で実行する。

## 開発ワークフロー（スペック駆動開発）

このプロジェクトは**スペック駆動開発**を採用しており、永続ドキュメントとカスタムコマンドで進める。

### 永続ドキュメント（コーディング前に `/setup-project` で作成）

| ファイル | 用途 |
|---|---|
| `docs/product-requirements.md` | PRD — ユーザーストーリー、受け入れ条件 |
| `docs/functional-design.md` | 機能設計 |
| `docs/architecture.md` | アーキテクチャ設計 |
| `docs/repository-structure.md` | リポジトリ構造定義 |
| `docs/development-guidelines.md` | コーディング規約 |
| `docs/glossary.md` | ドメイン用語集 |

### カスタムコマンド

- `/setup-project` — `docs/ideas/` を元に **PRD（`docs/product-requirements.md`）のみ** を対話的に作成。承認されたら完了。レビュー(`/review-doc`)・改訂を経て PRD を固めてから `/setup-docs` へ。
- `/setup-docs` — 承認済み PRD を元に残り5ドキュメント（機能設計・アーキテクチャ・リポジトリ構造・開発ガイドライン・用語集）をまとめて自動生成。PRD が無ければ `/setup-project` を案内して終了。
- `/add-feature <機能名>` — 完全自動実行。`.steering/YYYYMMDD-<機能名>/` 配下にステアリングファイルを作成し、タスクを全実装、`test`・`lint` の全パスを確認、`implementation-validator`サブエージェントで品質検証、追加した自動テストを `docs/testing/` の一覧へ反映、アーキテクチャに影響がある場合は永続ドキュメントを更新。
- `/fix-code <修正内容>` — 完全自動実行。既存ソースの修正（バグ修正・リファクタ・`/review-code` 指摘の反映）を実装し、対応する自動テストを追加・更新、`implementation-validator`で検証、`test`・`lint`の全パスを確認、変更した自動テストを `docs/testing/` の一覧へ反映。新機能追加は `/add-feature` を使う。
- `/review-doc <パス>` — `doc-reviewer`サブエージェントでドキュメントをレビュー。
- `/review-code [<パス>]` — `code-reviewer`サブエージェントでコードをレビュー。可読性・設計・テスト・セキュリティ・仕様整合性・ドメイン妥当性・解析性・資源管理性の8観点で評価。引数を省略した場合は `git diff HEAD`(ステージ済み + 未ステージ)を対象にする。修正は自動で行わない。
- `/write-test-cases [<機能>]` — 完全自動実行。`docs/product-requirements.md` の受け入れ条件から手動・E2E テスト仕様書(`docs/testing/e2e/manual-test-cases.md`)を生成・更新。既存ファイルがあれば未カバー条件のみ追記(冪等)。コード由来の自動テスト一覧は対象外(`/add-feature`・`/fix-code` が担当)。`git commit` は実行しない。
- `/review-test-cases <パス>` — `test-case-reviewer`サブエージェントでテストケース仕様書をレビュー。テスト設計技法(同値分割・境界値・デシジョンテーブル・状態遷移・組み合わせ/ペアワイズ・ユースケース/シナリオ・エラー推測)とテスト観点(カバレッジ/品質特性/記述品質)の2軸で評価し、PRDの受け入れ条件と突き合わせて未カバー条件・不足ケースを提案する。修正は自動で行わない。
- `/review-claude-assets [<パス>]` — `claude-asset-reviewer`サブエージェントで `.claude/` 配下の作成物(コマンド・サブエージェント・スキル)をレビュー。単一責務・宣言との整合・冪等性・相互参照の実在・CLAUDE.md/development-guidelines との同期などを評価。引数省略時は `git diff HEAD` の `.claude/` 変更が対象。修正は自動で行わない。
- `/suggest-commit-message` — `commit-message-writer`サブエージェントで `git diff HEAD` からコミットメッセージ案を提案。Conventional Commits 規約準拠。`git commit` は実行しない。

### ステアリングファイル

`.steering/` は機能ごとのタスク管理ファイルを格納（gitignore済み）。永続ドキュメントとは別管理で、一時的な作業記録として扱う。

## アーキテクチャメモ

- **フロントエンド:** JavaScript（ESモジュール、`"type": "module"`）
- **バックグラウンド処理:** C#（ポケモンデータの取得・変換などCLI/データ処理）
- **テスト:** Vitest（フロントエンド）
- **フロントエンドフレームワーク:** Vanilla JS（フレームワークなし）
- 初期要件・アイデアは `docs/ideas/` に格納。サンプルファイル（`docs/ideas/initial-requirements-sample.md`）はgitignore済み。
