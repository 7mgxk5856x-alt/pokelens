# E2E テストケース一覧（自動テスト）

pokelens で実装済みの**自動・E2E テスト**（Playwright）を、テストファイル・ケース単位でまとめたドキュメント。`page.route()` で `data/party.json` を mock 注入し、UI 結合と画面表示を実ブラウザ（Chromium）で検証する。

> 単体テストは [../unit/automated-test-cases.md](../unit/automated-test-cases.md)、統合テストは [../integration/automated-test-cases.md](../integration/automated-test-cases.md) を参照。手動（環境セットアップ）E2E は [./manual-test-cases.md](./manual-test-cases.md) を参照。

- **テストフレームワーク**: Playwright（`@playwright/test`）
- **対象**: `tests/e2e/**/*.spec.js`
- **対象ブラウザ**: Chromium のみ（PRD で PC Chrome 最新版指定）
- **実行コマンド**: `npm run test:e2e`（UI モード: `npm run test:e2e:ui` / headed: `npm run test:e2e:headed`）
- **初回セットアップ**: `npx playwright install chromium`

> このドキュメントはテストコードから手動で抽出したスナップショットです。テスト追加・変更時は併せて更新してください。

## 表の項目

| 項目 | 説明 |
|---|---|
| ケースID | 一意な識別子。`AET-<連番>` 形式（AET = Automated E2E Test） |
| 種別 | 正常系 / 異常系 / 境界値 のいずれか |
| 対応要件 | 検証対象の PRD 機能番号 |
| テスト観点 | 何を確認したいか |
| 入力 / 操作 | テスト内の主要操作・データ注入 |
| 期待結果 | DOM 検証で確認する内容 |
| 備考 | 元 MET 番号・対応する単体/統合テスト等 |

> **採番規則**: ケース ID は末尾追加で連番採番。元 MET 番号との対応は備考欄に明記する。ファイル別グルーピングのため、ID 番号と物理順序は必ずしも一致しない（番号飛びは許容）。

**ファイル接頭辞**: AET=Automated E2E Test

## サマリ

