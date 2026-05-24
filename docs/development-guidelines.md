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
| 定数（モジュールスコープ） | UPPER_SNAKE_CASE | `STAB_MULTIPLIER`, `MAX_PARTY_SIZE` |
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
- 最大行長: 規約上の目安は **100 文字**。ただし `.prettierrc` 等の設定ファイルは持たず Prettier のデフォルト `printWidth: 80` を使用しているため、`npm run format` 実行時は 80 文字基準で折り返される。JSDoc・長文コメントは 100 文字までの逸脱を許容する
- 文字列: **シングルクォート**
- セミコロン: **あり**

lint / format はコミットフックでは走らない（lint-staged / husky 等は未設定）。コミット前に必要に応じて手動で実行する:

```bash
npm run format   # Prettier
npm run lint     # ESLint
```

---

### 制御構文のブロック（波括弧）

`if` / `else` / `for` / `while` / `foreach` などの制御構文の本体は、**一行でも必ず波括弧 `{}` で囲む**（JavaScript・C# 共通）。後から行を追加して複数行化したときの波括弧の書き忘れ・字下げの誤認によるバグを防ぐため。

```js
// ✅ 良い例
if (isStab) {
  power *= STAB_MULTIPLIER;
}

// ❌ 悪い例: 波括弧なし（行を足すと壊れやすい）
if (isStab) power *= STAB_MULTIPLIER;
```

（JavaScript は ESLint の `curly` ルールで機械的に強制できる。）

---

### 変数宣言（JavaScript）

`const` を既定とし、再代入が必要な場合のみ `let` を使う。`var` は使わない（関数スコープ・巻き上げの落とし穴を避けるため）。

---

### マジックナンバー・マジックストリングと定数管理

**意味を持つ数値・文字列リテラルの直書きは原則禁止**（マジックナンバー／マジックストリング）。名前付き定数に切り出して意味を表す。ただし**意味・根拠・変更理由がコードから読み取れる場合は例外**として許容する。具体的には:

- **数値（例外＝定数化不要）**: 変数名・文脈・近傍コメントで自明なケース、および `0` / `1` / `-1` / `2` のような慣用値（ループ初期値・先頭アクセス・符号反転・「半分」など）。ただし**ドメインの意味を持つ数値は慣用値であっても名前付き定数を推奨**する（例: 能力ポイント無投資を表す `0` → `ZERO_ABILITY_POINTS`）。
- **文字列（例外＝定数化不要）**: 一度しか使わず意味が自明なもの（ユーザー向けメッセージ・ログ文言など）、および**1 箇所でのみ使う**外部データの**フィールドキー**（`entry['num']` のような Showdown / PokéAPI の JSON キー名アクセス。スキーマをそのまま写したもの）。
- **定数化する文字列**: **ロジックの比較・分岐に使う意味を持つ文字列**（ドメインの区分値・条件キーなど）や**複数箇所に現れる文字列**は、タイプミスがコンパイル/実行時に検出されず表記揺れの温床になるため、名前付き定数や列挙的なグループ（JS: `Set` / 凍結オブジェクト、C#: `enum` / 定数クラス）にまとめる。外部スキーマ由来の文字列でも、**値を分岐・比較に使う区分**（例: `move.category` の `'Physical'`）はこちらの対象。さらに**フィールドキーであっても、同じキーが複数箇所で繰り返し現れる場合や、書き込み（出力 JSON）と読み込みで同じキーを共有する場合**は、片側だけ変えたときに静かに壊れる（表記揺れ・キー名ドリフト）ため、定数クラスに一元化する（例: `ShowdownFetcher` の `num` / `name` / `baseStats` などを `ShowdownKey` にまとめる）。1 箇所限りのキーアクセスは前項の例外のまま。

