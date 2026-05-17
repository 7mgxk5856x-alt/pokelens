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

### コメント規約

**コメントは WHY のみ書く**。コードを読めばわかる WHAT は書かない。

```js
// ✅ 良い例: なぜそうするかを説明
// Showdownデータは威力不定技の power が null のため最大値で代替する
const basePower = move.power ?? move.maxPower;

// ❌ 悪い例: コードを読めばわかる
// basePower を設定する
const basePower = move.power ?? move.maxPower;
```

JSDoc は公開インターフェース（loader.js のエクスポート関数）にのみ付ける。
ロジック関数は関数名と引数名で意図が伝わる場合はコメント不要。

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

### C# コーディング規約（tools/PokelensTools/）

.NET 8 標準スタイルに従う。

| 種別 | 規則 | 例 |
|------|------|-----|
| クラス・インターフェース | PascalCase | `ShowdownFetcher`, `MergeConverter` |
| メソッド | PascalCase | `FetchDataAsync()` |
| ローカル変数・引数 | camelCase | `pokemonList`, `baseStats` |
| 非同期メソッド | `Async` サフィックス | `FetchDataAsync()` |

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
- ポケモン名サジェストが表示・選択できること
- タブキーで6つの相手パーティ入力欄を順番に移動できること
- 入力中にタブキーでサジェスト候補を選択し、エンターキーで決定できること
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

個人プロジェクトのため `main` 直コミットを許容する。ただし機能追加・大きな変更は `feature/*` ブランチを推奨:

```
main
 └─ feature/p0-party-display
 └─ feature/p1-matchup
 └─ fix/speed-calc-floor
```

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
- [ ] 他の `src/ui/` ファイルを直接 import しない（コンポーネント間の連携はコンストラクタへのコールバック注入で行う。`main.js` がコールバックを生成して各コンポーネントに渡す）
- [ ] `src/logic/` と `src/data/` のみを import する
- [ ] `npm test` を実行して既存テストが全パスすること
- [ ] `npm run lint` がパスすること
- [ ] Chrome 最新版で手動テストを実施する（確認項目はテスト戦略の手動テスト確認項目を参照）
