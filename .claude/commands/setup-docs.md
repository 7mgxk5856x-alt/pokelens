---
description: 承認済み PRD を元に派生5ドキュメントをまとめて生成する
---

# 派生ドキュメントの生成 (完全自動実行モード)

このコマンドは、**承認済みの `docs/product-requirements.md`（PRD）を元に、残り5つの永続ドキュメントをまとめて自動生成**します。PRD の作成は `/setup-project`、生成後の点検は `/review-doc` を使います。

**重要:** ステップ1の前提確認を通過したら、ユーザーの介入なしに最後まで自動で生成します。途中で停止・確認を求めないでください。**`git commit` は行いません。**

## 前提

- `docs/product-requirements.md` が存在すること。
- PRD は `/setup-project` で作成し、`/review-doc docs/product-requirements.md` でレビュー・改訂を済ませてあること（このコマンドは PRD を「固まったもの」として扱い、内容の妥当性は再検証しない）。

## 手順

### ステップ1: 前提確認

1. `docs/product-requirements.md` が存在するか確認する。
  - **存在しない場合**: 「PRD がありません。先に `/setup-project` で PRD を作成し、`/review-doc` でレビューしてから再実行してください」と案内して**終了する**。
2. このコマンドは PRD 確定後の**初回生成**を主目的とする。既存の派生ドキュメントがある場合は **無条件に上書き（全書き換え）する**（再実行時も同様）。上書きしたファイルは完了報告に明記する。
3. PRD がレビュー・改訂済みであること（前提）は機械的に検証しない。ユーザーの責任に委ね、PRD の内容の妥当性は再検証しない。

### ステップ2: 機能設計書の作成

1. `Skill('functional-design')` をロード
2. `docs/product-requirements.md` を読む
3. スキルのテンプレートとガイドに従って `docs/functional-design.md` を作成

### ステップ3: アーキテクチャ設計書の作成

1. `Skill('architecture-design')` をロード
2. PRD・機能設計書を読む
3. スキルのテンプレートとガイドに従って `docs/architecture.md` を作成

### ステップ4: リポジトリ構造定義書の作成

1. `Skill('repository-structure')` をロード
2. PRD・機能設計書・アーキテクチャ設計書を読む
3. スキルのテンプレートとガイドに従って `docs/repository-structure.md` を作成

### ステップ5: 開発ガイドラインの作成

1. `Skill('development-guidelines')` をロード
2. PRD・機能設計書・アーキテクチャ設計書・リポジトリ構造定義書を読む
3. スキルのテンプレートとガイドに従って `docs/development-guidelines.md` を作成

### ステップ6: 用語集の作成

1. `Skill('glossary-creation')` をロード
2. これまでに生成した全ドキュメント（PRD・機能設計書・アーキテクチャ設計書・リポジトリ構造定義書・開発ガイドライン）を読む
3. スキルのテンプレートとガイドに従って `docs/glossary.md` を作成

## 完了条件

- 派生5ドキュメント（機能設計・アーキテクチャ・リポジトリ構造・開発ガイドライン・用語集）が全て作成されていること

完了時のメッセージ:
```
「派生ドキュメントを生成しました!

✅ docs/functional-design.md
✅ docs/architecture.md
✅ docs/repository-structure.md
✅ docs/development-guidelines.md
✅ docs/glossary.md

これで6つの永続ドキュメントが揃い、開発を開始する準備が整いました。

次の使い方:
- 各ドキュメントの点検: 生成した各ファイルに /review-doc をかける
    /review-doc docs/functional-design.md
    /review-doc docs/architecture.md
    /review-doc docs/repository-structure.md
    /review-doc docs/development-guidelines.md
    /review-doc docs/glossary.md
- 機能の追加: /add-feature <機能名>
- ソースの修正: /fix-code <修正内容>
」
```