```js
// ❌ 悪い例: 1.5 が何を表すかコードから読めない
return basePower * attackStat * (isStab ? 1.5 : 1.0);

// ✅ 良い例: 名前付き定数で意味を表す
const STAB_MULTIPLIER = 1.5; // タイプ一致補正（Pokémon Champions）
return basePower * attackStat * (isStab ? STAB_MULTIPLIER : 1.0);

// ❌ 悪い例: 区分を表す文字列の直書き（タイプミスが検出されず、表記揺れの温床）
if (move.category === 'Physical') { /* ... */ }

// ✅ 良い例: ドメインの区分値を定数（または凍結オブジェクト）にまとめる
const MOVE_CATEGORY = Object.freeze({ PHYSICAL: 'Physical', SPECIAL: 'Special', STATUS: 'Status' });
if (move.category === MOVE_CATEGORY.PHYSICAL) { /* ... */ }

// ✅ 許容例: 意味が文脈から自明なため定数化は不要
const [first] = entries;            // 0 番目を取る
for (let i = 0; i < n; i++) { }     // ループの初期値・増分
const num = entry['num'];           // 外部 JSON のフィールドキー（1 箇所限り・スキーマそのまま）
throw new Error('party.json が見つかりません'); // 一度きりのユーザー向けメッセージ
```

```csharp
// C#: 取りうる値が閉じているなら enum、文字列キーが必要なら定数にまとめる
private const string LangJa = "ja"; // PokéAPI の日本語ロケールコード
if (lang == LangJa) { /* ... */ }
```

なお既存コードで本規約に未準拠の箇所（`'Physical'` 等の区分値の直書きなど）は、`/fix-code` 等で段階的に準拠させる。

**定数の管理方法**（増えた定数を散らかさないための置き場所のルール）:

- **スコープは最小にする**。定数はそれを使う場所に最も近いスコープへ置く。
  - **1 ファイル内でのみ使う** → そのファイル先頭のモジュールスコープ定数（JS: `const`）／クラスの定数メンバー（C#: `private const` / `private static readonly`）にまとめる。例: `PokeAPIFetcher` の `ConcurrencyLimit`、`src/logic/resolve-modifier.js` の `POWER_MAX_60_THRESHOLD` 等。
  - **1 関数内でしか使わず意味も局所的** → その関数内の `const` でよい（無理にファイル先頭へ引き上げない）。
- **共有は「実際に複数箇所で使われている」場合のみ**。「将来使うかもしれない」という理由で共通定数モジュールへ先に集約しない（早すぎる共通化を避ける）。本当に複数ファイルで共有する値が生じたときに初めて、専用モジュール（JS: 例 `src/logic/constants.js`、C#: 専用の `internal static class` の定数置き場）へ切り出す。
- **ドメインの計算式定数**（種族値オフセット・性格補正・タイプ一致倍率など）は、その計算を担うロジックの近傍に置き、用語は [用語集](./glossary.md) と対応させる。値の出典・式の根拠は WHY コメントで補う（名前だけでは「なぜその値か」までは伝わらないため）。
- **命名は「命名規則」に従う**（JS モジュール定数: `UPPER_SNAKE_CASE` / C# 定数・`static readonly`: `PascalCase`）。関連する定数は意味のまとまりで近接して並べ、必要なら 1 つのオブジェクト（JS）や定数群（C#）としてグループ化する。区分値の集合は凍結オブジェクト（JS: `Object.freeze`）／`enum`・定数クラス（C#）でまとめると、取りうる値が一覧でき表記揺れも防げる。

> 目的は「数値・文字列の意味を明示すること」であって「定数の数を増やすこと」ではない。名前付き定数にしても意味が増えない（上記の例外に該当する）場合は、リテラルのままでよい。

---

### コメント規約

**コメントは WHY のみ書く**。コードを読めばわかる WHAT は書かない。

**コメントは日本語で書く**。ただし日本語に訳しにくい用語や、英語のほうが正確・明快な場合（技術用語・API 名・ドメイン用語など）は英語のままでよい。

```js
// ✅ 良い例: なぜそうするかを説明
// 威力不定技（カウンター・じわれ等）はパッチ未定義のため火力指数計算の対象外
if (move.power === null) {
  return null;
}

// ❌ 悪い例: コードを読めばわかる
// power が null なら null を返す
if (move.power === null) {
  return null;
}
```

**ドキュメンテーションコメント**: 公開 API には、その言語の標準的なドキュメンテーションコメントを付ける。

