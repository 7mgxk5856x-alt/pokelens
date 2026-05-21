# 開発ガイドライン (Development Guidelines)

## コーディング規約

### 命名規則

#### 変数・関数

```js
// ✅ 良い例
const pokemonMaster = await loadPokemonData();
function calcPowerIndex(move, actualStats) { }
const isStab = pokemonTypes.includes(move.type);

// ❌ 悪い例
const data = await load();
function calc(m, s) { }
```

| 種別 | 規則 | 例 |
|------|------|-----|
| 変数 | camelCase、名詞 | `baseSpe`, `partyData` |
| 関数 | camelCase、動詞始まり | `calcSpeedPatterns`, `loadParty` |
| 定数（モジュールスコープ） | UPPER_SNAKE_CASE | `STAB_MODIFIER`, `MAX_PARTY_SIZE` |
| Boolean | `is` / `has` / `can` 始まり | `isPhysical`, `hasAbilityBonus` |

#### ファイル名

| 種別 | 規則 | 例 |
|------|------|-----|
| JSファイル | kebab-case | `power-index-calc.js`, `name-search.js` |
| テストファイル | `[対象].test.js` | `power-index-calc.test.js` |
| C# クラスファイル | PascalCase | `MergeConverter.cs` |

---

### コードフォーマット

- インデント: **2スペース**（Prettier が自動適用）
- 最大行長: **100文字**
- 文字列: **シングルクォート**
- セミコロン: **あり**

Prettier と ESLint が `git commit` 前に自動適用される。手動で実行する場合:

```bash
npm run format   # Prettier
npm run lint     # ESLint（コミット前の lint-staged では --fix が自動適用される）
```

---

### 制御構文のブロック（波括弧）

`if` / `else` / `for` / `while` / `foreach` などの制御構文の本体は、**一行でも必ず波括弧 `{}` で囲む**（JavaScript・C# 共通）。後から行を追加して複数行化したときの波括弧の書き忘れ・字下げの誤認によるバグを防ぐため。

```js
// ✅ 良い例
if (isStab) {
  power *= STAB_MODIFIER;
}

// ❌ 悪い例: 波括弧なし（行を足すと壊れやすい）
if (isStab) power *= STAB_MODIFIER;
```

（JavaScript は ESLint の `curly` ルールで機械的に強制できる。）

---

### 変数宣言（JavaScript）

`const` を既定とし、再代入が必要な場合のみ `let` を使う。`var` は使わない（関数スコープ・巻き上げの落とし穴を避けるため）。

---

### コメント規約

**コメントは WHY のみ書く**。コードを読めばわかる WHAT は書かない。

**コメントは日本語で書く**。ただし日本語に訳しにくい用語や、英語のほうが正確・明快な場合（技術用語・API 名・ドメイン用語など）は英語のままでよい。

```js
// ✅ 良い例: なぜそうするかを説明
// Showdownデータは威力不定技の power が null のため最大値で代替する
const basePower = move.power ?? move.maxPower;

// ❌ 悪い例: コードを読めばわかる
// basePower を設定する
const basePower = move.power ?? move.maxPower;
```

**ドキュメンテーションコメント**: 公開 API には、その言語の標準的なドキュメンテーションコメントを付ける。

- **JavaScript**: `export` する関数・モジュールの公開インターフェースに **JSDoc**（`/** ... */`、`@param` / `@returns` 等）を付ける（例: `src/data/loader.js` のエクスポート関数）。
- **C#**: `public` / `internal` で外部（他クラス・テスト）から使う型・メンバーに **XML Documentation Comments**（`/// <summary>` / `/// <param>` / `/// <returns>` 等）を付ける。

非 export のヘルパーや `private` メンバー、関数名と引数名だけで意図が伝わる単純なロジック関数には不要。WHAT の繰り返しになる冗長なドキュメンテーションコメントは書かない。

ただし private / 非 export の関数でも、理由が非自明な場合は WHY コメント（ドキュメンテーションコメントとは別）を付けてよい。コメントを付けるかは可視性ではなく「名前・引数・コードだけで意図が伝わるか」で判断する。

---

### 関数設計

**純粋関数を優先する**（`src/logic/` 配下は必ず純粋関数）:

