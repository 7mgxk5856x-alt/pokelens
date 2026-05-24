# 単体テストケース一覧（自動テスト）

pokelens で実装済みの**自動・単体テスト**を、テストファイル・関数単位でまとめたドキュメント。各ロジックがどの観点・入力・期待結果で検証されているかを俯瞰するための参照資料。フロントエンド（Vitest）と C# データ準備ツール（xUnit）の双方を対象とする。

> 統合テストは [../integration/automated-test-cases.md](../integration/automated-test-cases.md) に分離。手動（E2E）テストの仕様書は [../e2e/manual-test-cases.md](../e2e/manual-test-cases.md) を参照。

**フロントエンド**

- **テストフレームワーク**: Vitest（`environment: node`）
- **対象**: `tests/unit/**/*.test.js`
- **実行コマンド**: `npm test`（単一ファイルは `npx vitest run tests/unit/<file>.test.js`）

**C# データ準備ツール**

- **テストフレームワーク**: xUnit（.NET 8）
- **対象**: `tools/PokelensTools.Tests/*.cs`（`PipelineIntegrationTests.cs` を除く）
- **実行コマンド**: `dotnet test tools/PokelensTools.Tests`

> このドキュメントはテストコードから手動で抽出したスナップショットです。テスト追加・変更時は併せて更新してください。

## 表の項目

| 項目 | 説明 |
|---|---|
| ケースID | 一意な識別子。`<ファイル接頭辞>-<連番>` 形式 |
| 種別 | 正常系 / 異常系 / 境界値 / 組合せ / 純粋関数 のいずれか（複合する場合は併記） |
| テスト観点 | 何を確認したいか |
| 入力値 | 実際に渡す引数 |
| 期待結果 | 正しい出力・挙動 |
| 備考 | 補足・計算根拠など |

**ファイル接頭辞（フロントエンド）**: CAS=calc-actual-stats / PIC=power-index-calc / SPD=speed-calc / EIC=endurance-index-calc / RMOD=resolve-modifier / NS=name-search / LD=loader

**ファイル接頭辞（C#）**: IRT=IncrementalRunnerTests / MCT=MergeConverterTests / PAT=PatchApplicatorTests / PFT=PokeAPIFetcherTests / PNM=PokeApiNameTests / PSL=PokeApiSlugTests / SFT=ShowdownFetcherTests

## サマリ

### フロントエンド (Vitest)

