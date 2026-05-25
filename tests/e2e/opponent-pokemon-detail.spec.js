import { test, expect } from '@playwright/test';
import { mockParty } from './helpers/mock-party.js';
import { STANDARD_PARTY } from './helpers/party-fixtures.js';
import { SEL } from './helpers/selectors.js';

test.describe('相手ポケモン情報・素早さ 4 パターン', () => {
  test.beforeEach(async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');
  });

  test('AET-027: 基本情報・特性候補の表示（元 MET-022）', async ({ page }) => {
    const firstSlot = page.locator(SEL.opponentCards).first();
    const input = firstSlot.locator(SEL.oppInput);
    await input.fill('ガブ');
    await input.press('Enter');

    const detail = page.locator(SEL.opponentDetail);
    await expect(detail).toBeVisible();
    await expect(detail.locator('.detail-header .name')).toHaveText('ガブリアス');
    await expect(detail.locator('.detail-header .types')).toContainText('ドラゴン');
    await expect(detail.locator('.detail-header .types')).toContainText('じめん');

    // 特性候補（隠れ特性含む全特性が一覧表示）
    const abilityRow = detail.locator('.detail-row').filter({ hasText: '特性候補:' });
    const abilityText = (await abilityRow.textContent()) || '';
    // pokedex.json のデータ上は「さめはだ」「すながくれ」が含まれる
    expect(abilityText).toContain('さめはだ');
    expect(abilityText).toContain('すながくれ');

    // 種族値表示
    const statsText = (await detail.locator('.detail-stats').first().textContent()) || '';
    expect(statsText).toContain('H 108');
    expect(statsText).toContain('S 102');
  });

  test('AET-028: 素早さ 4 パターンを表形式・横並びで表示する', async ({ page }) => {
    const firstSlot = page.locator(SEL.opponentCards).first();
    const input = firstSlot.locator(SEL.oppInput);
    await input.fill('ガブ');
    await input.press('Enter');

    // 表ヘッダ: 左から「最速」「準速」「無振り」「最遅」の順
    const headers = page.locator(SEL.speedPatternHeaders);
    await expect(headers).toHaveCount(4);
    const headerTexts = await headers.allTextContents();
    expect(headerTexts).toEqual(['最速', '準速', '無振り', '最遅']);

    // データ行: ガブリアス（種族値102）の 4 実数値
    //   最速 = floor((102+32+20)*1.1) = 169
    //   準速 = (102+32+20)*1.0       = 154
    //   無振り = (102+0+20)*1.0      = 122
    //   最遅 = floor((102+0+20)*0.9) = 109
    const cells = page.locator(SEL.speedPatternCells);
    await expect(cells).toHaveCount(4);
    const cellTexts = await cells.allTextContents();
    expect(cellTexts).toEqual(['169', '154', '122', '109']);
  });

  test('AET-038: 相手側に耐久指数 4 パターン × 2 種類の表を表示する', async ({ page }) => {
    // ガブリアス（H108/B95/D85）の 4 パターン耐久指数:
    //   B32+↑ = floor((95+32+20)×1.1) = 161、B0 = 115
    //   D32+↑ = floor((85+32+20)×1.1) = 150、D0 = 105
    //   H32 = 215、H0 = 183（HP は性格補正対象外）
    //   特化: 215×161=34615 / 215×150=32250
    //   極振: 183×161=29463 / 183×150=27450
    //   H極振: 215×115=24725 / 215×105=22575
    //   無振り: 183×115=21045 / 183×105=19215
    const firstSlot = page.locator(SEL.opponentCards).first();
    const input = firstSlot.locator(SEL.oppInput);
    await input.fill('ガブ');
    await input.press('Enter');

    // 列ヘッダ: [空] + 「耐久特化 / 耐久極振 / H極振 / 無振り」 = 5 列
    const headers = page.locator(SEL.enduranceHeaders);
    await expect(headers).toHaveCount(5);
    const headerTexts = await headers.allTextContents();
    expect(headerTexts).toEqual(['', '耐久特化', '耐久極振', 'H極振', '無振り']);

    // 行ヘッダ: 上から「物理耐久指数 / 特殊耐久指数」
    const rowHeaders = page.locator(SEL.enduranceRowHeaders);
    await expect(rowHeaders).toHaveCount(2);
    const rowHeaderTexts = await rowHeaders.allTextContents();
    expect(rowHeaderTexts).toEqual(['物理耐久指数', '特殊耐久指数']);

    // データセル: 2 行 × 4 列 = 8 セル、上段=物理、下段=特殊、左から特化→極振→H極振→無振り
    const cells = page.locator(SEL.enduranceCells);
    await expect(cells).toHaveCount(8);
    const cellTexts = await cells.allTextContents();
    expect(cellTexts).toEqual([
      '34615', '29463', '24725', '21045',
      '32250', '27450', '22575', '19215',
    ]);
  });

  test('AET-041: 相手側メガ切替ボタンはメガシンカ可能なポケモンに常時表示・切替後に詳細パネルが連動更新', async ({
    page,
  }) => {
    // メガシンカ可能（リザードン）と不可（ガブリアス）で切替ボタン表示を比較
    // リザードンは複数メガ（X/Y）あり、通常 → メガリザードンＸ → メガリザードンＹ → 通常 の循環を確認
    const slots = page.locator(SEL.opponentCards);

    // スロット 0 で リザードン を確定（前方一致 "リザード" は charmeleon (リザード) もヒットするためフル名で確定）
    const input0 = slots.nth(0).locator(SEL.oppInput);
    await input0.fill('リザードン');
    await input0.press('Enter');

    // メガ切替ボタンがスロット 0 に表示される（持ち物に依存しない）
    const toggle0 = slots.nth(0).locator('.mega-toggle');
    await expect(toggle0).toHaveCount(1);

    // 詳細パネルがリザードンを表示
    const detail = page.locator(SEL.opponentDetail);
    await expect(detail.locator('.detail-header .name')).toHaveText('リザードン');

    // 切替: メガリザードンＸ → 詳細パネルも連動
    await toggle0.click({ force: true });
    await expect(slots.nth(0).locator('.opponent-info .name')).toHaveText('メガリザードンＸ');
    await expect(detail.locator('.detail-header .name')).toHaveText('メガリザードンＸ');

    // 切替: メガリザードンＹ
    await toggle0.click({ force: true });
    await expect(detail.locator('.detail-header .name')).toHaveText('メガリザードンＹ');

    // 切替: 通常へ循環
    await toggle0.click({ force: true });
    await expect(detail.locator('.detail-header .name')).toHaveText('リザードン');

    // スロット 1 で メタモン を確定（Champions データでメガシンカ非対応のポケモン。
    // ガブリアス は Champions ではメガシンカ可能 (メガストーン: ガブリアスナイト) のため非メガの代表として使えない）
    const input1 = slots.nth(1).locator(SEL.oppInput);
    await input1.fill('メタモン');
    await input1.press('Enter');
    // メガ切替ボタンは表示されない
    await expect(slots.nth(1).locator('.mega-toggle')).toHaveCount(0);
  });

  test('AET-045: 相手側メガレックウザはメガストーン不要メガとして循環対象に含まれる（item: null メガ）', async ({
    page,
  }) => {
    // 相手側は元々持ち物未知で全形態を循環するため、megaForms[].item === null メガ（メガレックウザ）も
    // 通常 → メガレックウザ → 通常 で循環する。AET-041 のリザードン（megaStone 経由メガ）と
    // 同等の挙動になることを担保する。
    const slots = page.locator(SEL.opponentCards);

    const input = slots.nth(0).locator(SEL.oppInput);
    await input.fill('レックウザ');
    await input.press('Enter');

    const toggle = slots.nth(0).locator('.mega-toggle');
    await expect(toggle).toHaveCount(1);

    const detail = page.locator(SEL.opponentDetail);
    await expect(detail.locator('.detail-header .name')).toHaveText('レックウザ');

    await toggle.click({ force: true });
    await expect(slots.nth(0).locator('.opponent-info .name')).toHaveText('メガレックウザ');
    await expect(detail.locator('.detail-header .name')).toHaveText('メガレックウザ');

    await toggle.click({ force: true });
    await expect(detail.locator('.detail-header .name')).toHaveText('レックウザ');
  });

  test('AET-029: 相手ポケモン切替時の詳細表示切替（元 MET-032）', async ({ page }) => {
    const cards = page.locator(SEL.opponentCards);

    // 1 匹目: ガブリアス
    const input1 = cards.nth(0).locator(SEL.oppInput);
    await input1.fill('ガブ');
    await input1.press('Enter');

    const detail = page.locator(SEL.opponentDetail);
    await expect(detail.locator('.detail-header .name')).toHaveText('ガブリアス');

    // 2 匹目: ピカチュウ
    const input2 = cards.nth(1).locator(SEL.oppInput);
    await input2.fill('ピカ');
    await input2.press('Enter');

    await expect(detail.locator('.detail-header .name')).toHaveText('ピカチュウ');
    // 1 匹目の情報は残らない
    await expect(detail.locator('.detail-header .name')).not.toHaveText('ガブリアス');
  });
});