```js
// ✅ 良い例: 副作用なし、同じ入力→同じ出力（Pokémon Champions 計算式）
export function calcSpeedPatterns(baseSpe) {
  const fastest = calcSpeed(baseSpe, 32, 1.1);
  const fast    = calcSpeed(baseSpe, 32, 1.0);
  return {
    fastestScarf: Math.floor(fastest * 1.5),
    fastScarf:    Math.floor(fast    * 1.5),
    fastest,
    fast,
    neutral: calcSpeed(baseSpe, 0, 1.0),
    slowest: calcSpeed(baseSpe, 0, 0.9),
  };
}

// ❌ 悪い例: 外部状態に依存
export function calcSpeedPatterns(pokemon) {
  const baseSpe = window.pokemonData[pokemon].baseStats.spe; // DOM/グローバルに依存
  ...
}
```

**関数の長さ**: 目安 30行以内。超えた場合は責務を分割する。

---

### エラーハンドリング

**システム境界（JSON読み込み）でのみ try-catch を使う**。ロジック層は例外を投げてよい。

```js
// ✅ DataLoader でのエラー処理（システム境界）
export async function loadData() {
  let pokemonMaster, party;
  try {
    [pokemonMaster, party] = await Promise.all([
      fetch('./data/pokemon-data.json').then(r => r.json()),
      fetch('./data/party.json').then(r => r.json()),
    ]);
  } catch (e) {
    throw new Error('データファイルの読み込みに失敗しました。pokemon-data.json と party.json を確認してください。');
  }
  return { pokemonMaster, party };
}

// ✅ ロジック層はエラーを握り潰さない
export function calcPowerIndex(move, actualStats, pokemonTypes, abilityModifier, itemModifier) {
  if (move.category === 'Status') return null;
  // null を返すのはコントラクトの一部（変化技）であり、エラーではない
  const basePower = move.power ?? move.maxPower;
  ...
}
```

**ユーザー向けエラー表示**: `src/ui/` 内で行う。エラーメッセージは具体的に（何が起きたか + 対処法）。

具体的なエラー種別・メッセージ定義・フォールバック挙動は [`docs/functional-design.md` のエラーハンドリング表](./functional-design.md) を参照すること。

---

### DOM 操作のセキュリティ

**innerHTML は使わない**。テキスト挿入は必ず `textContent` を使う:

```js
// ✅ 安全
element.textContent = pokemonName;

// ❌ XSS リスク
element.innerHTML = pokemonName;
```

テンプレートが必要な場合は `<template>` 要素か `document.createElement` を使う。

---

### アクセス制御（カプセル化）

**最小公開**を原則とする。公開（C# の `public` / JavaScript の `export`）するのは意図した API だけに限り、実装詳細は閉じる（C#: `private` / `internal`、JS: 非 export のモジュール内関数）。

- **テストのために可視性を広げない**。private / internal な処理を「テストしたいから」という理由だけで `public` 化・`export` 化しない。代わりに:
  - **公開 API を通してテストする**（内部の挙動も多くは公開面から観測できる）。
  - 単独でテストする価値があるほど複雑なら、**独立した単位（クラス・関数・モジュール）に切り出す**。それは「テスト用の公開」ではなく正当な API になる。
- **C#**: 既定は `internal`（このリポジトリの `tools/` はライブラリ公開しない実行アプリのため `public` にする必要はない）。テストアセンブリから内部型を検証したい場合は、`public` 化せず `[assembly: InternalsVisibleTo("PokelensTools.Tests")]` で見せる。
- **JavaScript**: `export` した時点でモジュールの公開 API。`src/logic/` の純粋関数のように「テスト対象の単位そのもの」を export するのは正当。内部ヘルパーをテストのためだけに export しない。

---

### C# コーディング規約（tools/PokelensTools/）

.NET 8 標準スタイルに従う。

| 種別 | 規則 | 例 |
|------|------|-----|
| クラス・インターフェース | PascalCase | `ShowdownFetcher`, `MergeConverter` |
| メソッド | PascalCase | `FetchDataAsync()` |
| ローカル変数・引数 | camelCase | `pokemonList`, `baseStats` |
| 非同期メソッド | `Async` サフィックス | `FetchDataAsync()` |
| private インスタンスフィールド | `_camelCase`（`_` 接頭辞） | `_http` |
| 定数・private static readonly | PascalCase | `ConcurrencyLimit`, `WriteOptions` |

> `this.` は曖昧さ回避や自身のインスタンスを渡す場合を除き省略する。private インスタンスフィールドは `_` 接頭辞でローカル変数・引数と区別するため、通常 `this.` は不要。