- **JavaScript**: `export` する関数・モジュールの公開インターフェースに **JSDoc**（`/** ... */`、`@param` / `@returns` 等）を付ける（例: `src/data/loader.js` のエクスポート関数）。
- **C#**: `public` / `internal` で外部（他クラス・テスト）から使う型・メンバーに **XML Documentation Comments** を付ける。含めるタグは以下に従う。

  | タグ | 要否 |
  |------|------|
  | `<summary>` | 必須 |
  | `<remarks>` | 必須 |
  | `<param>` | 引数が存在すれば必須（各引数に 1 つ）。引数がなければ記載不要 |
  | `<returns>` | 戻り値が存在すれば（`void` / コンストラクタ以外）必須。なければ記載不要 |
  | `<exception>` | 例外を送出しうる場合は必須（送出する例外型ごとに記載）。それ以外は記載不要 |

  ```csharp
  /// <summary>ファイルの SHA-256 ハッシュを小文字 16 進文字列で返す。</summary>
  /// <remarks>差分検知に用いる。ファイルが存在しない場合は空文字列を返し、例外にはしない。</remarks>
  /// <param name="filePath">ハッシュ対象のファイルパス。</param>
  /// <returns>小文字 16 進のハッシュ文字列。ファイルが無ければ空文字列。</returns>
  internal static string ComputeHash(string filePath)
  ```

非 export のヘルパーや `private` メンバー、関数名と引数名だけで意図が伝わる単純なロジック関数には不要。WHAT の繰り返しになる冗長なドキュメンテーションコメントは書かない。

ただし private / 非 export の関数でも、理由が非自明な場合は WHY コメント（ドキュメンテーションコメントとは別）を付けてよい。コメントを付けるかは可視性ではなく「名前・引数・コードだけで意図が伝わるか」で判断する。

---

### 関数設計

**純粋関数を優先する**（`src/logic/` 配下は必ず純粋関数）:

