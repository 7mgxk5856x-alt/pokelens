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
| JSファイル | kebab-case | `power-index.js`, `name-search.js` |
| テストファイル | `[対象].test.js` | `power-index.test.js` |
| C# クラスファイル | PascalCase | `Converter.cs` |

---

### コードフォーマット

- インデント: **2スペース**（Prettier が自動適用）
- 最大行長: **100文字**
- 文字列: **シングルクォート**
- セミコロン: **あり**

Prettier と ESLint が `git commit` 前に自動適用される。手動で実行する場合:

```bash
npm run format   # Prettier
npm run lint     # ESLint（--fix 付き）
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
// ✅ 良い例: 副作用なし、同じ入力→同じ出力
export function calcSpeedPatterns(baseSpe) {
  return {
    fastest: calcSpeed(baseSpe, 31, 252, 1.1),
    fast:    calcSpeed(baseSpe, 31, 252, 1.0),
    neutral: calcSpeed(baseSpe, 31,   0, 1.0),
    slowest: calcSpeed(baseSpe,  0,   0, 0.9),
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
export function calcPowerIndex(move, actualStats, pokemonTypes, abilityMod, itemMod) {
  if (move.category === 'status') return null;
  // null を返すのはコントラクトの一部（変化技）であり、エラーではない
  const basePower = move.power ?? move.maxPower;
  ...
}
```

**ユーザー向けエラー表示**: `src/ui/` 内で行う。エラーメッセージは具体的に（何が起きたか + 対処法）。

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

## テスト戦略

### テスト対象と種別

| 対象 | 種別 | ファイル |
|------|------|---------|
| 火力指数計算 | ユニット | `tests/unit/power-index.test.js` |
| 素早さ計算 | ユニット | `tests/unit/speed-calc.test.js` |
| 名前検索・正規化 | ユニット | `tests/unit/name-search.test.js` |
| DataLoader → UI フロー | 統合 | `tests/integration/data-flow.test.js` |

UIコンポーネント（`src/ui/`）は手動ブラウザテストで確認する（E2Eテストは不要）。

### テストの書き方（Given-When-Then）

```js
import { describe, it, expect } from 'vitest';
import { calcSpeedPatterns } from '../../src/logic/speed-calc.js';

describe('calcSpeedPatterns', () => {
  it('種族値90のポケモンで素早さ4パターンを正しく計算する', () => {
    // Given
    const baseSpe = 90; // ピカチュウ

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    expect(result.fastest).toBe(167);  // floor((90*2+31+63)*50/100+5)*1.1
    expect(result.fast).toBe(152);
    expect(result.neutral).toBe(121);
    expect(result.slowest).toBe(103);
  });

  it('変化技はnullを返す', () => {
    const move = { name: 'まもる', category: 'status', power: null };
    expect(calcPowerIndex(move, {}, [], 1.0, 1.0)).toBeNull();
  });
});
```

### テスト命名

```
[対象関数]_[条件]_[期待結果]
```

例: `calcPowerIndex_statusMove_returnsNull`

### カバレッジ目標

- `src/logic/` 配下: **80% 以上**
- `src/data/` 配下: 主要パスのみ
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

**例**:
```
feat(ui): 相手ポケモン詳細パネルを追加
fix(logic): 素早さ計算のfloorの位置を修正
test(logic): 火力指数の変化技ケースを追加
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
dotnet run --project tools/ShowdownFetcher

# 3. data/party.json を自分のパーティで編集

# 4. 開発サーバー起動（file:// ではなく必ず Vite 経由で開く）
npm run dev
```

### よく使うコマンド

```bash
npm test              # テスト実行
npm run test:watch    # ウォッチモード
npm run test:coverage # カバレッジ計測
npm run lint          # ESLint
npm run format        # Prettier
```

---

## 実装チェックリスト

新しいロジック関数を追加するとき:

- [ ] `src/logic/` に純粋関数として実装
- [ ] 対応するユニットテストを `tests/unit/` に追加
- [ ] 正常系・変化技・威力不定技などのエッジケースをテスト
- [ ] `npm test` と `npm run lint` がパスすること
- [ ] innerHTML を使っていないこと
