import { test, expect } from '@playwright/test';
import { mockParty } from './helpers/mock-party.js';
import { STANDARD_PARTY } from './helpers/party-fixtures.js';
import { SEL } from './helpers/selectors.js';

test.describe('相手ポケモン情報・素早さ 6 パターン', () => {
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

  test('AET-028: 素早さ 6 パターン表示（元 MET-023）', async ({ page }) => {
    const firstSlot = page.locator(SEL.opponentCards).first();
    const input = firstSlot.locator(SEL.oppInput);
    await input.fill('ガブ');
    await input.press('Enter');

    const items = page.locator(SEL.speedPatterns);
    await expect(items).toHaveCount(6);

    const expectedLabels = ['最速スカーフ', '準速スカーフ', '最速', '準速', '無振り', '最遅'];
    for (let i = 0; i < 6; i++) {
      const labelText = await items.nth(i).locator('.label').textContent();
      expect(labelText).toContain(expectedLabels[i]);
      const valueText = await items.nth(i).locator('.value').textContent();
      // 数値が表示される
      expect(parseInt(valueText || '0', 10)).toBeGreaterThan(0);
    }
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
