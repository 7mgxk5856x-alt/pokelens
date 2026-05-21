---
description: ブランチの差分からプルリクエストの title と description 案を生成する
---

# PR title / description 提案

引数（任意）: ベースブランチ名（例: `/suggest-pr main`）。省略時はブランチ名から自動判定する。

現在のブランチとベースの差分（コミット履歴 ＋ 変更内容）を解析し、`docs/development-guidelines.md` の規約に沿った PR の **title** と **description** 案を生成して `.claude/tmp/` に保存する。**PR は作成しない（`gh pr create` は実行しない）。**

## 実行方法

```bash
claude
# ベース自動判定
> /suggest-pr
# ベース指定
> /suggest-pr main
```

## 手順

### ステップ1: ベースの決定と差分の取得

**コマンド開始時に必ず以下を Bash で再実行し、最新の状態を取得する。**

1. 現在のブランチを確認する: `Bash('git branch --show-current')`
2. ベースブランチを決める:
  - **引数あり**: 指定されたブランチをベースにする。
  - **引数なし**: 現在のブランチ名から判定する。
    - `PN/feature/<機能>` → ベースは `PN/release`（存在しなければ `main`）
    - `PN/release` / `fix/*` / `chore/*` / その他 → `main`
    - 現在が `main`（デフォルトブランチ）の場合は「ベースブランチ上では PR を作成できません。feature/fix/chore ブランチに切り替えてください」と案内して**終了する**。
  - 判定したベースがローカルにもリモート追跡（`origin/<base>`）にも存在しない場合は `main` にフォールバックする（`git rev-parse --verify <base>` / `origin/<base>` で確認）。
3. 差分を取得する（**コミット済みの変更**が対象。未コミットの作業ツリー変更は PR に含まれない）:
  ```bash
  git log <base>..HEAD --oneline    # このブランチのコミット一覧
  git diff <base>...HEAD --stat     # 変更概要（3点リーダ = merge-base 以降）
  git diff <base>...HEAD            # 実際の差分内容
  ```
4. コミットが 0 件（ベースと差がない）の場合は「`<base>` との差分（コミット）がありません。先にコミットしてください」と案内して**終了する**。
5. 差分が著しく大きい場合（目安: 数百ファイル・数千行超）は全量を渡さず、`--stat` と代表的な差分に絞ってサブエージェントへ渡す。

### ステップ2: pr-writer サブエージェント起動

Task tool を使用して pr-writer サブエージェントを起動する:

- subagent_type: "pr-writer"
- description: "Draft pull request title and description"
- prompt: "ブランチの差分から PR の title と description 案を作成してください。\n\n## ベースブランチ\n[決定したベース]\n\n## コミット一覧（git log <base>..HEAD）\n[結果]\n\n## 変更概要（git diff <base>...HEAD --stat）\n[結果]\n\n## 差分内容（git diff <base>...HEAD。大きい場合は代表箇所）\n```diff\n[結果]\n```\n\n## 制約\n- title は docs/development-guidelines.md の Conventional Commits 規約に準拠（type/scope は同ファイルの表を単一の真実源とする）\n- description は 概要 / 変更内容 / テスト / 関連 の構成。末尾に Claude Code フッターを付ける\n- 差分・コミットに無い内容は書かない\n- git / gh は実行しない、ファイル保存もしない（提案のみ）"

### ステップ3: 提案結果の提示

サブエージェントが作成した **title と description の全文**をユーザーに提示する。

（全文の再掲はここだけでよい。ステップ4 後の最終応答では、保存先パスと `gh pr create` での利用方法の案内に絞る。）

### ステップ4: ファイルに保存

#### ステップ4-a: 既存ファイルの削除

`.claude/tmp/` は `.gitignore` 済みの一時領域である前提とする。書き込み前に既存の PR 案を削除する:

```bash
mkdir -p .claude/tmp
rm -f .claude/tmp/pr-title.txt .claude/tmp/pr-body.md
```

#### ステップ4-b: 書き込み

- `pr-writer` 出力の **`## PR title` ブロックのテキスト**を `.claude/tmp/pr-title.txt` に**1行のみ**で保存する（前後の空行・コードフェンス・引用符を含めない）。
- `pr-writer` 出力の **`## PR description` ブロックの本文**を `.claude/tmp/pr-body.md` に保存する（Markdown。`gh pr create --body-file` でそのまま使える形）。`pr-writer` がコードブロックで囲んで出力する場合は、外側のフェンス行（` ``` ` / ` ```markdown `）を除いた本文のみを保存する。
- 「判定根拠」ブロックはファイルに含めない（提案メタ情報のため）。
- 保存後、ユーザーへの最終応答で以下を明示する:
  - 保存先パス（`.claude/tmp/pr-title.txt` / `.claude/tmp/pr-body.md`）
  - 次のコマンドで PR を作成できること:
    ```bash
    gh pr create --base <base> --title "$(cat .claude/tmp/pr-title.txt)" --body-file .claude/tmp/pr-body.md
    ```

## 完了条件

- `.claude/tmp/pr-title.txt`（1行）と `.claude/tmp/pr-body.md` が保存されている。
- 最終応答で保存先パスと `gh pr create` の利用方法が提示されている。
- `gh pr create` / `git` は実行していない。

> ベース上（main）・差分 0 件で終了した場合は、案を保存せず案内のみで完了する。

## 出力形式

```markdown
# PR 提案

## ベース: <base> ← <現在のブランチ>（N コミット）

## PR title

\`\`\`
<type>(<scope>): <subject>
\`\`\`

## PR description

\`\`\`markdown
[本文]
\`\`\`
```

## 注意事項

- このコマンドは案の生成・保存のみで、`gh pr create` は実行しません
- 対象は**コミット済みの変更**（ベース以降）。未コミットの変更は PR に含まれません（必要なら先にコミット）
- サブエージェントは独立したコンテキストで動作するため、メインエージェントのコンテキストは消費しません
- 保存先 `.claude/tmp/` は `.gitignore` 済みの一時領域で、再実行ごとに上書きされます