**`var` の使用**: 右辺から型が自明な場合のみ `var` を使う。型が読み取れない場合（特にメソッド戻り値など）は明示的な型を書く。

```csharp
// ✅ 右辺で型が自明
var items = new List<Pokemon>();
var name = "ガブリアス";

// ❌ 型が読めない → 明示型にする
var count = GetCount();   // → int count = GetCount();
```

**champions-patch.json の管理ルール**:
- Pokémon Champions 独自データ（Showdown データとの差分）を手書き管理するファイル
- 追加・変更時は既存エントリのフォーマットを維持する
- 変更理由はコミットメッセージに記載する（例: `chore(tools): champions-patch.json にXXXを追加`）

**name-patch ファイル（`pokemon-name-patch.json` / `item-name-patch.json`）の管理ルール**:
- 各ファイルは `{ "showdownキー": "日本語名" }` のフラット形式。`_comment` 等 `_` 始まりのキーはコメント用途として使える（Showdown キーと衝突しないため副作用なし）
- 値は Showdown キーがある場合、`pokeapi-translations.json` 由来の日本語名を**上書き**する。値がない場合は翻訳不在を補完する
- 追加時は対応する Showdown キーが `cache/showdown-pokedex.json` / `cache/showdown-items.json` に存在することを確認する（フィルタで除外されたキーへの上書きは無効）
- `pokemon-name-patch.json` は同名重複を解消する目的のため、追加値が他エントリと一意であることを確認する
- 変更理由はコミットメッセージに記載する（例: `chore(tools): pokemon-name-patch.json に〇〇を追加`）

---

## テスト戦略

### テスト対象と種別

| 対象 | 種別 | ファイル |
|------|------|---------|
| 火力指数計算 | ユニット | `tests/unit/power-index-calc.test.js` |
| 補正条件解決 | ユニット | `tests/unit/resolve-modifier.test.js` |
| 素早さ計算 | ユニット | `tests/unit/speed-calc.test.js` |
| 名前検索・正規化 | ユニット | `tests/unit/name-search.test.js` |
| 実数値計算 | ユニット | `tests/unit/calc-actual-stats.test.js` |
| DataLoader JSON 読み込み | ユニット | `tests/unit/loader.test.js` |
| DataLoader → UI フロー | 統合 | `tests/integration/data-flow.test.js` |

UIコンポーネント（`src/ui/`）は手動ブラウザテストで確認する（E2Eテストは不要）。

**手動テスト対象ブラウザ**: Chrome 最新版

**確認項目**（機能追加・変更時に必ず実施）:
- 対象コンポーネントが正常に表示されること
- 自分ポケモン選択時に性格（上昇/下降ステの↑↓付き）・種族値（H-A-B-C-D-S）・実数値（H-A-B-C-D-S）が正しく表示されること
- 自分ポケモン選択時に各技の火力指数が表示されること（変化技は「−」）
- ポケモン名サジェストが表示・選択できること（ひらがな/カタカナ/半角カナ/ローマ字いずれの入力でも）
- タブキーで6つの相手パーティ入力欄を順番に移動できること
- 入力中にタブキー / 矢印キー（↓↑）でサジェスト候補を選択し、エンターキーで決定できること
- 各パネル（種族値・素早さ6パターン・火力指数）が正しく切り替わること
- 入力リセット・ページリロード後に初期状態に戻ること

### テストの書き方（Given-When-Then）

```js
import { describe, it, expect } from 'vitest';
import { calcSpeedPatterns } from '../../src/logic/speed-calc.js';
import { calcPowerIndex } from '../../src/logic/power-index-calc.js';

describe('calcSpeedPatterns', () => {
  it('種族値90のポケモンで素早さ6パターンを正しく計算する', () => {
    // Given
    const baseSpe = 90;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    // floor((90 + 能力ポイント + 20) × 性格補正)、スカーフはfloor(実数値 × 1.5)
    expect(result.fastestScarf).toBe(234); // floor(156 * 1.5)
    expect(result.fastScarf).toBe(213);    // floor(142 * 1.5)
    expect(result.fastest).toBe(156);      // floor((90+32+20)*1.1)
    expect(result.fast).toBe(142);         // floor((90+32+20)*1.0)
    expect(result.neutral).toBe(110);      // floor((90+0+20)*1.0)
    expect(result.slowest).toBe(99);       // floor((90+0+20)*0.9)
  });
});

describe('calcPowerIndex', () => {
  it('変化技はnullを返す', () => {
    // Given
    const move = { name: 'まもる', category: 'Status', power: null };

    // When / Then
    expect(calcPowerIndex(move, {}, [], 1.0, 1.0)).toBeNull();
  });
});
```