| テストファイル | テスト対象 | ケース数 |
|---|---|---:|
| [calc-actual-stats.test.js](#calc-actual-statstestjs) | `src/logic/calc-actual-stats.js` | 11 |
| [power-index-calc.test.js](#power-index-calctestjs) | `src/logic/power-index-calc.js` | 11 |
| [speed-calc.test.js](#speed-calctestjs) | `src/logic/speed-calc.js` | 10 |
| [endurance-index-calc.test.js](#endurance-index-calctestjs) | `src/logic/endurance-index-calc.js` | 8 |
| [resolve-modifier.test.js](#resolve-modifiertestjs) | `src/logic/resolve-modifier.js` | 43 |
| [name-search.test.js](#name-searchtestjs) | `src/logic/name-search.js` | 33 |
| [loader.test.js](#loadertestjs) | `src/data/loader.js` | 38 |
| **小計** | | **154** |

### C# データ準備ツール (xUnit)

| テストファイル | テスト対象 | ケース数 |
|---|---|---:|
| [IncrementalRunnerTests.cs](#incrementalrunnertestscs) | `IncrementalRunner` | 15 |
| [MergeConverterTests.cs](#mergeconvertertestscs) | `MergeConverter` | 23 |
| [PatchApplicatorTests.cs](#patchapplicatortestscs) | `PatchApplicator` | 8 |
| [PokeAPIFetcherTests.cs](#pokeapifetchertestscs) | `PokeAPIFetcher` | 5 |
| [PokeApiNameTests.cs](#pokeapinametestscs) | `PokeApiName` | 9 |
| [PokeApiSlugTests.cs](#pokeapislugtestscs) | `PokeApiSlug` | 19 |
| [ShowdownFetcherTests.cs](#showdownfetchertestscs) | `ShowdownFetcher` | 15 |
| **小計** | | **94** |

**単体テスト総合計: 248 ケース**（統合テスト 8 件は [../integration/automated-test-cases.md](../integration/automated-test-cases.md) を参照）

---

## calc-actual-stats.test.js

**テスト対象**: `src/logic/calc-actual-stats.js`（実数値計算）
**共通データ**: `GARCHOMP = { hp:108, atk:130, def:95, spa:80, spd:85, spe:102 }`

### `calcHp(base, abilityPoints)`

> HP の実数値を Champions 計算式 `base + abilityPoints + 75` で算出する。
>
> - `base`（number）: HP の種族値
> - `abilityPoints`（number）: HP の能力ポイント（0〜32）
> - 戻り値（number）: HP 実数値

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| CAS-001 | 境界値 | HP実数値の計算式（能力ポイント最大値 ap=32） | `calcHp(108, 32)` | `215` | base + ap + 75。定義域 0〜32 の上限 |
| CAS-001b | 境界値 | HP実数値の計算式（能力ポイント最小値 ap=0） | `calcHp(108, 0)` | `183` | base + 0 + 75。定義域 0〜32 の下限 |

### `calcStat(base, abilityPoints, natureModifier)`

> HP 以外の実数値を `floor((base + abilityPoints + 20) × natureModifier)` で算出する。
>
> - `base`（number）: 対象ステータスの種族値
> - `abilityPoints`（number）: 対象ステータスの能力ポイント（0〜32）
> - `natureModifier`（number）: 性格補正倍率（上昇 1.1 / 等倍 1.0 / 下降 0.9）
> - 戻り値（number）: 実数値

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| CAS-002 | 境界値 | 上昇補正（nature=1.1）・能力ポイント最大値 (ap=32) | `calcStat(130,32,1.1)` | `200` | floor((130+32+20)×1.1)。ap 定義域の上限 |
| CAS-002b | 境界値 | 等倍補正（nature=1.0）・能力ポイント最小値 (ap=0) | `calcStat(102,0,1.0)` | `122` | (102+0+20)×1.0。ap 定義域の下限 |
| CAS-002c | 境界値 | 下降補正（nature=0.9）・能力ポイント最小値 (ap=0) | `calcStat(80,0,0.9)` | `90` | floor((80+0+20)×0.9)。ap 定義域の下限 |
| CAS-003 | 境界値 | 等倍補正・能力ポイント最大値 (ap=32) | `calcStat(100,32,1.0)` | `152` | CAS-002b（ap=0）との対。ap 定義域の上限 |
| CAS-004 | 境界値 | 下降補正の floor 端数切り捨て | `calcStat(85,0,0.9)` | `94` | floor((85+20)×0.9)=floor(94.5) |

### `calcActualStats(baseStats, abilityPoints, natureModifiers)`

> 種族値・能力ポイント・性格補正から6ステータスの実数値をまとめて算出する純粋関数。
>
> - `baseStats`（object: `{hp,atk,def,spa,spd,spe: number}`）: 種族値
> - `abilityPoints`（object: 同形）: 各ステータスの能力ポイント
> - `natureModifiers`（object: `{[stat]: number}` の部分指定）: 性格補正倍率。未指定のステータスは 1.0 扱い
> - 戻り値（object: `{hp,atk,def,spa,spd,spe: number}`）: 各ステータスの実数値

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| CAS-005 | 組合せ | 全6ステータスを一括算出 | base=`GARCHOMP`, ap=`{hp:32,atk:32,他0}`, nature=`{atk:1.1,spa:0.9}` | `{hp:215,atk:200,def:115,spa:90,spd:105,spe:122}` | ガブリアス+いじっぱり |
| CAS-006 | 正常系 | 空の性格補正を全1.0扱い | base=`GARCHOMP`, ap=全0, nature=`{}` | `{hp:183,atk:150,def:115,spa:100,spd:105,spe:122}` | がんばりや |
| CAS-007 | 境界値 | def 下降補正の floor | base=`{def:85,他100}`, ap=全0, nature=`{def:0.9}` | `def === 94` | floor((85+20)×0.9) |
| CAS-008 | 純粋関数 | 引数オブジェクトを破壊しない | base/ap/nature（CAS-005相当） | 呼び出し後も base/ap/nature が不変 | 副作用なし |

---

## power-index-calc.test.js

**テスト対象**: `src/logic/power-index-calc.js`（火力指数計算）
**共通データ**: `STATS = { hp:215, atk:200, def:115, spa:90, spd:105, spe:122 }` / `physical()={Ground,Physical,power:100}` / `special()={Electric,Special,power:90}`

### `calcPowerIndex(move, actualStats, pokemonTypes, abilityModifier, itemModifier)`

> 技の火力指数（威力 × 攻撃/特攻実数値 × STAB補正 × 特性補正 × 持ち物補正）を算出する純粋関数。変化技・威力不定技は `null` を返す。
>
> - `move`（object: `{type: string, category: 'Physical'|'Special'|'Status', power: number|null}`）: 技データ
> - `actualStats`（object: `{atk, spa, …: number}`）: 実数値。物理技は `atk`、特殊技は `spa` を使用
> - `pokemonTypes`（string[]）: ポケモンのタイプ配列。`move.type` を含めば STAB 1.5 倍
> - `abilityModifier`（number）: 特性による補正倍率（呼び出し元で条件評価済み）
> - `itemModifier`（number）: 持ち物による補正倍率（同上）
> - 戻り値（number | null）: 火力指数。変化技・威力不定技は `null`

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PIC-001 | 異常系 | 変化技は計算対象外 | move=`{Status,power:null}`, `['Normal']`,1.0,1.0 | `null` | Status は火力指数なし |
| PIC-002 | 異常系 | 威力不定技は計算対象外 | `physical({power:null})`, `['Ground']`,1.0,1.0 | `null` | power=null |
| PIC-003 | 正常系 | 物理技は atk を使用 | `physical()`, `[]`,1.0,1.0 | `20000`（=100×200×1.0） | STABなし |
| PIC-004 | 正常系 | 特殊技は spa を使用 | `special()`, `[]`,1.0,1.0 | `8100`（=90×90×1.0） | STABなし |
| PIC-005 | 正常系 | STABあり（タイプ一致）で1.5倍 | `physical()`, `['Ground','Dragon']`,1.0,1.0 | `30000`（=100×200×1.5） | タイプ一致補正 |
| PIC-006 | 正常系 | STABなし（不一致）で1.0倍 | `physical()`, `['Fire']`,1.0,1.0 | `20000` | 補正なし |
| PIC-007 | 組合せ | 物理技 × STAB × 特性 × 持ち物の三重補正 | `physical()`, `['Ground']`,1.3,1.2 | `46800`（=100×200×1.5×1.3×1.2、`toBeCloseTo`） | atk 参照 |
| PIC-007b | 組合せ | 特殊技 × STAB × 特性 × 持ち物の三重補正 | `special({type:'Fire'})`, `['Fire']`,1.3,1.2 | `≈18954`（=90×90×1.5×1.3×1.2、`toBeCloseTo`） | spa 参照 |
| PIC-009 | 正常系 | multihit 最大総威力（power=basePower×multihit[1]）の火力指数計算 | `{type:'Water',category:'Physical',power:75}`, STATS, `['Water']`, 1.0, 1.0 | `22500`（=75×200×1.5、`toBeCloseTo`） | PRD 機能5/6・計算層担保（MCT-009/010 は変換層） |
| PIC-010 | 正常系 | 威力不定技パッチ適用後の power 値で火力指数計算 | `{type:'Normal',category:'Physical',power:120}`, STATS, `['Normal']`, 1.0, 1.0 | `36000`（=120×200×1.5、`toBeCloseTo`） | PRD 機能2/6・LD-016 は取得のみ、PIC-010 は計算結果 |
| PIC-008 | 純粋関数 | 引数を破壊しない | `physical()`, stats, types, 1.2,1.1 | 呼び出し後も move/stats/types が不変 | 副作用なし |

---

## speed-calc.test.js

**テスト対象**: `src/logic/speed-calc.js`（素早さ4パターン計算）

### `calcSpeedPatterns(baseSpe)`

> 種族値から素早さ4パターン（最速/準速/無補正/最遅）を算出する。
>
> - `baseSpe`（number）: 素早さの種族値。正の整数のみ受け付け、それ以外（0・負数・NaN・小数・文字列）は `RangeError`
> - 戻り値（object: `{fastest, fast, neutral, slowest: number}`）: 4パターンの素早さ実数値
> - 機能 15（P0.5）で 6 パターン → 4 パターンへ簡素化。こだわりスカーフ補正後の値は機能 16 で自分側 UI に集約済み（倍率値はマスターデータ `data/items.json` の `こだわりスカーフ.modifier.spe` から取得）

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| SPD-001 | 正常系 | 種族値90で4パターン算出 | `90` | `{fastest:156, fast:142, neutral:110, slowest:99}` | 代表値 |
| SPD-002 | 境界値 | 種族値最小値で全 4 パターン算出 | `1` | `{fastest:58, fast:53, neutral:21, slowest:18}` | floor の連鎖が破綻しない |
| SPD-003 | 境界値 | 大きい値でも破綻しない | `200` | `fastest:floor(252×1.1)`, `neutral:220`, `slowest:floor(220×0.9)` 等 | 種族値大 |
| SPD-004 | 正常系 | 最遅は0.9補正の floor | `100` | `slowest === 108` | floor((100+20)×0.9) |
| SPD-005 | 正常系 | 戻り値にスカーフキー（fastestScarf/fastScarf）が含まれない | `90` | `Object.keys(result) === ['fastest','fast','neutral','slowest']` | 機能 15 での 4 パターン化を担保 |
| SPD-006 | 異常系 | ゼロ入力を拒否 | `0` | `RangeError` を throw（メッセージに `'baseSpe は正の整数'` を含む） | 不正入力 |
| SPD-007 | 異常系 | 負数入力を拒否 | `-1` | `RangeError` を throw（メッセージに `'baseSpe は正の整数'` を含む） | 不正入力 |
| SPD-008 | 異常系 | NaN入力を拒否 | `NaN` | `RangeError` を throw（メッセージに `'baseSpe は正の整数'` を含む） | 不正入力 |
| SPD-009 | 異常系 | 小数入力を拒否 | `90.5` | `RangeError` を throw（メッセージに `'baseSpe は正の整数'` を含む） | 非整数 |
| SPD-010 | 異常系 | 文字列入力を拒否 | `'90'` | `RangeError` を throw（メッセージに `'baseSpe は正の整数'` を含む） | 型違い |

---

## endurance-index-calc.test.js

**テスト対象**: `src/logic/endurance-index-calc.js`（耐久指数計算）

### `calcEnduranceIndex(hp, defStat)` / `calcEnduranceIndexPatterns(baseStats)`

> 耐久指数 = HP 実数値 × 防御/特防 実数値。`calc-actual-stats.js` の `calcHp` / `calcStat` を再利用し HP・防御の式を二重定義しない。
>
> - `calcEnduranceIndex(hp, defStat)`: 2 引数の積を返す（純粋関数）
> - `calcEnduranceIndexPatterns(baseStats)`: 種族値 `{hp, def, spd}` から 4 パターン × 2 種類 = 8 値を計算
> - 戻り値（object: `{specialized, defOnly, hpOnly, none}` × `{physical, special}`）: 4 パターン耐久指数
> - HP は性格補正の対象外（PRD 機能 17 「※ HP は性格補正の対象外」と整合）

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| EIC-001 | 正常系 | HP × 防御 を積で返す | `(215, 115)` | `24725` | ガブリアス物理耐久 H32 |
| EIC-002 | 正常系 | 特殊側（HP × 特防）も同じ式 | `(215, 105)` | `22575` | ガブリアス特殊耐久 H32 |
| EIC-003 | 境界値 | 境界値 1×1 でも破綻しない | `(1, 1)` | `1` | 最小値 |
| EIC-004 | 境界値 | 大きい値 999×999 | `(999, 999)` | `998001` | 整数オーバーフロー想定外確認 |
| EIC-005 | 正常系 | ガブリアス（H108/B95/D85）の 4 パターン × 2 種類 = 8 値を完全列挙 | `{hp:108, def:95, spd:85, ...}` | 特化 34615/32250、極振 29463/27450、H極振 24725/22575、無振り 21045/19215 | PRD 機能 17 の代表値 |
| EIC-006 | 正常系 | 戻り値は 4 パターンキー（specialized/defOnly/hpOnly/none）を持つ | `garchompBaseStats` | `Object.keys === ['specialized','defOnly','hpOnly','none']` | キー契約検証 |
| EIC-007 | 正常系 | 各パターンは physical / special の 2 キーを持つ | `garchompBaseStats` | 全パターンで `Object.keys === ['physical','special']` | サブキー契約検証 |
| EIC-008 | 境界値 | 種族値最小（HP=1, B=1, D=1）でも算出が破綻しない | `{hp:1, def:1, spd:1, ...}` | 特化 physical = 108×58 = 6264、無振り physical = 76×21 = 1596 | floor 連鎖の境界 |

---

## resolve-modifier.test.js

**テスト対象**: `src/logic/resolve-modifier.js`（特性・持ち物の補正条件解決）
**共通データ**: `physical()={Ground,Physical,power:100,tags:[]}` / `special()={Fire,Special,power:90,tags:[]}` / `status()={Normal,Status,power:null,tags:[]}`

### `resolveModifier(modifier, move, pokemonTypes, kind)`

> 特性・持ち物の補正定義（`condition` の種類）を解決し、火力計算に用いる倍率・タイプを返す純粋関数。modifier が null/undefined や未知 condition のときは無補正（1.0）にフォールバックする。
>
> - `modifier`（object | null | undefined: `{condition?: string, atk?: number, spa?: number, stab?: number, moveType?: string, convertedType?: string}`）: 補正定義
> - `move`（object: `{type: string, category: string, power: number|null, tags?: string[]}`）: 技データ
> - `pokemonTypes`（string[]）: ポケモンのタイプ配列（STAB 判定に使用）
> - `kind`（`'ability'` | `'item'`）: 補正の出所。`isStab` 時の挙動が分岐する
> - 戻り値（object: `{multiplier: number, typesForCalc: string[], moveTypeOverride?: string}`）: 補正倍率・計算用タイプ・技タイプ上書き（任意）

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| RMOD-001 | 異常系 | modifier=null のフォールバック | `(null, physical(), ['Ground'], 'ability')` | `{multiplier:1.0, typesForCalc:['Ground']}` | 補正なし時 |
| RMOD-002 | 異常系 | modifier=undefined のフォールバック | `(undefined, physical(), ['Fire'], 'item')` | `{multiplier:1.0, typesForCalc:['Fire']}` | 補正なし時 |
| RMOD-003 | 正常系 | 無条件補正・物理は atk | `({atk:1.3,spa:1.3}, physical(), ['Ground'], 'item')` | `{multiplier:1.3, typesForCalc:['Ground']}` | condition なし |
| RMOD-004 | 正常系 | 無条件補正・特殊は spa | `({atk:1.3,spa:1.3}, special(), ['Fire'], 'item')` | `{multiplier:1.3, typesForCalc:['Fire']}` | condition なし |
| RMOD-005 | 正常系 | 変化技は補正対象外 | `({atk:1.3,spa:1.3}, status(), [], 'item')` | `multiplier === 1.0` | Status |
| RMOD-006 | 境界値 | 対象ステータス未定義は1.0 | `({atk:1.5}, special(), ['Fire'], 'item')` | `multiplier === 1.0` | spa未定義 |
| RMOD-007 | 境界値 | 空modifier・物理で1.0 | `({}, physical(), ['Ground'], 'item')` | `multiplier === 1.0` | atk/spa無 |
| RMOD-008 | 境界値 | 空modifier・特殊で1.0 | `({}, special(), ['Fire'], 'item')` | `multiplier === 1.0` | atk/spa無 |
| RMOD-009 | 組合せ | isStab+ability・一致でSTAB置換 | `({stab:2.0,condition:'isStab'}, physical({Ground}), ['Ground'], 'ability')` | `{multiplier:2.0, typesForCalc:[]}` | typesForCalc を空にしSTAB倍率を置換 |
| RMOD-010 | 組合せ | isStab+ability・不一致 | `({stab:2.0,condition:'isStab'}, physical({Fire}), ['Ground'], 'ability')` | `{multiplier:1.0, typesForCalc:['Ground']}` | 不一致時 |
| RMOD-011 | 境界値 | stab未指定はデフォルト2.0 | `({condition:'isStab'}, physical(), ['Ground'], 'ability')` | `multiplier === 2.0` | デフォルト |
| RMOD-012 | 組合せ | isStab+item・一致でstat倍率 | `({atk:1.2,spa:1.2,condition:'isStab'}, physical({Ground}), ['Ground'], 'item')` | `{multiplier:1.2, typesForCalc:['Ground']}` | たつじんのおび等 |
| RMOD-013 | 組合せ | isStab+item・不一致 | `({atk:1.2,spa:1.2,condition:'isStab'}, physical({Fire}), ['Ground'], 'item')` | `{multiplier:1.0, typesForCalc:['Ground']}` | 不一致時 |
| RMOD-014 | 組合せ | isStab+item・特殊でspa一致 | `({atk:1.2,spa:1.2,condition:'isStab'}, special({Fire}), ['Fire'], 'item')` | `multiplier === 1.2` | 特殊技 |
| RMOD-015 | 組合せ | isType・moveType一致 | `({spa:1.5,condition:'isType',moveType:'Fire'}, special({Fire}), ['Fire'], 'ability')` | `multiplier === 1.5` | 特定タイプ補正 |
| RMOD-016 | 組合せ | isType・moveType不一致 | `(同上, special({Water}), ['Water'], 'ability')` | `multiplier === 1.0` | 不一致時 |
| RMOD-017 | 境界値 | isType・moveType未指定 | `({spa:1.5,condition:'isType'}, special({Fire}), ['Fire'], 'ability')` | `multiplier === 1.0` | 条件不備 |
| RMOD-018 | 組合せ | タグ条件 isPunch・該当あり | `({atk:1.2,condition:'isPunch'}, physical({tags:['isPunch','isContact']}), ['Ground'], 'ability')` | `multiplier === 1.2` | パンチ技 |
| RMOD-019 | 組合せ | タグ条件 isPunch・該当なし | `({atk:1.2,condition:'isPunch'}, physical({tags:['isContact']}), ['Ground'], 'ability')` | `multiplier === 1.0` | タグ不一致 |
| RMOD-020〜026 | 組合せ | 各タグ条件・該当ありで補正（`it.each` 7件） | `({atk:1.2,condition:<C>}, physical({tags:[<C>]}), ['Ground'], 'ability')`、`<C>` = isPulse/isBite/isRecoil/isSlice/isContact/isSound/hasSecondary | 各 `multiplier === 1.2` | 7タグを個別実行 |
| RMOD-027 | 組合せ | isContact・該当なし | `({atk:1.3,condition:'isContact'}, physical({tags:['isProtect']}), ['Ground'], 'ability')` | `multiplier === 1.0` | タグ不一致 |
| RMOD-028 | 組合せ | isSound・特殊技で spa 適用 | `({atk:1.3,spa:1.3,condition:'isSound'}, special({tags:['isSound']}), ['Normal'], 'ability')` | `multiplier === 1.3` | 音技×特殊 |
| RMOD-029 | 組合せ | hasSecondary・該当なし | `({atk:1.3,condition:'hasSecondary'}, physical({tags:[]}), ['Ground'], 'ability')` | `multiplier === 1.0` | タグ不一致 |
| RMOD-030 | 異常系 | tags未定義でも安全 | `({atk:1.2,condition:'isPunch'}, {Ground,Physical,power:80}（tags無）, ['Ground'], 'ability')` | `multiplier === 1.0` | 欠落時の安全性 |
| RMOD-031 | 正常系 | powerMax60・閾値内 | `({atk:1.5,condition:'powerMax60'}, physical({power:40}), ['Ground'], 'ability')` | `multiplier === 1.5` | 低威力補正 |
| RMOD-032 | 境界値 | powerMax60・境界値60 | `(同上, physical({power:60}), ...)` | `multiplier === 1.5` | 60を含む |
| RMOD-033 | 境界値 | powerMax60・閾値超過61 | `(同上, physical({power:61}), ...)` | `multiplier === 1.0` | 61は対象外 |
| RMOD-034 | 異常系 | powerMax60・威力不定 | `(同上, physical({power:null}), ...)` | `multiplier === 1.0` | power=null |
| RMOD-035 | 組合せ | convertNormalTo・Normal技を変換 | `({atk:1.2,spa:1.2,condition:'convertNormalTo',convertedType:'Fairy'}, physical({Normal}), ['Normal'], 'ability')` | `{multiplier:1.2, typesForCalc:['Normal'], moveTypeOverride:'Fairy'}` | スキン系 |
| RMOD-036 | 組合せ | convertNormalTo・Normal以外は非適用 | `(同上, physical({Ground}), ['Normal'], 'ability')` | `{multiplier:1.0, typesForCalc:['Normal']}`、override無 | 非対象技 |
| RMOD-037 | 境界値 | convertNormalTo・convertedType未指定 | `({atk:1.2,condition:'convertNormalTo'}, physical({Normal}), ['Normal'], 'ability')` | `{multiplier:1.0, typesForCalc:['Normal']}` | 条件不備 |
| RMOD-038 | 組合せ | convertNormalTo・特殊技でも変換 | `({...convertedType:'Fairy'}, special({Normal}), ['Normal'], 'ability')` | `multiplier===1.2`, `moveTypeOverride==='Fairy'` | 特殊技 |
| RMOD-039 | 組合せ | convertAllTo・全技を変換 | `({atk:1.2,spa:1.2,condition:'convertAllTo',convertedType:'Normal'}, physical({Ground}), ['Ground'], 'ability')` | `{multiplier:1.2, typesForCalc:['Ground'], moveTypeOverride:'Normal'}` | ノーマライズ系 |
| RMOD-040 | 組合せ | convertAllTo・Normal技も上書き | `(同上, physical({Normal}), ['Normal'], 'ability')` | `moveTypeOverride === 'Normal'` | 上書きの一貫性 |
| RMOD-041 | 境界値 | convertAllTo・convertedType未指定 | `({atk:1.2,condition:'convertAllTo'}, physical(), ['Ground'], 'ability')` | `{multiplier:1.0, typesForCalc:['Ground']}` | 条件不備 |
| RMOD-042 | 異常系 | 未知 condition は無視 | `({atk:1.5,condition:'isUnknownCondition'}, physical(), ['Ground'], 'ability')` | `{multiplier:1.0, typesForCalc:['Ground']}` | フォールバック |
| RMOD-043 | 純粋関数 | 引数を破壊しない | `({atk:1.3,spa:1.3,condition:'isStab'}, physical({Ground}), ['Ground','Dragon'], 'item')` | 呼び出し後も modifier/move/types が不変 | 副作用なし |

---

## name-search.test.js

**テスト対象**: `src/logic/name-search.js`（ポケモン名サジェスト検索）

### `normalizeQuery(query)`

> 入力文字列（ひらがな・半角カナ・ローマ字・それらの混在）を全角カタカナへ正規化する。半角の濁点/半濁点は合成し、ASCII記号はそのまま残す。
>
> - `query`（string）: 検索入力文字列
> - 戻り値（string）: 全角カタカナへ正規化した文字列

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| NS-001 | 正常系 | ひらがな→全角カタカナ | `'うーらおす'` | `'ウーラオス'` | かな種別の正規化 |
| NS-002 | 正常系 | 半角カナ→全角カナ | `'ｳｰﾗｵｽ'` | `'ウーラオス'` | 半角→全角 |
| NS-003 | 正常系 | 全角カナは冪等 | `'ガブリアス'` | `'ガブリアス'` | そのまま |
| NS-004 | 正常系 | 半角濁点の合成 | `'ｶﾞﾌﾞﾘｱｽ'` | `'ガブリアス'` | 濁点合成 |
| NS-005 | 正常系 | 半角半濁点の合成 | `'ﾊﾟﾝﾁ'` | `'パンチ'` | 半濁点合成 |
| NS-006 | 組合せ | ASCII記号は保持しかなのみ変換 | `'ウーラオス (れ'` | `'ウーラオス (レ'` | 非かな保持 |
| NS-007 | 境界値 | 空文字 | `''` | `''` | 空入力 |
| NS-008 | 組合せ | 半角/全角/ひらがな混在の一括変換 | `'ｶﾞブりあス'` | `'ガブリアス'` | 混在 |
| NS-009 | 正常系 | ローマ字 基本変換 | `'gabu'` | `'ガブ'` | ローマ字 |
| NS-010 | 正常系 | ローマ字 拗音 | `'kairyu'` | `'カイリュ'` | 拗音 |
| NS-011 | 正常系 | ローマ字 ヘボン式 | `'shi'` / `'chi'` / `'tsu'` | `'シ'` / `'チ'` / `'ツ'` | ヘボン式 |
| NS-012 | 正常系 | 大文字・混在も小文字扱い | `'GABU'` / `'Gabu'` | `'ガブ'` / `'ガブ'` | 大文字小文字 |
| NS-013 | 境界値 | 末尾の単独 n は撥音 | `'pan'` | `'パン'` | 末尾撥音 |
| NS-014 | 正常系 | nn は撥音 | `'minna'` / `'minn'` | `'ミンナ'` / `'ミン'` | 二重n |
| NS-015 | 組合せ | nn+拗音yは撥音+拗音 | `'nnya'` | `'ンニャ'` | 撥音＋拗音 |
| NS-016 | 正常系 | 促音（子音重ね） | `'kka'` / `'katta'` | `'ッカ'` / `'カッタ'` | 促音 |
| NS-017 | 境界値 | 半端な子音は途中まで変換 | `'gab'` / `'gabur'` | `'ガ'` / `'ガブ'` | 不完全入力 |
| NS-018 | 組合せ | ローマ字とカナの混在 | `'ガbu'` | `'ガブ'` | 混在 |
| NS-019 | 組合せ | n+y は撥音にせず拗音 | `'nyaa'` | `'ニャア'` | 拗音優先 |
| NS-020 | 正常系 | 外来音 | `'fairi'` / `'vi'` | `'ファイリ'` / `'ヴィ'` | fa/vi |
| NS-020a | 正常系 | ローマ字＋ハイフン→長音記号 | `'ri-'` / `'u-raosu'` | `'リー'` / `'ウーラオス'` | ローマ字入力時の長音補完 |
| NS-020b | 正常系 | かな＋ハイフン→長音記号 | `'ぴ-'` / `'ピ-'` | `'ピー'` / `'ピー'` | かな入力中の補完 |
| NS-020c | 正常系 | 全角ハイフン `－`→長音記号 | `'ri－'` | `'リー'` | NFKC で `-` 経由 |
| NS-020d | 回帰 | ハイフン無しローマ字は影響なし | `'gabu'` | `'ガブ'` | NS-009 の回帰確認 |
| NS-020e | 回帰 | 既存長音記号 `ー` は冪等 | `'ウーラオス'` | `'ウーラオス'` | 元入力に `ー` 含む場合の保持 |

### `searchByName(query, entries)`

> 正規化したクエリで `entries` を前方一致検索し、`num` 昇順・最大10件に絞って返す純粋関数。
>
> - `query`（string）: 検索入力（内部で `normalizeQuery` により正規化）
> - `entries`（object[]: `{num: number, name: string}[]`）: 検索対象の図鑑エントリ配列
> - 戻り値（object[]）: 前方一致したエントリ（num 昇順・最大10件）

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| NS-021 | 境界値 | 空文字は空配列 | `searchByName('', entries)` | `[]` | 空入力 |
| NS-022 | 正常系 | 前方一致を num 昇順で返す | `searchByName('ウー', entries)` | num=`[831,892,892]`、先頭 `'ウールー'` | ソート |
| NS-023 | 正常系 | ひらがな入力でカナにヒット | `searchByName('うー', entries)` | `['ウールー','ウーラオス','ウーラオス (れんげきのかた)']` | 正規化込み |
| NS-024 | 正常系 | 半角カナ入力でヒット | `searchByName('ｶﾞﾌﾞ', entries)` | `['ガブリアス']` | 半角カナ |
| NS-025 | 異常系 | 不一致は空配列 | `searchByName('ヌル', entries)` | `[]` | 0件 |
| NS-026 | 境界値 | 11件以上は10件にカット | 15件中 `searchByName('ウ', many)` | 件数`10`、num=`[1..10]` | 件数上限 |
| NS-027 | 純粋関数 | entries を破壊しない | `searchByName('ウ', entries)` | 呼び出し後も entries が不変 | 副作用なし |
| NS-028 | 正常系 | ローマ字＋ハイフン入力でヒット | `searchByName('ri-', entries+リーフィア)` | `['リーフィア']` | NS-020a の UI 結合（PRD 機能 3 受け入れ条件の代表例） |

**`searchByName` 共通データ**: `entries = [{892,'ウーラオス'}, {892,'ウーラオス (れんげきのかた)'}, {831,'ウールー'}, {445,'ガブリアス'}, {25,'ピカチュウ'}]`

---

## loader.test.js

**テスト対象**: `src/data/loader.js`（`DataLoader` クラス）。`fetch` をスタブし、サンプルデータ（ガブリアス/じしん/こだわりスカーフ/さめはだ/いじっぱり等）で検証。

**前提（スタブ機構）**: 各テストは `vi.stubGlobal('fetch', ...)` でリクエスト URL ごとにレスポンス（OK / 404 / 構文不正）を差し替える。`beforeEach` で `vi.unstubAllGlobals()` するため各ケースは独立したスタブを持ち、実行順依存はない。

### `DataLoader.load()`

> 全マスターデータ JSON（pokedex/moves/items/abilities/types/move-categories/natures/party）を `fetch` で読み込み、ファイル欠落・JSON構文不正・party必須フィールド欠落を検出して日本語メッセージで throw する。
>
> - 引数: なし
> - 戻り値（`Promise<object>`）: `{pokedex, moves, items, abilities, typeNames, moveCategories, natures, userParty}`。失敗時は `Error` を throw

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-001 | 正常系 | 全データを取得 | 全 JSON が正常に応答 | `data` の pokedex/moves/items/abilities/typeNames/moveCategories/natures/userParty が各サンプルと一致 | 正常系 |
| LD-002 | 異常系 | pokedex 欠落で throw | `pokedex.json` が 404 | throw `'データファイルが見つかりません。C# ツールを実行してください'` | データ欠落 |
| LD-003 | 異常系 | party 欠落で throw | `party.json` が 404 | throw `'party.json が見つかりません'` | party欠落 |
| LD-004 | 異常系 | party 構文不正で throw | `party.json` の json() が `SyntaxError` を reject | throw `'party.json の形式が正しくありません。JSONを確認してください'` | パースエラー |
| LD-005 | 異常系 | party キーが非配列で throw | `party.json = { party: {} }` | throw `'party.json の形式が正しくありません。…'` | 型不正 |
| LD-006 | 異常系 | 必須 species 欠落で throw | party要素に species 無 | throw `'party.json の形式が正しくありません。…'` | 必須検証 |
| LD-007 | 異常系 | 必須 nature 欠落で throw | party要素に nature 無 | throw `'party.json の形式が正しくありません。…'` | 必須検証 |
| LD-008 | 異常系 | 必須 abilityPoints 欠落で throw | party要素に abilityPoints 無 | throw `'party.json の形式が正しくありません。…'` | 必須検証 |
| LD-009 | 異常系 | 必須 moves 欠落で throw | party要素に moves 無 | throw `'party.json の形式が正しくありません。…'` | 必須検証 |
| LD-010 | 異常系 | types 欠落で throw | `types.json` が 404 | throw `'types.json が見つかりません。リポジトリを確認してください'` | types欠落 |
| LD-010b | 正常系 | species が pokedex.json に未登録でも load() は継続 | party.species=`'未知のポケモン'`、他 JSON は正常 | throw せず完了、`userParty.party[0].species==='未知のポケモン'`、`getPokemonByName('未知のポケモン')===null` | PRD 機能1「不明なポケモン表示で起動継続」をデータ層で担保（UI 側で null チェック） |

### `DataLoader.getTypeName(type)`

> タイプの英語名を日本語名に変換する。未知のタイプは英語表記をそのまま返す。
>
> - `type`（string）: タイプの英語名（例 `'Fire'`）
> - 戻り値（string）: 日本語名。未知のタイプは入力をそのまま返す

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-011 | 正常系 | 既知タイプを日本語化 | `getTypeName('Fire')` / `getTypeName('Dragon')` | `'ほのお'` / `'ドラゴン'` | 英→日 |
| LD-012 | 異常系 | 未知タイプはそのまま返す | `getTypeName('Unknown')` | `'Unknown'` | フォールバック |

### `DataLoader.getMoveCategory(cat)`

> 技カテゴリの内部値（Physical/Special/Status）を日本語表記に変換する。
>
> - `cat`（string）: 技カテゴリの内部値（`'Physical'` / `'Special'` / `'Status'`）
> - 戻り値（string）: 日本語表記。未知の値は入力をそのまま返す

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-013 | 正常系 | カテゴリを日本語化（Physical / Status） | `getMoveCategory('Physical')` / `getMoveCategory('Status')` | `'物理'` / `'変化'` | 英→日 |
| LD-013a | 正常系 | カテゴリを日本語化（Special） | `getMoveCategory('Special')` | `'特殊'` | 同値分割の欠落補完 |

### `DataLoader.getNatureModifiers(name)`

> 性格名から補正倍率マップを返す。補正なしの性格は空オブジェクトを返す。
>
> - `name`（string）: 性格名（例 `'いじっぱり'`）
> - 戻り値（object: `{[stat]: number}`）: 補正倍率マップ。補正なしは空オブジェクト `{}`

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-014 | 正常系 | 補正ありの性格 | `getNatureModifiers('いじっぱり')` | `{atk:1.1, spa:0.9}` | 補正マップ |
| LD-015 | 正常系 | 補正なしの性格 | `getNatureModifiers('がんばりや')` | `{}` | 空オブジェクト |

### `DataLoader.getMove(name)`

> 技名から Move オブジェクトを返す。未登録は `null`。
>
> - `name`（string）: 技名（例 `'じしん'`）
> - 戻り値（object | null）: Move オブジェクト。未登録は `null`

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-016 | 正常系 | 存在する技 | `getMove('じしん')` | `type='Ground', category='Physical', power=100`（非null） | 取得成功 |
| LD-017 | 異常系 | 存在しない技 | `getMove('存在しない技')` | `null` | 未登録 |

### `DataLoader.getItemModifier(name)`

> 持ち物名から補正オブジェクトを返す（`modifier` ラッパーを除去した形）。未登録は `null`。
>
> - `name`（string）: 持ち物名（例 `'こだわりスカーフ'`）
> - 戻り値（object | null）: 補正オブジェクト（`modifier` ラッパー除去）。未登録は `null`

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-018 | 正常系 | 登録済み持ち物 | `getItemModifier('こだわりスカーフ')` | `spe === 1.5`（ラッパー除去） | 取得成功 |
| LD-019 | 異常系 | 未登録の持ち物 | `getItemModifier('存在しない持ち物')` | `null` | 未登録 |

### `DataLoader.getAbilityModifier(name)`

> 特性名から補正オブジェクトを返す（`modifier` ラッパーを除去した形）。未登録は `null`。
>
> - `name`（string）: 特性名（例 `'さめはだ'`）
> - 戻り値（object | null）: 補正オブジェクト（`modifier` ラッパー除去）。未登録は `null`

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-020 | 正常系 | 登録済み特性 | `getAbilityModifier('さめはだ')` | `condition === 'isContact'`（ラッパー除去） | 取得成功 |
| LD-021 | 異常系 | 未登録の特性 | `getAbilityModifier('存在しない特性')` | `null` | 未登録 |

### `DataLoader.searchByName(query)`

> ロード済みの図鑑データから前方一致検索する。`load()` 前や空文字は空配列を返す。
>
> - `query`（string）: 検索入力
> - 戻り値（object[]）: 前方一致した図鑑エントリ（num 昇順・最大10件）。`load()` 前や空文字は空配列

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-022 | 正常系 | 前方一致検索 | `searchByName('ガブ')` | `['ガブリアス']` | 検索成功 |
| LD-023 | 境界値 | load() 前の呼び出し | 未ロードで `searchByName('ガブ')` | `[]` | 未初期化の安全性 |
| LD-024 | 境界値 | 空文字クエリ | `searchByName('')` | `[]` | 空入力 |

### `DataLoader.getPokemonByName(name)`

> ポケモン名から図鑑エントリ（PokedexEntry）を返す。未登録は `null`。
>
> - `name`（string）: ポケモン名（例 `'ガブリアス'`）
> - 戻り値（object | null）: 図鑑エントリ（PokedexEntry）。未登録は `null`

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-025 | 正常系 | 存在するポケモン名・返却オブジェクト構造を確認 | `getPokemonByName('ガブリアス')` | 非null かつ `name='ガブリアス'`, `types=['Dragon','Ground']`, `baseStats.spe===102`, `abilities` が配列で長さ ≥ 1 | calcActualStats / calcSpeedPatterns へ渡す前提構造の整合性担保 |
| LD-026 | 異常系 | 存在しないポケモン名 | `getPokemonByName('存在しないポケモン')` | `null` | 未登録 |
| LD-027 | 正常系 | メガフォーム名で `null` 返却（機能 7: party.json でメガ名指定は不明扱い） | `getPokemonByName('メガフシギバナ')` / `getPokemonByName('メガリザードンＸ')` / `getPokemonByName('メガリザードンＹ')` | いずれも `null` | PRD 機能 7。マスターデータ上はメガ独立エントリだが JS 層で null フィルタ |

### `DataLoader.getMegaInfo(parentName)` / `isMegaForm(name)` / `getMegaFormData(megaName)`（機能 7）

> メガシンカ関連 API。`src/data/mega-evolutions.json` の親 → `{ stones, megaForms }` マップを参照する。
>
> - `getMegaInfo(parentName)`: メガシンカ可能な親なら `{ stones: string[], megaForms: string[] }`、不可なら `null`
> - `isMegaForm(name)`: 名前がメガフォームなら `true`
> - `getMegaFormData(megaName)`: メガフォーム名で pokedex エントリ。通常名・未登録名なら `null`

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| LD-028 | 正常系 | getMegaInfo: 単一メガ（フシギバナ） | `getMegaInfo('フシギバナ')` | `{stones:['フシギバナイト'], megaForms:['メガフシギバナ']}` | 標準パターン |
| LD-029 | 正常系 | getMegaInfo: 複数メガ（リザードン） | `getMegaInfo('リザードン')` | `{stones:['リザードナイトＸ','リザードナイトＹ'], megaForms:['メガリザードンＸ','メガリザードンＹ']}` | 複数フォーム |
| LD-030 | 異常系 | getMegaInfo: メガ不可ポケモン | `getMegaInfo('ガブリアス')` | `null` | 非対象 |
| LD-031 | 正常系 | isMegaForm: メガフォーム名 | `isMegaForm('メガフシギバナ')` / `isMegaForm('メガリザードンＸ')` | `true` | 判定 |
| LD-032 | 異常系 | isMegaForm: 通常名・未登録名 | `isMegaForm('フシギバナ')` / `isMegaForm('ガブリアス')` / `isMegaForm('存在しないポケモン')` | `false` | 非対象 |
| LD-033 | 正常系 | getMegaFormData: メガ名で PokedexEntry | `getMegaFormData('メガフシギバナ')` | 非null かつ `name='メガフシギバナ'`, `baseStats.atk===100`, `abilities=['あついしぼう']` | 取得成功 |
| LD-034 | 異常系 | getMegaFormData: 通常名で null | `getMegaFormData('フシギバナ')` / `getMegaFormData('ガブリアス')` | `null` | 通常名は別 API 経由のため |
| LD-035 | 正常系 | searchByName: メガフォームをサジェスト除外 | `searchByName('メガ')` | `[]` | 機能 7 のサジェスト除外を担保 |
| LD-036 | 正常系 | searchByName: 親ポケモン名は通常通り候補に含まれる | `searchByName('リザード')` | `[{name:'リザードン',...}]` のみ | メガ除外しても親は含まれる |

---

## IncrementalRunnerTests.cs

**テスト対象**: `tools/PokelensTools/IncrementalRunner.cs`（増分実行判定）。チェックサムの新旧比較で再実行すべきステップを決める。

### `IncrementalRunner.DetermineSteps(previous, current)`

> チェックサムの新旧を比較し、再実行が必要なステップ（Step2/3/4）を判定する。初回（`previous` が null）は全ステップ必要。
>
> - `previous`（`ChecksumSet?`）: 前回保存したチェックサム。初回は null
> - `current`（`ChecksumSet`）: 今回計算したチェックサム
> - 戻り値（`Steps` = `record(bool NeedsStep2, bool NeedsStep3, bool NeedsStep4)`）: 各ステップの要否

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| IRT-001 | 正常系 | 初回は全ステップ必要 | `previous=null`, `current=`全11項目 | `Step2/3/4 = true` | 初回実行 |
| IRT-002 | 正常系 | Showdown 図鑑変更でStep2〜4 | `showdown-pokedex` のみ差分 | `Step2/3/4 = true` | 上流変更 |
| IRT-003 | 組合せ | champions-patch のみ変更 | `champions-patch` のみ差分 | `Step2=false, Step3/4=true` | Step分岐 |
| IRT-003b | 組合せ | showdown-pokedex + champions-patch 同時変化 | `showdown-pokedex` と `champions-patch` の両方が差分 | `Step2/3/4 = true` | 最上流の Step2 起動点が優先 |
| IRT-004b | 組合せ | champions-patch + moves-power-patch 同時変化 | `champions-patch` と `moves-power-patch` の両方が差分 | `Step2=false, Step3/4=true` | 最上流の Step3 起動点が優先 |
| IRT-004 | 組合せ | moves-power-patch のみ変更 | `moves-power-patch` のみ差分 | `Step2/3=false, Step4=true` | Step4のみ |
| IRT-005 | 組合せ | items-modifiers のみ変更 | `items-modifiers` のみ差分 | `Step2/3=false, Step4=true` | Step4のみ |
| IRT-006 | 組合せ | abilities-modifiers のみ変更 | `abilities-modifiers` のみ差分 | `Step2/3=false, Step4=true` | Step4のみ |
| IRT-007 | 組合せ | pokemon-name-patch のみ変更 | `pokemon-name-patch` のみ差分 | `Step2/3=false, Step4=true` | Step4のみ |
| IRT-008 | 組合せ | item-name-patch のみ変更 | `item-name-patch` のみ差分 | `Step2/3=false, Step4=true` | Step4のみ |
| IRT-009 | 組合せ | pokeapi-translations のみ変更 | `pokeapi-translations` のみ差分 | `Step2/3=false, Step4=true` | Step4のみ |
| IRT-010 | 正常系 | 変更なしは全ステップ不要 | `previous === current`（同一） | `Step2/3/4 = false` | 無変更 |

### `IncrementalRunner.LoadChecksums(path)`

> JSON ファイルからチェックサムを読み込む。ファイルが存在しなければ null。
>
> - `path`（string）: チェックサム JSON のパス
> - 戻り値（`ChecksumSet?`）: 読み込んだ値。ファイル無し・デシリアライズ不可は null

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| IRT-011 | 異常系 | 存在しないファイル | ランダムな未存在パス | `null` | 未存在時の安全性 |

### `IncrementalRunner.ComputeHash(filePath)`

> ファイル内容のハッシュ文字列を計算する。ファイルが存在しなければ空文字。
>
> - `filePath`（string）: 対象ファイルのパス
> - 戻り値（string）: ハッシュ文字列。ファイル無しは空文字

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| IRT-012 | 異常系 | 存在しないファイル | ランダムな未存在パス | `""`（空文字） | 未存在時の安全性 |

### `IncrementalRunner.SaveChecksums(checksums, path)`

> チェックサムを JSON ファイルへ保存する（`LoadChecksums` と対）。
>
> - `checksums`（`ChecksumSet`）: 保存するチェックサム
> - `path`（string）: 出力パス
> - 戻り値: なし（void）

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| IRT-013 | 正常系 | 保存→読込のラウンドトリップ | 11キー辞書を `SaveChecksums` 後 `LoadChecksums` | 元の辞書と一致 | 永続化の往復 |

---

## MergeConverterTests.cs

**テスト対象**: `tools/PokelensTools/MergeConverter.cs`（Showdown 生データ → アプリ用 JSON への変換）。

### `MergeConverter.FlagToTag(flag)`

> Showdown の技フラグ名をアプリ内タグ名に変換する。一般フラグは `is` プレフィックス付与、`slicing` は `isSlice`。
>
> - `flag`（string）: Showdown のフラグ名（例 `'contact'`）
> - 戻り値（string）: タグ名（例 `'isContact'`）

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| MCT-001 | 正常系 | 一般フラグは is プレフィックス | `'contact'` / `'punch'` / `'pulse'` / `'bite'` / `'protect'` | `'isContact'` / `'isPunch'` / `'isPulse'` / `'isBite'` / `'isProtect'` | 接頭辞付与 |
| MCT-002 | 境界値 | slicing は特例変換 | `'slicing'` | `'isSlice'` | 不規則変換 |

### `MergeConverter.ConvertPokedex(showdownPokedex, pokemonNames, abilityNames, pokemonNamePatch)`

> Showdown 図鑑を、日本語名・特性スロット順に整形した図鑑へ変換する。日本語訳が無いエントリは除外。
>
> - `showdownPokedex`（JsonObject）: Showdown 図鑑
> - `pokemonNames`（JsonObject）: PokéAPI 由来のポケモン和名マップ
> - `abilityNames`（JsonObject）: 特性の和名マップ
> - `pokemonNamePatch`（JsonObject）: ポケモン和名の上書きパッチ
> - 戻り値（JsonObject）: 変換後の図鑑

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| MCT-003 | 正常系 | 日本語名に変換 | pikachu, 和名 `'ピカチュウ'` | `name === 'ピカチュウ'` | 和名適用 |
| MCT-004 | 正常系 | 特性を和名＋スロット順に | abilities `{0:Static, H:Lightning Rod}` | `['せいでんき','ひらいしん']`（2件・順序保持） | スロット順 |
| MCT-005 | 異常系 | 訳なしエントリは除外 | 和名マップに無いエントリ | 空（除外） | 未翻訳除外 |
| MCT-006 | 組合せ | 名前パッチが訳を上書き | patch `{pikachu:'パートナーピカチュウ'}` | `name === 'パートナーピカチュウ'` | パッチ優先 |

### `MergeConverter.ConvertMoves(showdownMoves, moveNames, movesPowerPatch)`

> Showdown 技データを、和名キー・`power`/`accuracy` 正規化・タグ付与した技へ変換する。
>
> - `showdownMoves`（JsonObject）: Showdown 技データ
> - `moveNames`（JsonObject）: 技の和名マップ
> - `movesPowerPatch`（JsonObject）: 威力上書きパッチ
> - 戻り値（JsonObject）: 変換後の技

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| MCT-007 | 境界値 | basePower 0 は power=null | splash（basePower 0） | `power === null` | 威力不定化 |
| MCT-008 | 境界値 | accuracy true は accuracy=null | accuracy `true` | `accuracy === null` | 必中の正規化 |
| MCT-009 | 正常系 | 連続技は最大ヒット×威力 | doubleslap（basePower 15, multihit `[2,5]`） | `power === 75`（15×5） | 多段ヒット |
| MCT-010 | 正常系 | 単一整数 multihit も同様 | tripleaxel（basePower 20, multihit `3`） | `power === 60`（20×3） | 多段ヒット |
| MCT-011 | 組合せ | 威力パッチで null を上書き | metronome ＋ patch `power:120` | `power === 120` | パッチ適用 |
| MCT-012 | 正常系 | flags をタグに変換 | thunderbolt flags `{protect, mirror}` | tags に `isProtect`, `isMirror` | フラグ→タグ |
| MCT-013 | 正常系 | recoil フィールドで isRecoil 付与 | doubleedge `recoil:true` | tags に `isRecoil` | 反動技 |
| MCT-014 | 境界値 | flags なしは tags フィールド省略 | splash（flags なし） | `tags` フィールド無し（null） | 空時の省略 |
| MCT-015 | 正常系 | secondary オブジェクトで hasSecondary | flamethrower `secondary:{…}` | tags に `hasSecondary` | 追加効果 |
| MCT-016 | 正常系 | secondaries 配列で hasSecondary | firefang `secondaries:[…]` | tags に `hasSecondary` | 追加効果（複数） |
| MCT-017 | 境界値 | 追加効果なし時の tags 構成（肯定＋否定） | thunderbolt（flags=`{protect,mirror}`、secondary なし） | tags に `isProtect` が含まれ、かつ `hasSecondary` を含まない | flags は正しく変換され、secondary 由来のみ抑止される |

### `MergeConverter.ConvertItems(itemsModifiers, itemNames, itemNamePatch)`

> 持ち物の補正定義を和名キーへ変換する。和名が無いものは除外、パッチで補完／上書き可能。
>
> - `itemsModifiers`（JsonObject）: 英語キーの持ち物補正
> - `itemNames`（JsonObject）: 持ち物の和名マップ
> - `itemNamePatch`（JsonObject）: 和名の補完／上書きパッチ
> - 戻り値（JsonObject）: 和名キーの持ち物補正

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| MCT-018 | 正常系 | 和名キーへ変換 | choicescarf → `'こだわりスカーフ'` | 和名キーあり・英語キーなし | キー変換 |
| MCT-019 | 異常系 | 訳なしは除外 | 和名マップに無い | 空（除外） | 未翻訳除外 |
| MCT-020 | 組合せ | 名前パッチが訳を上書き | patch で和名上書き | パッチ名キーあり・元和名キーなし | パッチ優先 |
| MCT-021 | 組合せ | 名前パッチが欠落訳を補完 | 和名なし ＋ patch | パッチ名キーあり | 補完 |

### `MergeConverter.ConvertAbilities(abilitiesModifiers, abilityNames)`

> 特性の補正定義を和名キーへ変換する。和名が無いものは除外。
>
> - `abilitiesModifiers`（JsonObject）: 英語キーの特性補正
> - `abilityNames`（JsonObject）: 特性の和名マップ
> - 戻り値（JsonObject）: 和名キーの特性補正

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| MCT-022 | 正常系 | 和名キーへ変換 | ironfist → `'てつのこぶし'` | 和名キーあり・英語キーなし | キー変換 |
| MCT-023 | 異常系 | 訳なしは除外 | 和名マップに無い | 空（除外） | 未翻訳除外 |

---

## PatchApplicatorTests.cs

**テスト対象**: `tools/PokelensTools/PatchApplicator.cs`（マスターデータ JSON へのパッチ部分適用）。

### `PatchApplicator.ApplyPokedexPatch(pokedexPath, patchSection)`

> 図鑑 JSON ファイルにパッチを部分適用する（baseStats/types/abilities を部分上書き）。`patchSection` が null なら無変更。
>
> - `pokedexPath`（string）: 図鑑 JSON のパス（その場で書き換え）
> - `patchSection`（`JsonObject?`）: パッチ内容。null は無変更
> - 戻り値: なし（void）

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PAT-001 | 正常系 | baseStats を部分上書き | patch `baseStats{atk:99, spe:110}` | `atk=99, spe=110`、`hp/def` 据え置き | 部分マージ |
| PAT-002 | 正常系 | types を上書き | patch `types['Electric','Steel']` | 2要素に置換 | 配列置換 |
| PAT-003 | 正常系 | abilities を部分上書き／追加 | patch `abilities{0:Surge Surfer, 1:Hidden Power}` | slot0 上書き・slot1 追加・slotH 据え置き | スロット単位 |
| PAT-004 | 境界値 | 未指定フィールドは不変 | patch `baseStats{atk:99}` のみ | `hp` 据え置き | 非対象保持 |
| PAT-005 | 異常系 | null パッチは無変更 | `patchSection = null` | ファイル内容が不変 | null ガード |

### `PatchApplicator.ApplyMovesPatch(movesPath, patchSection)`

> 技 JSON ファイルにパッチを部分適用する（basePower/accuracy/category を上書き）。
>
> - `movesPath`（string）: 技 JSON のパス（その場で書き換え）
> - `patchSection`（`JsonObject?`）: パッチ内容
> - 戻り値: なし（void）

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PAT-006 | 正常系 | basePower を上書き | patch `basePower:110` | `basePower === 110` | 上書き |
| PAT-007 | 正常系 | accuracy を上書き | patch `accuracy:85` | `accuracy=85`、`basePower` 据え置き 90 | 部分上書き |
| PAT-008 | 正常系 | category を上書き | patch `category:'Physical'` | `category='Physical'`、`type` 据え置き Electric | 部分上書き |

---

## PokeAPIFetcherTests.cs

**テスト対象**: `tools/PokelensTools/Fetchers/PokeAPIFetcher.cs`（PokéAPI からの和名取得。HTTP オーケストレーション）。

### `PokeAPIFetcher.FetchJapaneseNameAsync(url)`

> 指定 URL から名称 JSON を取得し日本語名を返す（HTTP モックで検証）。404/400 は null、不正 JSON は例外伝播。
>
> - `url`（string）: 取得先 URL
> - 戻り値（`Task<string?>`）: 日本語名。NotFound/BadRequest は null。不正 JSON は `JsonException`

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PFT-001 | 正常系 | 200＋ja名 | 200 ＋ `ja` 名を含む JSON | `'ピカチュウ'` | 取得成功 |
| PFT-002 | 異常系 | 404 は null | HTTP 404 | `null` | NotFound |
| PFT-003 | 異常系 | 400 は null | HTTP 400 | `null` | BadRequest |
| PFT-004 | 異常系 | 不正 JSON は例外 | 200 ＋ `'not json'` | `JsonException` を throw | パース失敗を伝播 |
| PFT-005 | 異常系 | 日本語なしは null | 200 ＋ `en` のみ | `null` | 該当なし |

---

## PokeApiNameTests.cs

**テスト対象**: `tools/PokelensTools/Common/PokeApiName.cs`（PokéAPI レスポンス JSON からの和名・一致フォルム抽出。純粋ロジック）。

### `PokeApiName.FindMatchingVariety(speciesNode, targetSlug)`

> PokéAPI species の `varieties` から、対象スラッグに最長前方一致する variety 名を返す。無ければ null。
>
> - `speciesNode`（JsonNode）: species エンドポイントの JSON
> - `targetSlug`（string）: 照合対象スラッグ
> - 戻り値（`string?`）: 一致した variety 名。無ければ null

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PNM-001 | 正常系 | 完全一致 | varieties `[rotom, rotom-wash]`, target `'rotom-wash'` | `'rotom-wash'` | 完全一致 |
| PNM-002 | 正常系 | 最長前方一致 | varieties `[ogerpon, ogerpon-wellspring-mask]`, target `'ogerpon-wellspring'` | `'ogerpon-wellspring-mask'` | 最長一致 |
| PNM-003 | 異常系 | 不一致は null | varieties `[pikachu]`, target `'raichu'` | `null` | 不一致 |
| PNM-004 | 異常系 | varieties フィールド無し | `{name:'test'}` | `null` | フィールド欠落 |

### `PokeApiName.ExtractJa(root, arrayKey)`

> 名称配列から日本語名を抽出する。`ja` を優先し、無ければ `ja-Hrkt`、どちらも無ければ null。
>
> - `root`（JsonNode）: 名称配列を含む JSON
> - `arrayKey`（string）: 配列のキー（`'names'` / `'form_names'`）
> - 戻り値（`string?`）: 日本語名。無ければ null

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PNM-005 | 正常系 | ja を ja-Hrkt より優先 | names に `ja` と `ja-Hrkt` 両方 | `'ピカチュウ漢字版'`（ja） | 優先順位 |
| PNM-006 | 正常系 | ja-Hrkt のみなら採用 | names に `ja-Hrkt` のみ | `'ピカチュウ'` | フォールバック |
| PNM-007 | 異常系 | 日本語なしは null | names に `en` のみ | `null` | 該当なし |
| PNM-008 | 異常系 | 配列キー欠落は null | `{name:'test'}`（names なし） | `null` | フィールド欠落 |
| PNM-009 | 正常系 | form_names キーでも動作 | `form_names` に `ja` | `'ウォッシュロトム'` | 別キー対応 |

---

## PokeApiSlugTests.cs

**テスト対象**: `tools/PokelensTools/Common/PokeApiSlug.cs`（Showdown 名 → PokéAPI slug 変換。純粋ロジック）。

### `PokeApiSlug.PokemonFormSlug(showdownName)`

> Showdown のポケモン名を PokéAPI の form スラッグへ変換する（記号・空白・アクセント等を正規化）。
>
> - `showdownName`（string）: Showdown 表記のポケモン名
> - 戻り値（string）: PokéAPI スラッグ

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PSL-001〜013 | 正常系 | 各種表記のスラッグ化（`Theory` 13件）<br>変換種別の内訳:<br>① 小文字化のみ（`'Bulbasaur'`）<br>② 既存ハイフン保持＋小文字化（`'Rotom-Wash'` / `'Necrozma-Dusk-Mane'` / `'Venusaur-Mega'` / `'Urshifu-Rapid-Strike'`）<br>③ 空白→ハイフン（`'Tapu Koko'`）<br>④ 約物（`.` / `:`）除去＋空白→ハイフン（`'Mr. Mime'` / `'Mime Jr.'` / `'Type: Null'`）<br>⑤ アポストロフィ除去（ASCII `'` / Unicode `’`）（`'Farfetch'd'` / `'Farfetch’d-Galar'`）<br>⑥ パーセント記号除去（`'Zygarde-10%'`）<br>⑦ アクセント正規化（`'Flabébé'` → `'flabebe'`） | 各 `'bulbasaur'` / `'rotom-wash'` / `'necrozma-dusk-mane'` / `'tapu-koko'` / `'mr-mime'` / `'mime-jr'` / `'type-null'` / `'farfetchd'` / `'farfetchd-galar'` / `'zygarde-10'` / `'flabebe'` / `'venusaur-mega'` / `'urshifu-rapid-strike'` | 記号・空白・アクセントの各正規化を網羅 |

### `PokeApiSlug.ItemSlug(showdownName)`

> Showdown の持ち物名を PokéAPI の item スラッグへ変換する。
>
> - `showdownName`（string）: Showdown 表記の持ち物名
> - 戻り値（string）: PokéAPI スラッグ

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| PSL-014〜019 | 正常系 | 各種持ち物のスラッグ化（`Theory` 6件） | `'Choice Scarf'` / `'Life Orb'` / `'Wellspring Mask'` / `'Ice Stone'` / `'Auspicious Armor'` / `'Metal Alloy'` | 各 `'choice-scarf'` / `'life-orb'` / `'wellspring-mask'` / `'ice-stone'` / `'auspicious-armor'` / `'metal-alloy'` | 空白→ハイフン・小文字化 |

---

## ShowdownFetcherTests.cs

**テスト対象**: `tools/PokelensTools/ShowdownFetcher.cs`（Showdown の JS データ取得・JSON 化）。

### `ShowdownFetcher.JsToJson(js)`

> Showdown の JS データ（`exports.X = {…}`）を JSON 文字列へ変換する。未引用キー・数値キーの引用、末尾カンマ除去等を行う。トップレベルオブジェクトが無ければ `FormatException`。
>
> - `js`（string）: Showdown の JS ソース
> - 戻り値（string）: パース可能な JSON 文字列

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| SFT-001 | 正常系 | exports ラッパー除去＋未引用キー引用 | `exports.BattlePokedex = {pikachu:{num:25,name:"Pikachu"}}` | `pikachu.num=25, name='Pikachu'` | 基本変換 |
| SFT-002 | 正常系 | 数値キーを引用 | `{pikachu:{abilities:{0:"Static",H:"Lightning Rod"}}}` | キー `"0"`/`"H"` で取得可 | 数値キー |
| SFT-003 | 境界値 | 配列要素は引用しない | `{foo:{nums:[2,5], strs:["a","b"]}}` | `nums[0]=2, nums[1]=5` | 配列保持 |
| SFT-004 | 正常系 | 末尾カンマ除去 | オブジェクト・配列の末尾カンマ | パース成功（`b` 配列3要素） | 末尾カンマ |
| SFT-005 | 正常系 | ネストオブジェクト | `pikachu.baseStats{…}` ネスト | `hp=35, spe=90` | 入れ子 |
| SFT-006 | 境界値 | 既に引用済みキー | `{"pikachu":{"num":25}}` | `num=25` | 冪等 |
| SFT-007 | 正常系 | 真偽値の保持 | `{protect:{accuracy:true, basePower:0}}` | `accuracy=true, basePower=0` | bool |
| SFT-008 | 異常系 | トップレベル無しは例外 | `'use strict';`（オブジェクト無し） | `FormatException` を throw | 不正入力 |

### `ShowdownFetcher.BuildMoveEntry(entry)`

> Showdown 技エントリをアプリ用エントリへ変換する。Z技（`isZ`）・ダイマックス技（`isMax`）は null で除外。
>
> - `entry`（JsonObject）: Showdown 技エントリ
> - 戻り値（`JsonObject?`）: 変換後エントリ。Z技/ダイマックス技は null

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| SFT-009 | 異常系 | Z技は除外 | `isZ:true` | `null` | 除外 |
| SFT-010 | 異常系 | ダイマックス技は除外 | `isMax:true` | `null` | 除外 |
| SFT-011 | 正常系 | 通常技は変換 | 通常の技エントリ | 非null、`basePower=90` | 取得成功 |

### `ShowdownFetcher.BuildItemEntry(entry)`

> Showdown 持ち物エントリを変換する。`isNonstandard` の持ち物は null で除外。
>
> - `entry`（JsonObject）: Showdown 持ち物エントリ
> - 戻り値（`JsonObject?`）: 変換後エントリ。非標準は null

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| SFT-012 | 異常系 | 非標準持ち物は除外 | `isNonstandard:'Past'` | `null` | 除外 |
| SFT-013 | 正常系 | 通常持ち物は変換 | 通常の持ち物エントリ | 非null、`name='Choice Scarf'` | 取得成功 |

### `ShowdownFetcher.BuildAbilityEntry(entry)`

> Showdown 特性エントリを変換する。`isNonstandard` の特性は null で除外。
>
> - `entry`（JsonObject）: Showdown 特性エントリ
> - 戻り値（`JsonObject?`）: 変換後エントリ。非標準は null

| ケースID | 種別 | テスト観点 | 入力値 | 期待結果 | 備考 |
|---|---|---|---|---|---|
| SFT-014 | 異常系 | 非標準特性は除外 | `isNonstandard:'Future'` | `null` | 除外 |
| SFT-015 | 正常系 | 通常特性は変換 | 通常の特性エントリ | 非null、`num=9` | 取得成功 |
