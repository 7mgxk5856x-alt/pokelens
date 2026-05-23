import { test, expect } from '@playwright/test';
import { mockParty } from './helpers/mock-party.js';
import {
  STANDARD_PARTY,
  UNKNOWN_SPECIES_FIXTURE,
  NATURE_MIX,
} from './helpers/party-fixtures.js';
import { SEL } from './helpers/selectors.js';

test.describe('自分パーティ表示', () => {
  test('AET-001: 起動時に 6 枚カード表示（元 MET-004）', async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');

    const cards = page.locator(SEL.ownCards);
    await expect(cards).toHaveCount(6);

    for (let i = 0; i < 6; i++) {
      const card = cards.nth(i);
      await expect(card.locator('.name')).not.toBeEmpty();
      await expect(card.locator('.types')).not.toBeEmpty();
      await expect(card.locator('.base-stats')).not.toBeEmpty();
    }
  });

  test('AET-002: 初期は未選択・詳細非表示（元 MET-005）', async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');

    const selected = page.locator(`${SEL.ownCards}.selected`);
    await expect(selected).toHaveCount(0);

    // 詳細ビューは display:none 状態
    const detail = page.locator(SEL.ownDetail);
    await expect(detail).toBeHidden();
  });

  test('AET-003: カード選択で詳細表示（元 MET-006）', async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');

    const firstCard = page.locator(SEL.ownCards).first();
    await firstCard.click({ force: true });

    // 選択状態
    await expect(firstCard).toHaveClass(/selected/);

    // 詳細ビューが表示される
    const detail = page.locator(SEL.ownDetail);
    await expect(detail).toBeVisible();

    // 主要要素の存在
    await expect(detail.locator('.detail-header .name')).not.toBeEmpty();
    await expect(detail.locator('.detail-header .types')).not.toBeEmpty();
    await expect(detail.locator('.detail-moves')).toBeVisible();

    // 特性 / 持ち物 / 性格 / 種族値 / 実数値の各行
    const rowTexts = await detail.locator('.detail-row, .detail-stats').allTextContents();
    expect(rowTexts.some((t) => t.startsWith('特性:'))).toBe(true);
    expect(rowTexts.some((t) => t.startsWith('持ち物:'))).toBe(true);
    expect(rowTexts.some((t) => t.startsWith('性格:'))).toBe(true);
    expect(rowTexts.some((t) => t.startsWith('種族値:'))).toBe(true);
    expect(rowTexts.some((t) => t.startsWith('実数値:'))).toBe(true);
  });

  test('AET-004: 未知 species で「不明なポケモン: ...」表示、他カード正常（元 MET-007）', async ({
    page,
  }) => {
    await mockParty(page, UNKNOWN_SPECIES_FIXTURE);
    await page.goto('/');

    const cards = page.locator(SEL.ownCards);
    await expect(cards).toHaveCount(6);

    // 2 枚目（index 1）は不明な species
    const unknownCard = cards.nth(1);
    await expect(unknownCard.locator('.error')).toContainText('不明なポケモン: まぼろしポケモン');

    // 他カード（1 枚目）は正常表示
    const okCard = cards.nth(0);
    await expect(okCard.locator('.name')).toHaveText('ガブリアス');
    await expect(okCard.locator('.types')).not.toBeEmpty();
    await expect(okCard.locator('.base-stats')).not.toBeEmpty();
  });

  test('AET-005: party.json 変更後リロードで反映（元 MET-026）', async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');
    await expect(page.locator(SEL.ownCards).first().locator('.name')).toHaveText('ガブリアス');

    // route 設定を更新（既存 route を解除して新規 fixture を設定）
    await page.unroute('**/data/party.json');
    await mockParty(page, NATURE_MIX);
    await page.reload();

    await expect(page.locator(SEL.ownCards)).toHaveCount(2);
    await expect(page.locator(SEL.ownCards).nth(0).locator('.name')).toHaveText('ガブリアス');
    await expect(page.locator(SEL.ownCards).nth(1).locator('.name')).toHaveText('ガブリアス');
  });

  test('AET-006: 別ポケモン選択時の詳細表示切替（元 MET-034）', async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');

    const cards = page.locator(SEL.ownCards);
    await cards.nth(0).click({ force: true });
    const detail = page.locator(SEL.ownDetail);
    await expect(detail).toBeVisible();
    await expect(detail.locator('.detail-header .name')).toHaveText('ガブリアス');

    await cards.nth(1).click({ force: true });
    await expect(detail.locator('.detail-header .name')).toHaveText('ピカチュウ');

    // 1 枚目の名前は残らない
    await expect(detail.locator('.detail-header .name')).not.toHaveText('ガブリアス');
    // 1 枚目の選択状態は解除されている
    await expect(cards.nth(0)).not.toHaveClass(/selected/);
    await expect(cards.nth(1)).toHaveClass(/selected/);
  });
});