### テスト命名

`describe` に関数名・モジュール名、`it` に日本語自然文で条件と期待結果を記述する:

```js
describe('calcPowerIndex', () => {
  it('変化技はnullを返す', () => { ... });
  it('物理技は攻撃実数値を使って計算する', () => { ... });
});
```

### カバレッジ目標

- `src/logic/` 配下: **80% 以上**
- `src/data/` 配下: 正常系・ファイル不存在時のエラーパスをカバーする（数値目標は設けない）
- `src/ui/` 配下: 対象外（手動テスト）

```bash
npm run test:coverage   # カバレッジレポート生成
```

---

## Git 運用

### ブランチ戦略

個人プロジェクトのため軽微な変更は `main` 直コミットを許容する。機能開発は **優先度（PN: `P0` / `P0.5` / `P1` …）を最上位の名前空間** にしたブランチで進める。

```
main
└─ P0/release                  # P0 統合ブランチ（E2E はここで実施）
   ├─ P0/feature/own-party     # 個別機能ブランチ
   ├─ P0/feature/power-index
   └─ P0/feature/opponent-search
```

- 個別機能は `PN/feature/<機能>`、PN 単位の統合は `PN/release`。
  - `feature/PN` と `feature/PN/<機能>` は git のブランチ参照が D/F（ディレクトリ/ファイル）競合を起こし共存できないため、優先度を最上位（`PN/...`）に置いてこれを回避している。
- バグ修正・リファクタは `fix/<対象>`、コマンド・ツール等の整備は `chore/<対象>`（優先度フェーズに紐づかないため、種別を最上位に置いたまま）。

**統合の流れ（PN ごと）**:

1. `PN/feature/<機能>` で 1 機能を実装し、テスト・レビューまで完了させる。
2. 完了した機能ブランチを `PN/release` にマージする。
3. PN の全機能ブランチが `PN/release` にマージされたら、`PN/release` 上で **E2E**（手動・E2E テスト）に進む。
4. E2E を通過したら `PN/release` を `main` にマージする。

### コミットメッセージ（Conventional Commits）

```
<type>(<scope>): <subject>
```

| type | 用途 |
|------|------|
| `feat` | 新機能 |
| `fix` | バグ修正 |
| `docs` | ドキュメント |
| `refactor` | リファクタリング |
| `test` | テスト追加・修正 |
| `chore` | ビルド・設定変更 |

有効な `scope` 一覧:

| scope | 対象 |
|-------|------|
| `ui` | `src/ui/` |
| `logic` | `src/logic/` |
| `data` | `src/data/` |
| `tools` | `tools/PokelensTools/` |
| `docs` | `docs/` |
| `config` | `vite.config.js` / `vitest.config.js` / `eslint.config.js` 等 |

**例**:
```
feat(ui): 相手ポケモン詳細パネルを追加
fix(logic): 素早さ計算のfloorの位置を修正
test(logic): 火力指数の変化技ケースを追加
chore(config): vitest.config.js の include パターンを追加
```

---

## 開発環境セットアップ

### 必要なツール

| ツール | バージョン | 用途 |
|--------|-----------|------|
| Node.js | LTS | Vite・Vitest 実行 |
| npm | LTS付属 | パッケージ管理 |
| .NET SDK | 8.0以上 | C# データ準備ツール |

### セットアップ手順

```bash
# 1. 依存関係のインストール
npm install

# 2. C# ツールでマスターデータを生成
dotnet run --project tools/PokelensTools

# 3. data/party.json を自分のパーティで編集

# 4. 開発サーバー起動（file:// ではなく必ず Vite 経由で開く）
npm run dev
```

### よく使うコマンド

```bash
# JS（フロントエンド）
npm run dev           # 開発サーバー起動（Vite）
npm test              # テスト実行
npm run test:watch    # ウォッチモード
npm run test:coverage # カバレッジ計測
npm run test:ui       # Vitest ブラウザ UI（任意・デバッグ補助）
npm run lint          # ESLint
npm run format        # Prettier

# C# ツール
dotnet run --project tools/PokelensTools      # マスターデータ生成
dotnet test tools/PokelensTools.Tests         # C# ユニット・統合テスト実行
```

