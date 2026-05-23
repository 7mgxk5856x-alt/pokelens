import { test, expect } from '@playwright/test';
import {
  mockParty,
  mockPartyInvalidJson,
} from './helpers/mock-party.js';
import { partyMissingField } from './helpers/party-fixtures.js';
import { SEL } from './helpers/selectors.js';

test.describe('異常系・データ検証', () => {
  test('AET-030: party.json 構文不正で起動中断・エラー表示（元 MET-024）', async ({ page }) => {
    await mockPartyInvalidJson(page);
    await page.goto('/');

    const errorEl = page.locator(SEL.errorMessage);
    await expect(errorEl).toBeVisible();
    await expect(errorEl).toContainText('party.json の形式が正しくありません');

    // パーティカードは描画されない
    await expect(page.locator(SEL.ownCards)).toHaveCount(0);
  });

  test('AET-031: species フィールド欠落（元 MET-025）', async ({ page }) => {
    await mockParty(page, partyMissingField('species'));
    await page.goto('/');

    const errorEl = page.locator(SEL.errorMessage);
    await expect(errorEl).toBeVisible();
    await expect(errorEl).toContainText('party.json の形式が正しくありません');
    await expect(page.locator(SEL.ownCards)).toHaveCount(0);
  });

  test('AET-032: nature フィールド欠落（元 MET-025b）', async ({ page }) => {
    await mockParty(page, partyMissingField('nature'));
    await page.goto('/');

    const errorEl = page.locator(SEL.errorMessage);
    await expect(errorEl).toBeVisible();
    await expect(errorEl).toContainText('party.json の形式が正しくありません');
    await expect(page.locator(SEL.ownCards)).toHaveCount(0);
  });

  test('AET-033: abilityPoints フィールド欠落（元 MET-025c）', async ({ page }) => {
    await mockParty(page, partyMissingField('abilityPoints'));
    await page.goto('/');

    const errorEl = page.locator(SEL.errorMessage);
    await expect(errorEl).toBeVisible();
    await expect(errorEl).toContainText('party.json の形式が正しくありません');
    await expect(page.locator(SEL.ownCards)).toHaveCount(0);
  });

  test('AET-034: moves フィールド欠落（元 MET-025d）', async ({ page }) => {
    await mockParty(page, partyMissingField('moves'));
    await page.goto('/');

    const errorEl = page.locator(SEL.errorMessage);
    await expect(errorEl).toBeVisible();
    await expect(errorEl).toContainText('party.json の形式が正しくありません');
    await expect(page.locator(SEL.ownCards)).toHaveCount(0);
  });
});
