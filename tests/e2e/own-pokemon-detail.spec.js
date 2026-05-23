import { test, expect } from '@playwright/test';
import { mockParty } from './helpers/mock-party.js';
import {
  STANDARD_PARTY,
  GARCHOMP_PHYSICAL,
  GARCHOMP_SPECIAL,
  NATURE_MIX,
  MOVE_VARIANTS_FIXTURE,
  MULTIHIT_FIXTURE,
  IRONFIST_FIXTURE,
} from './helpers/party-fixtures.js';
import { SEL } from './helpers/selectors.js';

test.describe('自分ポケモン詳細・火力指数', () => {
  test('AET-007: 基本情報の全項目表示（元 MET-008）', async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    const detail = page.locator(SEL.ownDetail);
    await expect(detail.locator('.detail-header .name')).toHaveText('ガブリアス');
    await expect(detail.locator('.detail-header .types')).toContainText('ドラゴン');

    const rows = await detail.locator('.detail-row, .detail-stats').allTextContents();
    expect(rows.some((t) => t.startsWith('特性:'))).toBe(true);
    expect(rows.some((t) => t.startsWith('持ち物:'))).toBe(true);
    expect(rows.some((t) => t.startsWith('性格:'))).toBe(true);
    expect(rows.some((t) => t.startsWith('種族値:'))).toBe(true);
    expect(rows.some((t) => t.startsWith('実数値:'))).toBe(true);
  });

  test('AET-008: 性格の上昇/下降表記（補正あり・補正なし、元 MET-009）', async ({ page }) => {
    await mockParty(page, NATURE_MIX);
    await page.goto('/');

    const cards = page.locator(SEL.ownCards);
    const detail = page.locator(SEL.ownDetail);

    // 補正あり: いじっぱり (A↑ / C↓)
    await cards.nth(0).click({ force: true });
    const natureWithMod = await detail
      .locator('.detail-row', { hasText: '性格:' })
      .textContent();
    expect(natureWithMod).toContain('いじっぱり');
    expect(natureWithMod).toContain('A↑');
    expect(natureWithMod).toContain('C↓');

    // 補正なし: まじめ (補正なし)
    await cards.nth(1).click({ force: true });
    const natureNoMod = await detail
      .locator('.detail-row', { hasText: '性格:' })
      .textContent();
    expect(natureNoMod).toContain('まじめ');
    expect(natureNoMod).toContain('(補正なし)');
  });

  test('AET-009: 技一覧の表示（元 MET-010）', async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    const headers = await page.locator(`${SEL.ownMovesTable} thead th`).allTextContents();
    expect(headers).toEqual(['技名', 'タイプ', '威力', '分類', '命中', '火力指数']);

    const rows = page.locator(SEL.ownMoveRows);
    await expect(rows).toHaveCount(4);
  });

  test('AET-010: 物理技の火力指数 = 30000（元 MET-011）', async ({ page }) => {
    await mockParty(page, GARCHOMP_PHYSICAL);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    // じしん（威力100, じめん, 物理） × atk実数値200 × STAB1.5 = 30000
    const jishinRow = page
      .locator(SEL.ownMoveRows)
      .filter({ hasText: 'じしん' })
      .first();
    await expect(jishinRow.locator('td.power-index')).toHaveText('30000');
  });

  test('AET-011: 変化技の火力指数欄が「−」（元 MET-012）', async ({ page }) => {
    await mockParty(page, MOVE_VARIANTS_FIXTURE);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    const statusRow = page
      .locator(SEL.ownMoveRows)
      .filter({ hasText: 'つるぎのまい' })
      .first();
    await expect(statusRow.locator('td.power-index')).toHaveText('−');
  });

  test('AET-012: 威力不定技の威力欄が「−」（元 MET-013）', async ({ page }) => {
    await mockParty(page, MOVE_VARIANTS_FIXTURE);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    const counterRow = page
      .locator(SEL.ownMoveRows)
      .filter({ hasText: 'カウンター' })
      .first();
    // 威力欄（3 列目）が「−」
    const cells = counterRow.locator('td');
    await expect(cells.nth(2)).toHaveText('−');
    // 火力指数も「−」（パッチ未定義なので）
    await expect(counterRow.locator('td.power-index')).toHaveText('−');
  });

  test('AET-013: 必中技の命中率欄が「−」（元 MET-014）', async ({ page }) => {
    await mockParty(page, MOVE_VARIANTS_FIXTURE);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    const tsubameRow = page
      .locator(SEL.ownMoveRows)
      .filter({ hasText: 'つばめがえし' })
      .first();
    // 命中率欄（5 列目）が「−」
    const cells = tsubameRow.locator('td');
    await expect(cells.nth(4)).toHaveText('−');
  });

  test('AET-014: multihit 最大総威力 = 75（元 MET-015）', async ({ page }) => {
    await mockParty(page, MULTIHIT_FIXTURE);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    const oufukuRow = page
      .locator(SEL.ownMoveRows)
      .filter({ hasText: 'おうふくビンタ' })
      .first();
    // 威力欄（3 列目）が 75（=15×5、最大総威力）
    await expect(oufukuRow.locator('td').nth(2)).toHaveText('75');
  });

  test('AET-015: タイプ名・技分類の日本語変換（元 MET-027）', async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');

    // カード上のタイプ表示
    const garchompCard = page.locator(SEL.ownCards).first();
    await expect(garchompCard.locator('.types')).toContainText('ドラゴン');
    await expect(garchompCard.locator('.types')).toContainText('じめん');

    // 詳細の技分類欄
    await garchompCard.click({ force: true });
    const moveRows = page.locator(SEL.ownMoveRows);
    const jishinRow = moveRows.filter({ hasText: 'じしん' }).first();
    // 分類欄（4 列目）が「物理」
    await expect(jishinRow.locator('td').nth(3)).toHaveText('物理');

    const migawariRow = moveRows.filter({ hasText: 'みがわり' }).first();
    // 変化技の分類は「変化」
    await expect(migawariRow.locator('td').nth(3)).toHaveText('変化');
  });

  test('AET-016: てつのこぶし条件付き補正（パンチ技のみ 1.2 倍、元 MET-031）', async ({
    page,
  }) => {
    await mockParty(page, IRONFIST_FIXTURE);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    const machRow = page.locator(SEL.ownMoveRows).filter({ hasText: 'マッハパンチ' }).first();
    const bakajikaraRow = page
      .locator(SEL.ownMoveRows)
      .filter({ hasText: 'ばかぢから' })
      .first();

    const machPower = parseInt(
      (await machRow.locator('td.power-index').textContent()) || '0',
      10
    );
    const bakajikaraPower = parseInt(
      (await bakajikaraRow.locator('td.power-index').textContent()) || '0',
      10
    );

    // 両方とも格闘タイプ→ STAB 補正は同一（ゴウカザル は 火/格闘）
    // パンチ技は てつのこぶし 1.2 倍補正、非パンチ技は補正なし
    // よって マッハパンチ威力40・ばかぢから威力120 → power-index 比率の差から判定
    // マッハパンチ: 40 × atk × STAB(1.5) × 1.2  = 72 × atk
    // ばかぢから:  120 × atk × STAB(1.5)        = 180 × atk
    // 比率: 72 / 180 = 0.4。両方とも 1.2 倍が乗ったとしたら同じ比率にはならない
    // 明示確認: マッハパンチの power-index は 40 × atk × 1.5 × 1.2 / 1（ばかぢからの 1 倍補正）と比較
    // 確認手段: マッハパンチ power-index ÷ 40 と ばかぢから power-index ÷ 120 の比率
    const machPerPower = machPower / 40;
    const bakajikaraPerPower = bakajikaraPower / 120;
    // マッハパンチは 1.2 倍補正があるので、ratio は 1.2 倍大きい
    const ratio = machPerPower / bakajikaraPerPower;
    expect(ratio).toBeCloseTo(1.2, 1);
  });

  test('AET-017: 特殊技の火力指数 = 17550（元 MET-033）', async ({ page }) => {
    await mockParty(page, GARCHOMP_SPECIAL);
    await page.goto('/');
    await page.locator(SEL.ownCards).first().click({ force: true });

    // りゅうせいぐん（威力130, ドラゴン, 特殊）× spa実数値90 × STAB1.5 = 17550
    const ryuseiRow = page
      .locator(SEL.ownMoveRows)
      .filter({ hasText: 'りゅうせいぐん' })
      .first();
    await expect(ryuseiRow.locator('td.power-index')).toHaveText('17550');
  });
});