---

## 開発ワークフロー（スペック駆動 × コマンド駆動）

本プロジェクトはスペック駆動開発を採用し、`docs/` の永続ドキュメントを起点に、Claude Code のカスタムコマンドで「設計 → 実装 → レビュー → コミット」を進める。コマンドの概要は `CLAUDE.md`、各コマンドの定義は `.claude/commands/`、サブエージェントの定義は `.claude/agents/` を参照。

### コマンドの役割

| コマンド | 役割 | 変更を加えるか | 主な実行タイミング |
|---|---|---|---|
| `/setup-project` | `docs/ideas/` から **PRD のみ** を対話作成（承認で完了。`/review-doc` を経て `/setup-docs` へ） | 生成する | プロジェクト初期（一度きり） |
| `/setup-docs` | 承認済み PRD から派生5ドキュメント（機能設計・アーキテクチャ・リポジトリ構造・開発ガイドライン・用語集）をまとめて生成 | 生成する | PRD 確定後（一度きり） |
| `/add-feature <機能名>` | 機能を全自動実装（ステアリング作成 → 実装 → `implementation-validator` 検証 → test/lint 通過 → テスト一覧反映） | 実装する | 機能ごと |
| `/fix-code <修正内容>` | 既存ソースの修正を全自動実行（実装 → テスト追加/更新 → test/lint 通過 → `implementation-validator` 検証 → テスト一覧反映） | 修正する | バグ修正・リファクタ・指摘反映 |
| `/write-test-cases [<機能>]` | PRD の受け入れ条件から手動・E2E テスト仕様書（`e2e/manual-test-cases.md`）を生成・更新 | 生成する | 受け入れ条件の追加・変更時 |
| `/review-doc <パス>` | 永続ドキュメントを品質レビュー | 指摘のみ | ドキュメント変更時 |
| `/review-code [<パス>]` | コードを8観点でレビュー（引数省略で `git diff HEAD`） | 指摘のみ | 実装後 |
| `/review-test-cases <パス>` | テストケース仕様書を「設計技法 × テスト観点」の2軸でレビュー | 指摘のみ | テスト仕様の更新時 |
| `/suggest-commit-message` | `git diff HEAD` からコミットメッセージ案を生成し `.claude/tmp/` に保存 | 指摘のみ（commit しない） | コミット前 |

> `setup-project` / `add-feature` / `fix-code` は **手を動かす**（生成・実装・修正する）コマンド。`review-*` / `suggest-commit-message` は **判断を助ける**（提案・指摘のみで、ファイルを変更しない）コマンド。`/review-code` の指摘の反映は `/fix-code` に渡すか、手動で行う。

### 機能開発の標準フロー

1機能は `PN/feature/<機能>` ブランチで進める（→「Git 運用 / ブランチ戦略」）。

```
1. ブランチ作成     git switch -c P0/feature/<機能名>
2. 実装            /add-feature <機能名>
                   └ ステアリング作成・実装・検証・test/lint を自動で通し、
                     追加した自動テストを docs/testing/ の一覧へ反映する
3. コードレビュー    /review-code
                   └ 自動実装の結果を人の目で点検し、指摘は手で修正
4. テスト仕様の点検  /review-test-cases <パス>（/add-feature が反映した一覧を点検）
5. コミット         /suggest-commit-message → git commit -F .claude/tmp/commit-msg-1.txt
6. PN 統合へマージ   P0/feature/<機能名> → P0/release
```

- `/add-feature` が「実装完了基準」（test / lint / カバレッジ）まで通すため、3〜4 は「自動実装の成果物を点検・補強する」工程に位置づく。
- PN の全機能が `PN/release` に揃ったら、次の「E2E フェーズ」へ進む。
- **ブランチはコミット直前でもよい**: `/add-feature` はコミットせず変更を未コミットで残すため、`main` 上で実装してしまっても `git switch -c P<N>/feature/<機能名>` で変更ごと新ブランチへ移せる（`main` はクリーンのまま・手戻りなし）。`/add-feature` は完了時に `main` 上だと同趣旨の注意を表示する。

### E2E フェーズ（PN 統合後）

PN の全機能ブランチが `PN/release` にマージされたら、`PN/release` 上で手動・E2E テストを実施する。