| テストファイル | テスト対象 | ケース数 |
|---|---|---:|
| [own-party-display.spec.js](#own-party-displayspecjs) | 自分パーティの一覧表示・選択・状態遷移・メガシンカ切替 | 9 |
| [own-pokemon-detail.spec.js](#own-pokemon-detailspecjs) | 自分ポケモン詳細・火力指数・性格・技一覧・耐久指数 | 15 |
| [opponent-suggest.spec.js](#opponent-suggestspecjs) | 相手パーティ入力・サジェスト検索・XSS 耐性 | 10 |
| [opponent-pokemon-detail.spec.js](#opponent-pokemon-detailspecjs) | 相手ポケモン詳細・素早さ 4 パターン・耐久指数 4 パターン・切替・メガシンカ切替 | 5 |
| [error-handling.spec.js](#error-handlingspecjs) | party.json 構文不正・必須フィールド欠落 | 5 |
| **合計** | | **44** |

---

## own-party-display.spec.js

**テスト対象**: 自分パーティ表示（`src/ui/own-party-panel.js` + `src/main.js` の統合）

| ケースID | 種別 | 対応要件 | テスト観点 | 入力 / 操作 | 期待結果 | 備考 |
|---|---|---|---|---|---|---|
| AET-001 | 正常系 | 機能1 | 起動時に 6 枚カード表示 | STANDARD_PARTY を注入し `/` へ移動 | `#own-party .pokemon-card` が 6 枚。各カードに `.name` / `.types` / `.base-stats` がある | 元 MET-004 |
| AET-002 | 正常系 | 機能1 | 初期は未選択・詳細非表示 | STANDARD_PARTY を注入 | `.selected` クラスを持つカードが 0 枚。`#own-detail` が hidden | 元 MET-005 |
| AET-003 | 正常系 | 機能1 | カード選択で詳細表示 | 1 枚目をクリック | `.selected` 付与・`#own-detail` が visible・特性/持ち物/性格/種族値/実数値/技テーブルが表示 | 元 MET-006 |
| AET-004 | 異常系 | 機能1 | 未知 species で「不明なポケモン: ...」表示・他カード正常 | UNKNOWN_SPECIES_FIXTURE（2 枚目に「まぼろしポケモン」）を注入 | 2 枚目の `.error` に「不明なポケモン: まぼろしポケモン」。1 枚目（ガブリアス）は正常表示 | 元 MET-007 |
| AET-005 | 正常系 | 機能1 | party.json 変更後リロードで反映 | STANDARD_PARTY → reload + 別 fixture | リロード後のカード枚数が新 fixture に追従 | 元 MET-026 |
| AET-006 | 正常系 | 機能1 | 別ポケモン選択時の詳細表示切替 | 1 枚目→ 2 枚目選択 | 詳細の `.name` が「ガブリアス」→「ピカチュウ」に切替。1 枚目の `.selected` は外れる | 元 MET-034。MET-032（相手側）と対称 |
| AET-039 | 正常系 | 機能7 | 自分側メガ切替ボタンの表示制御（持ち物がメガストーン一致時のみ表示）+ 切替後にカード表示更新 | MEGA_FIXTURE（フシギバナ+フシギバナイト/+カメックスナイト/+こだわりハチマキ、リザードン+ナイトＸ/持ち物なし、ガブリアス+スカーフ） | カード 0/3 のみ `.mega-toggle` 表示。フシギバナ切替で `.name` が「フシギバナ」↔「メガフシギバナ」 | PRD 機能 7。持ち物制約と切替循環を検証 |
| AET-040 | 正常系 | 機能7 | 自分側は持ち物に対応するメガのみ循環する（D-10: リザードン + リザードナイトＸ は 通常 ↔ メガリザードンＸ） | MEGA_FIXTURE のリザードン+リザードナイトＸ カード | 3 回押下で 通常→メガリザードンＸ→通常→メガリザードンＸ を循環（2 状態ループ。メガリザードンＹ は登場しない） | PRD 機能 7 + D-10。自分側は持ち物既知のため「正確に参照」のユーザーストーリーを優先し、対応外メガ形態は循環に含めない（相手側は AET-041 で全形態循環を担保） |
| AET-042 | 正常系 | 機能7 | 選択済みカードでメガ切替時、自分ポケモン詳細パネルが連動して再描画される | MEGA_FIXTURE のフシギバナを選択後、メガ切替を押下 | 詳細の `.detail-header .name` が「フシギバナ」→「メガフシギバナ」、特性が「しんりょく」→「あついしぼう」へ更新 | PRD 機能 7。詳細パネル連動・特性切替を担保 |

---

## own-pokemon-detail.spec.js

**テスト対象**: 自分ポケモン詳細パネル（`src/ui/own-pokemon-detail.js` + 火力指数計算結果の UI 反映）

| ケースID | 種別 | 対応要件 | テスト観点 | 入力 / 操作 | 期待結果 | 備考 |
|---|---|---|---|---|---|---|
| AET-007 | 正常系 | 機能2 | 基本情報の全項目表示 | STANDARD_PARTY 1 枚目を選択 | 名前/タイプ/特性/持ち物/性格/種族値/実数値の各行が存在 | 元 MET-008 |
| AET-008 | 正常系 | 機能2 | 性格の上昇/下降表記（補正あり・補正なし） | NATURE_MIX（いじっぱり/まじめ）を順に選択 | 「いじっぱり A↑ C↓」「まじめ (補正なし)」が表示 | 元 MET-009 |
| AET-009 | 正常系 | 機能2 | 技一覧の表示 | STANDARD_PARTY 1 枚目を選択 | 技テーブルの thead が `['技名','タイプ','威力','分類','命中','火力指数']`、tbody が 4 行 | 元 MET-010 |
| AET-010 | 正常系 | 機能5 | 物理技の火力指数 = 30000 | GARCHOMP_PHYSICAL（いじっぱり HP32 atk32 こだわりスカーフ）を選択 | じしんの `td.power-index` が `30000`（=100×200×1.5） | 元 MET-011。PIC 系の UI 結合担保 |
| AET-011 | 境界値 | 機能5 | 変化技の火力指数欄が「−」 | MOVE_VARIANTS_FIXTURE で つるぎのまい を含むパーティを選択 | つるぎのまい行の `td.power-index` が「−」 | 元 MET-012 |
| AET-012 | 境界値 | 機能5・6 | 威力不定技の威力欄が「−」 | MOVE_VARIANTS_FIXTURE の カウンター を確認 | カウンター行の威力欄（3 列目）と火力指数欄が「−」 | 元 MET-013 |
| AET-013 | 境界値 | 機能2 | 必中技の命中率欄が「−」 | MOVE_VARIANTS_FIXTURE の つばめがえし を確認 | つばめがえし行の命中率欄（5 列目）が「−」 | 元 MET-014 |
| AET-014 | 正常系 | 機能5・6 | multihit 最大総威力 = 75 | MULTIHIT_FIXTURE（おうふくビンタ含む）を選択 | おうふくビンタ行の威力欄が `75`（=15×5） | 元 MET-015。MCT-009/PIT-007 の UI 結合担保 |
| AET-015 | 正常系 | 機能2 | タイプ名・技分類の日本語変換 | STANDARD_PARTY 1 枚目（ガブリアス）を選択 | カードのタイプが「ドラゴン / じめん」、技分類が「物理」「変化」等の日本語表記 | 元 MET-027。LD-011/013 の UI 結合担保 |
| AET-016 | 正常系 | 機能5 | 条件付き補正（てつのこぶし）がパンチ技のみに 1.2 倍 | IRONFIST_FIXTURE（ゴウカザル＋マッハパンチ＋ばかぢから）を選択 | パンチ技と非パンチ技の power-index/power 比率の差が 1.2 倍 | 元 MET-031。RMOD-018 系の UI 結合担保 |
| AET-016b | 正常系 | 機能5 | 無条件補正（おやこあい）が全技に 1.25 倍乗る | PARENTAL_BOND_FIXTURE（ガブリアス＋いじっぱり HP32 atk32＋特性=おやこあい）を選択 | じしんの火力指数=`37500`（=100×200×1.5×1.25）、げきりんの火力指数=`45000`（=120×200×1.5×1.25） | おやこあい（abilities-modifiers.json の `parentalbond`）が DataLoader → resolve-modifier → calcPowerIndex → UI まで連鎖することを担保 |
| AET-017 | 正常系 | 機能5 | 特殊技の火力指数 = 17550（SpA 経路） | GARCHOMP_SPECIAL（能力ポイント全 0、りゅうせいぐん習得）を選択 | りゅうせいぐん行の `td.power-index` が `17550`（=130×90×1.5） | 元 MET-033。MET-011 の物理経路と対称 |
| AET-035 | 正常系 | 機能16 | こだわりスカーフ持ちで S 実数値の右にスカーフ補正値（`floor(spe×1.5)`）を併記 | GARCHOMP_PHYSICAL（こだわりスカーフ・いじっぱり HP32 atk32、spe 実数値 122）を選択 | 実数値行に `S 122 (183)` を含み、S より前の列に `(` が出ない | PRD 機能 16。マスターデータ（`data/items.json` の `こだわりスカーフ.modifier.spe`）の倍率値を `loader.getItemModifier()` 経由で UI に伝搬することを担保 |
| AET-036 | 正常系 | 機能16 | こだわりスカーフ以外の持ち物では S 実数値のみ表示（括弧併記なし） | STANDARD_PARTY 2 枚目（ピカチュウ・いのちのたま・おくびょう spe32、spe 実数値 156）を選択 | 実数値行に `S 156` を含み、`(` `)` が出ない | PRD 機能 16。非該当条件の回帰防止 |
| AET-037 | 正常系 | 機能17 | 自分側で物理耐久指数・特殊耐久指数を実数値から算出し、grid 内 DOM 順序（種族値→物理→実数値→特殊）で表示 | GARCHOMP_PHYSICAL（H32 atk32 こだわりスカーフ、HP実数値 215・B実数値 115・D実数値 105）を選択 | `.detail-stats-grid > *` が 4 件、`物理耐久指数: 24725`（=215×115）と `特殊耐久指数: 22575`（=215×105）を含み、種族値→物理→実数値→特殊の順 | PRD 機能 17。`endurance-index-calc.js` の `calcEnduranceIndex` を UI に伝搬することを担保 |

---

## opponent-suggest.spec.js

**テスト対象**: 相手パーティ入力・サジェスト検索（`src/ui/search-input.js` + `src/ui/opponent-party-panel.js`）

| ケースID | 種別 | 対応要件 | テスト観点 | 入力 / 操作 | 期待結果 | 備考 |
|---|---|---|---|---|---|---|
| AET-018 | 正常系 | 機能3 | 前方一致サジェスト・図鑑番号順 | 入力欄に「ピ」を入力 | 候補が複数表示され、すべて「ピ」始まり | 元 MET-016 |
| AET-019 | 正常系 | 機能3 | 4 入力方式（ひらがな/全角カナ/半角カナ/ローマ字）すべて | 各入力欄に「がぶ」「ガブ」「ｶﾞﾌﾞ」「gabu」を入力 | いずれも候補に「ガブリアス」を含む | 元 MET-017 |
| AET-020 | 正常系 | 機能3 | 候補確定とカード表示 | 「ガブ」入力 → Enter | 入力欄が hidden、`.opponent-info` が visible・name=ガブリアス・types・base-stats が表示 | 元 MET-018 |
| AET-021 | 正常系 | 機能3 | キーボード操作（サジェスト表示中・非表示時） | (a) 表示中: Tab → 候補ハイライト遷移、フォーカスは入力欄のまま (b) 非表示時: Tab → 次の入力欄へフォーカス | (a) `.is-hover` が候補間で移動、入力欄フォーカス維持 (b) 次の入力欄が focused | 元 MET-019 |
| AET-022 | 異常系 | 機能3 | 該当なしで「見つかりません」 | 「存在しないポケモン」を入力 | `.suggest-item.is-not-found` が `見つかりません` を表示 | 元 MET-020 |
| AET-023 | 正常系 | 機能3 | クリア（× ボタン）後の状態 | 「ガブ」確定 → × クリック | 入力欄再表示・空欄、× ボタンが hidden、相手詳細パネルも hidden | 元 MET-021 |
| AET-023b | 正常系 | 機能3 | 確定済みスロットの × は Tab フォーカス対象外 | スロット 0 を確定 → スロット 1 の入力欄にフォーカス → Tab | スロット 0 の × はフォーカスされず、スロット 2 の入力欄にフォーカスが移動する | PRD 機能 3「Tab で 6 つの入力欄を順に移動」の受け入れ条件 |
| AET-024 | 正常系 | 機能3 | 未入力スロットが残ってもアプリが正常動作 | 1 枠のみ確定、残り 5 枠は空欄のまま | 確定枠は info 表示、残り 5 枠は入力欄表示・空欄、エラー非表示 | 元 MET-028 |
| AET-025 | 異常系 | 機能3 | XSS 耐性: `<script>alert(1)</script>` 入力で alert 発火しない | dialog/pageerror ハンドラ登録 →「<script>alert(1)</script>」入力 | dialog 発火なし、pageerror 発生なし、サジェストは「見つかりません」表示 | 元 MET-029。`textContent` 使用の安全性確認 |
| AET-026 | 境界値 | 機能3 | サジェスト 10 件上限 | 「ア」など多数ヒットするクエリを入力 | 候補数 ≤ 10 | 元 MET-030。NS-026 の UI 結合担保 |

---

## opponent-pokemon-detail.spec.js

**テスト対象**: 相手ポケモン詳細パネル・素早さ 4 パターン（`src/ui/opponent-pokemon-detail.js`）

| ケースID | 種別 | 対応要件 | テスト観点 | 入力 / 操作 | 期待結果 | 備考 |
|---|---|---|---|---|---|---|
| AET-027 | 正常系 | 機能4 | 基本情報・特性候補の表示 | ガブリアス を確定 | 名前=ガブリアス、タイプに「ドラゴン」「じめん」、特性候補に「さめはだ」「すながくれ」、種族値に `H 108` / `S 102` | 元 MET-022。隠れ特性件数はマスターデータ準拠 |
| AET-028 | 正常系 | 機能15 | 素早さ 4 パターンの表形式・横並び表示 | ガブリアス を確定 | `.speed-patterns thead th` が左から「最速/準速/無振り/最遅」の 4 列、`.speed-patterns tbody td` がガブリアス（種族値102）の `169 / 154 / 122 / 109` | 機能 15（旧 6 パターン MET-023 を 4 パターン化）。スカーフ補正は機能 16 で自分側 S 列に集約済み |
| AET-029 | 正常系 | 機能4 | 相手ポケモン切替時の詳細表示切替 | ガブリアス確定 → ピカチュウ確定 | 詳細の名前が ピカチュウ に切替、ガブリアス は残らない | 元 MET-032 |
| AET-041 | 正常系 | 機能7 | 相手側メガ切替ボタンは持ち物に依存せず常時表示 + 切替で詳細パネル連動・複数メガ循環 + メガ不可ポケモンでは非表示 | リザードン確定 → メガ切替 3 回（通常→Ｘ→Ｙ→通常） / ガブリアス確定 | リザードンスロットに `.mega-toggle` 表示、押下で info の `.name` と詳細の `.detail-header .name` が連動更新（リザードン↔メガリザードンＸ↔メガリザードンＹ）。ガブリアスはボタン非表示 | PRD 機能 7。相手側の常時表示・複数メガ循環・詳細連動・メガ不可ポケモンの除外を担保 |
| AET-038 | 正常系 | 機能17 | 相手側で耐久指数 4 パターン × 2 種類（物理・特殊）= 8 値を表形式で表示 | ガブリアス（H108/B95/D85）を確定 | `.endurance-patterns thead th` が 5 列（空 + 「耐久特化/耐久極振/H極振/無振り」）、`.endurance-patterns tbody th` が「物理耐久指数/特殊耐久指数」、`tbody td` が `34615 / 29463 / 24725 / 21045 / 32250 / 27450 / 22575 / 19215` | PRD 機能 17。`calcEnduranceIndexPatterns` の 4×2 行列を UI に伝搬することを担保 |

---

## error-handling.spec.js

**テスト対象**: party.json 異常検出（`src/data/loader.js` + `src/main.js` エラー表示）

| ケースID | 種別 | 対応要件 | テスト観点 | 入力 / 操作 | 期待結果 | 備考 |
|---|---|---|---|---|---|---|
| AET-030 | 異常系 | 機能1 | party.json 構文不正で起動中断・エラー表示 | mockPartyInvalidJson で不正 JSON を返す | `#error-message` が visible で「party.json の形式が正しくありません」を含む、`.pokemon-card` 0 枚 | 元 MET-024 |
| AET-031 | 異常系 | 機能1 | species フィールド欠落 | `partyMissingField('species')` で species を欠落 | 起動中断、エラー表示、カード 0 枚 | 元 MET-025。LD-006 の UI 結合担保 |
| AET-032 | 異常系 | 機能1 | nature フィールド欠落 | `partyMissingField('nature')` | 起動中断、エラー表示、カード 0 枚 | 元 MET-025b。LD-007 の UI 結合担保 |
| AET-033 | 異常系 | 機能1 | abilityPoints フィールド欠落 | `partyMissingField('abilityPoints')` | 起動中断、エラー表示、カード 0 枚 | 元 MET-025c。LD-008 の UI 結合担保 |
| AET-034 | 異常系 | 機能1 | moves フィールド欠落 | `partyMissingField('moves')` | 起動中断、エラー表示、カード 0 枚 | 元 MET-025d。LD-009 の UI 結合担保 |
