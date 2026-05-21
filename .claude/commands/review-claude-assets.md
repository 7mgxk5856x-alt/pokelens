---
description: .claude/ 配下の作成物（コマンド・サブエージェント・スキル）の詳細レビューをサブエージェントで実行
---

# Claude アセットレビュー

引数(任意): ファイル/ディレクトリパス(例: `/review-claude-assets .claude/commands/add-feature.md`)

引数を省略した場合は **HEAD との差分**(`git diff HEAD`、ステージ済み + 未ステージ)のうち `.claude/` 配下の変更をレビュー対象とする。

`claude-asset-reviewer` サブエージェントで、カスタムコマンド・サブエージェント・スキルの定義特有の観点（単一責務・宣言との整合・冪等性・相互参照の実在・ドキュメント同期など）をレビューする。**修正は自動で行わない。**

## 実行方法

```bash
claude
# パス指定
> /review-claude-assets .claude/commands/add-feature.md
# 引数なし: .claude/ 配下の未コミット変更をレビュー
> /review-claude-assets
```

主な対象:

- コマンド: `.claude/commands/*.md`
- サブエージェント: `.claude/agents/*.md`
- スキル: `.claude/skills/<name>/SKILL.md`（＋ `template.md` / `guide.md`）

## 手順

### ステップ1: 対象の決定

- **引数あり**: 指定パスが存在するか確認する。ディレクトリの場合は配下の対象ファイルを列挙する。
- **引数なし**: 以下で `.claude/` 配下の変更を対象にする。**`.claude/tmp/`（一時領域）は除外する。**
  ```bash
  git diff HEAD --name-only -- .claude/ ':(exclude).claude/tmp/'   # 変更対象（ステージ済み + 未ステージ）
  git diff HEAD -- .claude/ ':(exclude).claude/tmp/'               # 実際の差分
  ```
  対象が 0 件の場合は「`.claude/`（`tmp/` を除く）に HEAD との差分がありません。レビュー対象がないため終了します」と報告して終了する。

### ステップ2: claude-asset-reviewer サブエージェント起動

Task tool を使用して claude-asset-reviewer サブエージェントを起動する。プロンプトは引数の有無で切り替える。

**引数ありの場合**:
- subagent_type: "claude-asset-reviewer"
- description: "Claude asset review"
- prompt: "[対象パス] をレビューしてください。\n\n対象の型（command / agent / skill）を判定し、`claude-asset-reviewer` の定義にある共通観点・型別観点のすべてで評価してください（観点はエージェント定義を単一の真実源とし、ここでは列挙しない）。\n\n手順:\n1. 対象を Read する（skill の場合は同ディレクトリの template.md / guide.md も読む）\n2. 参照先（Skill('...') / subagent_type / 参照パス）が実在するか実際に確認する\n3. CLAUDE.md のコマンド一覧・docs/development-guidelines.md の役割表と一致しているか確認する\n\nレビューレポートを作成してください。自動修正は行わないでください。"

**引数なしの場合**(`.claude/` の差分をレビュー):
- subagent_type: "claude-asset-reviewer"
- description: "Claude asset review (HEAD diff)"
- prompt: "`.claude/` 配下の HEAD との差分を対象にレビューしてください（`.claude/tmp/` は一時領域のため対象外）。\n\n対象ファイル一覧:\n[git diff HEAD --name-only -- .claude/ ':(exclude).claude/tmp/' の結果]\n\n手順:\n1. `git diff HEAD -- .claude/ ':(exclude).claude/tmp/'` で差分を取得し、必要に応じて各ファイルを Read する（skill は template.md / guide.md も）\n2. 変更行を中心に、対象の型（command / agent / skill）ごとに `claude-asset-reviewer` の定義にある共通観点・型別観点のすべてで評価する\n3. 参照先（Skill('...') / subagent_type / 参照パス）の実在、CLAUDE.md・docs/development-guidelines.md との同期を実際に確認する\n\nレビューレポートを作成してください。自動修正は行わないでください。"

### ステップ3: レビュー結果の要約

サブエージェントが作成したレビューレポートの要点を抽出し、ユーザーに報告する。特に「参照切れ」「ドキュメント不同期」「責務超過」を明示する。

## 出力形式

```markdown
# Claude アセットレビュー結果

## 対象: [パス または ".claude/ の差分（N ファイル）"]

### 主な改善点

1. [改善点1] (優先度: 高/中/低)
2. [改善点2] (優先度: 高/中/低)
3. [改善点3] (優先度: 高/中/低)

### 参照・同期の問題

- [参照切れ / CLAUDE.md・development-guidelines との不一致 など]

### 総合評価

[1-5]/5

### 次のアクション

- [推奨対応1]
- [推奨対応2]

詳細なレポートはサブエージェントの出力を参照してください。
```

## 注意事項

- レビューは詳細な分析のため、数分かかる場合があります
- サブエージェントは独立したコンテキストで動作するため、メインエージェントのコンテキストは消費しません
- 自動修正は行いません。検出された問題の修正は別途実施してください
- 引数なしモードは「ステージ済み + 未ステージ」の差分が対象です。未追跡(untracked)ファイルは含まれません(必要なら `git add` 後に再実行するか、パスを明示してください)
- 引数なしモードでは `.claude/tmp/`（コミットメッセージ案などの一時領域・`.gitignore` 済み）はレビュー対象から除外します