```js
// ✅ 良い例: 副作用なし、同じ入力→同じ出力（Pokémon Champions 計算式）
// 能力ポイント上限・性格補正倍率は calc-actual-stats.js でドメイン定数として一元定義し、
// speed-calc.js / endurance-index-calc.js から共通で import する（二重定義を避ける）
import {
  calcStat,
  MAX_ABILITY_POINTS,
  NATURE_UP,
  NATURE_NEUTRAL,
  NATURE_DOWN,
} from './calc-actual-stats.js';

export function calcSpeedPatterns(baseSpe) {
  return {
    fastest: calcStat(baseSpe, MAX_ABILITY_POINTS, NATURE_UP),
    fast:    calcStat(baseSpe, MAX_ABILITY_POINTS, NATURE_NEUTRAL),
    neutral: calcStat(baseSpe, 0, NATURE_NEUTRAL),
    slowest: calcStat(baseSpe, 0, NATURE_DOWN),
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
  // null を返すのはコントラクトの一部（変化技・威力不定技）であり、エラーではない
  if (move.power === null) return null;
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
- **C#**: 既定は `internal`（このリポジトリの `tools/` はライブラリ公開しない実行アプリのため `public` にする必要はない）。**純粋ロジック（文字列・JSON 変換など副作用のない計算）は、テストのために `internal` 公開する前に責務を持つ協力クラスへ切り出す**ことを優先する（切り出した型を単体テストすれば、可視性はテスト都合ではなく役割で正当化される。例: `PokeAPIFetcher` の slug 変換・名前抽出を `PokeApiSlug` / `PokeApiName` へ抽出）。`[assembly: InternalsVisibleTo("PokelensTools.Tests")]` で内部型を見せるのは、**抽出が適さないケース**――HTTP など副作用を持つオーケストレーションを依存注入（偽 `HttpClient` 等）でふるまいテストする場合や、型ごと内部に閉じたまま検証したい場合――に限る。いずれも `public` 化はしない。
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

**namespace はフォルダ構造に揃える**: `tools/PokelensTools/<Folder>/` 配下のファイルは `namespace PokelensTools.<Folder>;`（例: `Common/` → `PokelensTools.Common`、`Fetchers/` → `PokelensTools.Fetchers`、`Pipeline/` → `PokelensTools.Pipeline`、`Models/` → `PokelensTools.Models`）。**ルート直下のファイル**（`Program.cs` / `AssemblyInfo.cs`）は namespace 宣言不要（`Program.cs` は top-level statements、`AssemblyInfo.cs` は assembly 属性のみで、いずれも namespace に属する型を宣言しない）。テストプロジェクトは `PokelensTools.Tests`（プロジェクト単位）に集約しフォルダ分割しない。.NET 標準慣習に従うことで IDE の新規ファイル生成テンプレートとも整合する。

**ファイルパス引数を library 関数に渡さない**: `Common/DataPaths.cs` がリポジトリレイアウト由来のファイルパスを一元提供する。Library 関数（Fetcher / Pipeline / IncrementalRunner 等）は**path 引数を取らず**、内部で `DataPaths.Cache.X()` / `DataPaths.Master.X()` / `DataPaths.Patch.X()` を直接参照する。テストは `using var _ = DataPaths.OverrideRepoRoot(tempDir);` で scope ごとに RepoRoot を redirect することで temp dir に隔離する（AsyncLocal ベースなので並列テストでも互いに干渉しない）。例外は `IncrementalRunner.ComputeHash(string filePath)` のような**汎用ユーティリティ**（任意のファイルパスをデータとして受け取る・DataPaths への依存がない）で、これは引数を保持する。「library 関数を parameterless に保つ」原則と「テスト隔離」を両立する設計。

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
| UI 結合・画面表示・サジェスト・火力指数の UI 反映 | E2E（Playwright） | `tests/e2e/*.spec.js` |
| C# データ準備パイプラインの結合 | 統合（C#） | `tools/PokelensTools.Tests/PipelineIntegrationTests.cs` |
| 環境セットアップ・C# データ生成・dev サーバー起動 | 手動 E2E | [`docs/testing/e2e/manual-test-cases.md`](./testing/e2e/manual-test-cases.md) |

### E2E テスト方針（Playwright）

UI コンポーネント（`src/ui/`）の結合動作は Playwright で自動化する。テストは Chromium 上で実ブラウザを起動し、Vite dev サーバーに接続する（`playwright.config.js` の `webServer` で自動起動）。

**データ注入パターン**:
- 各テストの `beforeEach` で `tests/e2e/helpers/mock-party.js` の `mockParty(page, fixture)` を呼び、`page.goto('/')` する（直接 `page.route` を書かない）
- マスターデータ（pokedex / moves / items / abilities / types / move-categories / natures）は実ファイルを使用する（mock しない）
- アプリ本体（`src/`）は無改修。テストモード切替や DI は持ち込まない

**規約**:
- セレクタは `tests/e2e/helpers/selectors.js` の `SEL` 定数を介して参照する（DOM 構造変更を一箇所に閉じる）
- party fixture は `tests/e2e/helpers/party-fixtures.js` にエクスポート（再利用前提）
- 異常系 fixture は `mockPartyInvalidJson()` / `partyMissingField()` のヘルパーを使う（mock-party.js / party-fixtures.js 提供）
- カードのクリックは `element.click({ force: true })` を使う（CSS `transition: border-color` が Playwright の stability check と衝突するため）
- 並列実行を前提とし、テスト間で共有状態を持たない（`page.route` はテストスコープ）

**実行**:
```bash
npm run test:e2e          # 通常実行（CLI）
npm run test:e2e:ui       # Playwright UI モード
npm run test:e2e:headed   # ヘッド付きブラウザで実行（デバッグ用）
```

**初回セットアップ**: `npx playwright install chromium`（〜100MB のバイナリ取得）

### テストの書き方（Given-When-Then）

```js
import { describe, it, expect } from 'vitest';
import { calcSpeedPatterns } from '../../src/logic/speed-calc.js';
import { calcPowerIndex } from '../../src/logic/power-index-calc.js';

describe('calcSpeedPatterns', () => {
  it('種族値90のポケモンで素早さ4パターンを正しく計算する', () => {
    // Given
    const baseSpe = 90;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    // floor((90 + 能力ポイント + 20) × 性格補正)
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
- `src/ui/` 配下: 単体テスト対象外。UI 結合は Playwright E2E（`tests/e2e/`）で担保する

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
| `ui` | `src/ui/` および `tests/e2e/`（UI 結合 E2E テスト） |
| `logic` | `src/logic/` および対応する `tests/unit/` |
| `data` | `src/data/` および対応する `tests/unit/loader.test.js` |
| `tools` | `tools/PokelensTools/` および `tools/PokelensTools.Tests/` |
| `docs` | `docs/` 配下（テスト仕様書 `docs/testing/` を含む） |
| `config` | `vite.config.js` / `vitest.config.js` / `playwright.config.js` / `eslint.config.js` / `.gitignore` / `package.json` 等 |

**例**:
```
feat(ui): 相手ポケモン詳細パネルを追加
fix(logic): 素早さ計算のfloorの位置を修正
test(logic): 火力指数の変化技ケースを追加
test(ui): E2E に opponent search のキーボード操作ケースを追加
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

# 2. （E2E テストを実行する場合）Playwright Chromium バイナリ取得（〜100MB、初回のみ）
npx playwright install chromium

# 3. C# ツールでマスターデータを生成
dotnet run --project tools/PokelensTools

# 4. data/party.json を自分のパーティで編集

# 5. 開発サーバー起動（file:// ではなく必ず Vite 経由で開く）
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
| `/setup-docs` | 承認済み PRD から派生5ドキュメント（機能設計・アーキテクチャ・リポジトリ構造・開発ガイドライン・用語集）をまとめて生成 | 生成する | PRD 確定後（初回生成。再実行で全書き換え） |
| `/update-docs ["<意図文>" or <機能名>]` | **docs を設計の単一真実源として扱うコマンド**。git 状態で 2 モードを自動切替: **Phase 1（設計記述・pre-impl）** = 機能の意図文を受け、PRD・機能設計・アーキテクチャ・リポジトリ構造・用語集・開発ガイドラインへの設計記述をカテゴリ別承認で進める。**Phase 3（事後反映・post-impl）** = 実装完了後の PRD チェックボックス更新・テスト一覧反映 | 更新する（承認後） | **`/add-feature` の前（Phase 1）** と **`/add-feature` の後（Phase 3）** |
| `/add-feature <機能名>` | **docs を仕様として実装する実装専念モード**。設計は `/update-docs` (Phase 1) で固定済みの前提。`design.md` は実装計画に縮退し、新たな設計判断はしない。docs に未記載の判断が必要になったらルールD で中断 → ユーザーが `/update-docs phase1:` で docs を補ってから再開 | 実装する | docs で設計が固まった機能ごと |
| `/fix-code <修正内容>` | 既存ソースの修正を全自動実行（実装 → テスト追加/更新 → test/lint 通過 → `implementation-validator` 検証 → `docs/testing/` 一覧反映）。永続ドキュメント波及がある修正後は `/update-docs` を案内 | 修正する | バグ修正・リファクタ・指摘反映 |
| `/write-e2e-cases [<機能>]` | PRD の受け入れ条件から手動・E2E テスト仕様書（`e2e/manual-test-cases.md`）に未カバー条件を追加（既存ケースの改訂・削除は手動） | 生成する | E2E フェーズ冒頭（全機能対象）／受け入れ条件の追加時 |
| `/review-doc <パス>` | 永続ドキュメントを品質レビュー | 指摘のみ | ドキュメント変更時 |
| `/review-code [<パス>]` | コードを8観点でレビュー（引数省略で `git diff HEAD`） | 指摘のみ | 実装後 |
| `/review-test-cases <パス>` | テストケース仕様書を「設計技法 × テスト観点」の2軸でレビュー | 指摘のみ | テスト仕様の更新時 |
| `/review-claude-assets [<パス>]` | `.claude/` の作成物（コマンド・サブエージェント・スキル）を定義特有の観点でレビュー（引数省略で `.claude/` の `git diff`） | 指摘のみ | コマンド・エージェント・スキルの追加/変更時 |
| `/suggest-commit-message` | `git diff HEAD` からコミットメッセージ案を生成し `.claude/tmp/` に保存 | commit しない（案を `.claude/tmp/` に保存） | コミット前 |
| `/suggest-pr [<ベース>]` | ブランチとベースの差分から PR の title・description 案を生成し `.claude/tmp/` に保存 | PR を作らない（案を `.claude/tmp/` に保存） | PR 作成前 |

> `setup-project` / `setup-docs` / `add-feature` / `fix-code` / `update-docs` / `write-e2e-cases` は **手を動かす**（生成・実装・修正・ドキュメント更新する）コマンド。`review-*` / `suggest-commit-message` / `suggest-pr` は **判断を助ける**（提案・指摘が主体で、ソース・ドキュメントは変更しない）コマンド。`review-*` はファイルを一切変更せず、`/suggest-commit-message`・`/suggest-pr` は案を `.claude/tmp/`（一時領域）に保存するのみで `git commit` / `gh pr create` はしない。`/review-code` の指摘の反映は `/fix-code` に渡すか、手動で行う。`/update-docs` は手を動かすコマンドだが **カテゴリ別にユーザー承認を得てから適用** する点で他の自動実行系（`/add-feature` 等）と異なる。

### 機能開発の標準フロー

1機能は `PN/feature/<機能>` ブランチで進める（→「Git 運用 / ブランチ戦略」）。

```
1. ブランチ作成        git switch -c P0/feature/<機能名>
2. 設計記述           /update-docs "<機能の意図文>"
                      └ Phase 1（pre-impl）: PRD・functional-design・architecture・
                        repository-structure・glossary・development-guidelines への
                        設計反映をカテゴリ別ユーザー承認で進める
                      └ docs が固まった時点で「設計の単一真実源」として確定
3. 実装               /add-feature <機能名>
                      └ docs を仕様として読んで実装計画（steering の tasklist）に分解
                      └ 実装ループ → test/lint → implementation-validator まで自動
                      ※ docs に書かれていない判断が出たら **ルールD で中断**して 2 に戻る
4. 事後反映           /update-docs
                      └ Phase 3（post-impl）: src/・tests/ の git 差分を検出して
                        PRD チェックボックス更新・テストケース一覧反映・docs 細部追従
5. コードレビュー      /review-code
                      └ 実装結果を人の目で点検し、指摘は手で修正
6. テスト仕様の点検    /review-test-cases <パス>（/update-docs が反映した一覧を点検）
7. コミット            /suggest-commit-message → git commit -F .claude/tmp/commit-msg-1.txt
8. PN 統合へマージ     P0/feature/<機能名> → P0/release
```

- **docs が設計の単一真実源** という原則を守るため、設計記述（2）を実装（3）より前に置く。`/add-feature` は docs に従って実装するだけで、新たな設計判断はしない。
- `/add-feature` の **ルールD**: 実装中に docs 未記載の判断が必要になったら自己判断せず中断。ユーザーが `/update-docs phase1:"<追加意図>"` で docs を補い、`/add-feature` を再開する。
- 5〜6 は「自動実装の成果物を点検・補強する」工程に位置づく。
- PN の全機能が `PN/release` に揃ったら、次の「E2E フェーズ」へ進む。
- **ブランチはコミット直前でもよい**: `/add-feature` はコミットせず変更を未コミットで残すため、`main` 上で実装してしまっても `git switch -c P<N>/feature/<機能名>` で変更ごと新ブランチへ移せる（`main` はクリーンのまま・手戻りなし）。`/add-feature` は完了時に `main` 上だと同趣旨の注意を表示する。

### E2E フェーズ（PN 統合後）

PN の全機能ブランチが `PN/release` にマージされたら、`PN/release` 上で手動・E2E テストを実施する。

```
1. 仕様生成   /write-e2e-cases   … PRD 受け入れ条件から docs/testing/e2e/manual-test-cases.md を生成・更新
2. 仕様点検   /review-test-cases docs/testing/e2e/manual-test-cases.md
3. 実行       Chrome で手動・E2E テストを実施（docs/testing/e2e/ の手順に従う）
4. main へ    E2E 通過後、PN/release を main にマージ
```

### 修正（バグ修正・リファクタ・指摘反映）のフロー

バグ修正・リファクタ・`/review-code` 指摘の反映は `fix/*` ブランチで進める（→「Git 運用 / ブランチ戦略」）。

```
1. ブランチ作成      git switch -c fix/<対象>
2. 修正             /fix-code <修正内容>
                    └ 修正実装・テスト追加/更新・検証・test/lint を自動で通し、
                      変更した自動テストを docs/testing/ の一覧へ反映する
3. （任意）         /update-docs
   ドキュメント反映  └ /fix-code 完了時に「永続ドキュメント波及あり」と案内された場合のみ実行。
                      PRD・永続ドキュメント（functional-design / glossary / architecture /
                      repository-structure / development-guidelines）への反映を承認サイクルで進める
4. テスト仕様の点検  /review-test-cases <パス>（/fix-code が反映した一覧を点検）
5. コミット          /suggest-commit-message → git commit -F .claude/tmp/commit-msg-1.txt
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
- [ ] Playwright E2E（`npm run test:e2e`）で UI 結合を検証する。新規 UI セレクタを増やした場合は `tests/e2e/helpers/selectors.js` の `SEL` 定数を更新する
- [ ] Chrome 最新版で手動確認も実施する（確認項目は [`docs/testing/e2e/manual-test-cases.md`](./testing/e2e/manual-test-cases.md) を参照。自動 E2E でカバーしている範囲はスキップ可）
