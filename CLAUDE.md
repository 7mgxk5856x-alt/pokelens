# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

**pokelens** はポケモン対戦支援ツール。選出画面・対戦中に、自分パーティの火力指数・相手ポケモンの種族値・素早さ4パターンなどを即参照できるローカルWebツール。データソース: Pokémon Showdown。対象: 個人利用、PCブラウザ、ローカル環境。

## コマンド

```bash
npm run lint         # ESLint
npm run format       # Prettier
npm test             # テスト実行（vitest）
npm run test:watch   # vitestウォッチモード
npm run test:coverage
```

単一ファイルのテスト実行: `npx vitest run src/path/to/file.test.js`

コミット前にhusky + lint-stagedが`.js`ファイルへESLintとPrettierを自動適用する。

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

- `/setup-project` — `docs/ideas/` を元に上記6ドキュメントを対話的に作成。PRD承認後に後続ステップを自動実行。
- `/add-feature <機能名>` — 完全自動実行。`.steering/YYYYMMDD-<機能名>/` 配下にステアリングファイルを作成し、タスクを全実装、`implementation-validator`サブエージェントで品質検証、`test`・`lint`・`typecheck`の全パスを確認、必要に応じてdocsを更新。
- `/review-docs <パス>` — `doc-reviewer`サブエージェントでドキュメントをレビュー。

### ステアリングファイル

`.steering/` は機能ごとのタスク管理ファイルを格納（gitignore済み）。永続ドキュメントとは別管理で、一時的な作業記録として扱う。

## アーキテクチャメモ

- **フロントエンド:** JavaScript（ESモジュール、`"type": "module"`）
- **バックグラウンド処理:** C#（ポケモンデータの取得・変換などCLI/データ処理）
- **テスト:** Vitest（フロントエンド）
- **フロントエンドフレームワーク:** 未決定（`/setup-project` 完了後に `docs/architecture.md` で確定）
- 初期要件・アイデアは `docs/ideas/` に格納。サンプルファイル（`docs/ideas/initial-requirements-sample.md`）はgitignore済み。