```
1. 仕様生成   /write-test-cases   … PRD 受け入れ条件から docs/testing/e2e/manual-test-cases.md を生成・更新
2. 仕様点検   /review-test-cases docs/testing/e2e/manual-test-cases.md
3. 実行       Chrome で手動・E2E テストを実施（docs/testing/e2e/ の手順に従う）
4. main へ    E2E 通過後、PN/release を main にマージ
```

### 修正（バグ修正・リファクタ・指摘反映）のフロー

バグ修正・リファクタ・`/review-code` 指摘の反映は `fix/*` ブランチで進める（→「Git 運用 / ブランチ戦略」）。

```
1. ブランチ作成     git switch -c fix/<対象>
2. 修正            /fix-code <修正内容>
                   └ 修正実装・テスト追加/更新・検証・test/lint を自動で通し、
                     変更した自動テストを docs/testing/ の一覧へ反映する
3. テスト仕様の点検  /review-test-cases <パス>（/fix-code が反映した一覧を点検）
4. コミット         /suggest-commit-message → git commit -F .claude/tmp/commit-msg-1.txt
```

**自動テストとテストケース仕様書の同期は「修正を行う側の責務」とする。**

- **基本は `/fix-code`（および `/add-feature`）を使う**。これらのコマンドは、修正に伴う自動テストの追加・変更を `docs/testing/` の該当一覧へ反映するところまでを1つの作業として行う。
- **手動でソースを修正した場合**は、テストケースを確認し、必要に応じて **自動テストとテストケース仕様書（`docs/testing/`）の両方**を編集する。テストコードと仕様書一覧は同じコミットで同期させる（テストコードだけ・仕様書だけが変わった状態でコミットしない）。

### ドキュメント・プロセス変更のフロー

- 永続ドキュメント（`docs/*.md`）を変更したら `/review-doc <パス>` で点検する。
- コマンド・サブエージェント・テストドキュメントなど **プロセス/ツールの整備は `chore/*` ブランチ**で進める（例: `chore/dev-workflow`）。機能コードのブランチ（`PN/feature/*`）とは分け、履歴の意図を読み取りやすく保つ。

### コミットの進め方

- コミット前に `/suggest-commit-message` でメッセージ案を作り、Conventional Commits 規約（→「Git 運用 / コミットメッセージ」）に沿ってコミットする。
- 複数の論理単位が混在する場合はコマンドが分割を提案する。分割時は **依存される変更（前提となる側）を先**にコミットする（`commit-msg-N.txt` の番号順がそのまま `git commit` の実行順になる）。

---

## 実装完了基準

以下を全て満たした時点で実装完了とみなす（`/add-feature` の品質判定基準も同じ）:

- [ ] `npm test` が全パス（既存テストを含む）
- [ ] `npm run lint` が全パス
- [ ] 新規ロジック関数のカバレッジ 80% 以上（`npm run test:coverage` で確認）
- [ ] C# ツール側を変更した場合は `dotnet test tools/PokelensTools.Tests` が全パス
- [ ] UIコンポーネント変更がある場合は Chrome 最新版で手動テスト完了

---

## 実装チェックリスト

新しいロジック関数を追加するとき:

- [ ] `src/logic/` に純粋関数として実装
- [ ] `src/data/` や `src/ui/` を import しない（データは引数として受け取る）
- [ ] 対応するユニットテストを `tests/unit/` に追加
- [ ] 正常系・変化技・威力不定技などのエッジケースをテスト
- [ ] `npm test` と `npm run lint` がパスすること

新しいUIコンポーネントを追加するとき:

- [ ] `src/ui/` にファイルを追加（ファイル名: kebab-case、クラス名: PascalCase）
- [ ] `innerHTML` を使わず `textContent` / `document.createElement` / `<template>` を使う
- [ ] 他の `src/ui/` コンポーネントを直接 import しない（コンポーネント間の連携はコンストラクタへのコールバック注入で行う。`main.js` がコールバックを生成して各コンポーネントに渡す）
- [ ] import してよいのは `src/logic/` と `src/data/`、および `src/ui/` 内の純粋ユーティリティモジュール（コンポーネントを含まない DOM ヘルパー等。例: `src/ui/dom-utils.js`）のみ
- [ ] `npm test` を実行して既存テストが全パスすること
- [ ] `npm run lint` がパスすること
- [ ] Chrome 最新版で手動テストを実施する（確認項目はテスト戦略の手動テスト確認項目を参照）
