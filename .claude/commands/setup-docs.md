---
description: 承認済み PRD を元に派生5ドキュメントをまとめて生成する
---

# 派生ドキュメントの生成 (完全自動実行モード)

このコマンドは、**承認済みの `docs/product-requirements.md`（PRD）を元に、残り5つの永続ドキュメントをまとめて自動生成**します。PRD の作成は `/setup-project`、生成後の点検は `/review-doc` を使います。

**重要:** ステップ1の前提確認を通過したら、ユーザーの介入なしに最後まで自動で生成します。途中で停止・確認を求めないでください。

## 前提

- `docs/product-requirements.md` が存在すること。
- PRD は `/setup-project` で作成し、`/review-doc docs/product-requirements.md` でレビュー・改訂を済ませてあること（このコマンドは PRD を「固まったもの」として扱い、内容の妥当性は再検証しない）。

## 手順

### ステップ1: 前提確認

1. `docs/product-requirements.md` が存在するか確認する。
  - **存在しない場合**: 「PRD がありません。先に `/setup-project` で PRD を作成し、`/review-doc` でレビューしてから再実行してください」と案内して**終了する**。
2. 既存の派生ドキュメント（`docs/functional-design.md` 等）が既にある場合は、上書き前にその旨を完了報告に含める前提で進める（このコマンドは初回生成を主目的とする）。

### ステップ2: 機能設計書の作成

1. **functional-designスキル**をロード
2. `docs/product-requirements.md` を読む
3. スキルのテンプレートとガイドに従って `docs/functional-design.md` を作成

### ステップ3: アーキテクチャ設計書の作成

1. **architecture-designスキル**をロード
2. 既存のドキュメント（PRD・機能設計書）を読む
3. スキルのテンプレートとガイドに従って `docs/architecture.md` を作成

### ステップ4: リポジトリ構造定義書の作成

1. **repository-structureスキル**をロード
2. 既存のドキュメントを読む
3. スキルのテンプレートに従って `docs/repository-structure.md` を作成

### ステップ5: 開発ガイドラインの作成

1. **development-guidelinesスキル**をロード
2. 既存のドキュメントを読む
3. スキルのテンプレートに従って `docs/development-guidelines.md` を作成

### ステップ6: 用語集の作成

1. **glossary-creationスキル**をロード
2. 既存のドキュメントを読む
3. スキルのテンプレートに従って `docs/glossary.md` を作成

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
- 各ドキュメントの点検: /review-doc <パス>
  例: /review-doc docs/architecture.md
- 機能の追加: /add-feature <機能名>
- ソースの修正: /fix-code <修正内容>
」
```
