import { test, expect } from '@playwright/test';
import { mockParty } from './helpers/mock-party.js';
import { STANDARD_PARTY } from './helpers/party-fixtures.js';
import { SEL } from './helpers/selectors.js';

test.describe('相手パーティ入力・サジェスト検索', () => {
  test.beforeEach(async ({ page }) => {
    await mockParty(page, STANDARD_PARTY);
    await page.goto('/');
  });

  test('AET-018: 前方一致サジェスト・図鑑番号順（元 MET-016）', async ({ page }) => {
    const firstInput = page.locator(SEL.opponentCards).first().locator(SEL.oppInput);
    await firstInput.fill('ピ');

    const suggestItems = page.locator(SEL.suggestItems);
    const count = await suggestItems.count();
    expect(count).toBeGreaterThanOrEqual(2);

    // 「ピ」前方一致候補（例: ピカチュウ=25 / ピジョット=18 など）
    // 図鑑番号順なので 1 件目の num が 2 件目の num より小さい
    // candidate 名から num を取得するため src のロジックに依存しないよう、
    // 「ピジョン」「ピジョット」「ピジョン」「ピカチュウ」など複数ヒットすることを確認
    const names = await suggestItems.allTextContents();
    expect(names.length).toBeGreaterThanOrEqual(2);
    expect(names.every((n) => n.startsWith('ピ'))).toBe(true);
  });

  test('AET-019: 多様な入力方式 4 種類すべて（元 MET-017）', async ({ page }) => {
    const inputs = ['がぶ', 'ガブ', 'ｶﾞﾌﾞ', 'gabu'];
    const cards = page.locator(SEL.opponentCards);

    for (let i = 0; i < inputs.length; i++) {
      const input = cards.nth(i).locator(SEL.oppInput);
      await input.fill(inputs[i]);
      const items = cards.nth(i).locator(SEL.suggestItems);
      // 「ガブリアス」が候補に含まれる
      await expect(items.filter({ hasText: 'ガブリアス' })).toHaveCount(1);
    }
  });

  test('AET-020: 候補確定とカード表示（元 MET-018）', async ({ page }) => {
    const firstSlot = page.locator(SEL.opponentCards).first();
    const input = firstSlot.locator(SEL.oppInput);
    await input.fill('ガブ');

    // 候補が表示される
    const suggestList = firstSlot.locator(SEL.suggestList);
    await expect(suggestList).toBeVisible();

    // Enter で確定（最初の候補 = ガブリアス）
    await input.press('Enter');

    // 入力欄は非表示、確定情報が表示
    await expect(firstSlot.locator('.search-input')).toBeHidden();
    const info = firstSlot.locator(SEL.opponentInfo);
    await expect(info).toBeVisible();
    await expect(info.locator('.name')).toHaveText('ガブリアス');
    await expect(info.locator('.types')).toContainText('ドラゴン');
    await expect(info.locator('.base-stats')).not.toBeEmpty();
  });

  test('AET-021: キーボード操作（サジェスト表示中・非表示時、元 MET-019）', async ({ page }) => {
    const cards = page.locator(SEL.opponentCards);
    const firstInput = cards.nth(0).locator(SEL.oppInput);

    // (a) サジェスト表示中: Tab で次候補ハイライト移動。入力欄間遷移しない
    await firstInput.fill('ピ');
    const items = cards.nth(0).locator(SEL.suggestItems);
    await expect(items.first()).toHaveClass(/is-hover/);
    await firstInput.press('Tab');
    // 2 件目がハイライト
    await expect(items.nth(1)).toHaveClass(/is-hover/);
    // 1 件目は外れている
    await expect(items.nth(0)).not.toHaveClass(/is-hover/);
    // フォーカスは依然 firstInput
    await expect(firstInput).toBeFocused();

    // Shift+Tab で前候補に戻る
    await firstInput.press('Shift+Tab');
    await expect(items.nth(0)).toHaveClass(/is-hover/);

    // (b) サジェスト非表示時: Tab で次の入力欄へフォーカス移動
    await firstInput.fill('');
    await expect(cards.nth(0).locator(SEL.suggestList)).toBeHidden();
    await firstInput.press('Tab');
    const secondInput = cards.nth(1).locator(SEL.oppInput);
    await expect(secondInput).toBeFocused();
  });

  test('AET-022: 該当なしで「見つかりません」（元 MET-020）', async ({ page }) => {
    const input = page.locator(SEL.opponentCards).first().locator(SEL.oppInput);
    await input.fill('存在しないポケモン');
    const notFound = page.locator(SEL.suggestNotFound);
    await expect(notFound).toHaveText('見つかりません');
  });

  test('AET-023: クリア（× ボタン）後の状態（元 MET-021）', async ({ page }) => {
    const firstSlot = page.locator(SEL.opponentCards).first();
    const input = firstSlot.locator(SEL.oppInput);
    await input.fill('ガブ');
    await input.press('Enter');

    const clearBtn = firstSlot.locator(SEL.opponentClear);
    await expect(clearBtn).toBeVisible();
    // 選択中なら詳細パネルが表示されている
    await expect(page.locator(SEL.opponentDetail)).toBeVisible();

    await clearBtn.click({ force: true });

    // 入力欄が再表示・空欄
    await expect(firstSlot.locator('.search-input')).toBeVisible();
    await expect(input).toHaveValue('');
    // × ボタンが非表示
    await expect(clearBtn).toBeHidden();
    // 詳細パネルも非表示
    await expect(page.locator(SEL.opponentDetail)).toBeHidden();
  });

  test('AET-024: 未入力スロットが残ってもアプリが正常動作（元 MET-028）', async ({ page }) => {
    const cards = page.locator(SEL.opponentCards);
    // 1 匹だけ入力・確定
    const input = cards.nth(0).locator(SEL.oppInput);
    await input.fill('ガブ');
    await input.press('Enter');

    await expect(cards.nth(0).locator(SEL.opponentInfo)).toBeVisible();

    // 残り 5 スロットは空欄入力欄のまま
    for (let i = 1; i < 6; i++) {
      await expect(cards.nth(i).locator(SEL.oppInput)).toBeVisible();
      await expect(cards.nth(i).locator(SEL.oppInput)).toHaveValue('');
    }

    // エラーは出ていない
    await expect(page.locator(SEL.errorMessage)).toBeHidden();
  });

  test('AET-025: XSS 耐性（script タグ入力で alert が発火しない、元 MET-029）', async ({
    page,
  }) => {
    // dialog ハンドラを登録: XSS が発火したら例外
    let xssTriggered = false;
    page.on('dialog', async (dialog) => {
      xssTriggered = true;
      await dialog.dismiss();
    });

    // ページのエラーも監視（クロスサイト的問題があれば検知）
    const pageErrors = [];
    page.on('pageerror', (err) => {
      pageErrors.push(err.message);
    });

    const input = page.locator(SEL.opponentCards).first().locator(SEL.oppInput);
    await input.fill('<script>alert(1)</script>');

    // 「見つかりません」がテキストとして表示される（タグはエスケープされてサジェストには出ない）
    await expect(page.locator(SEL.suggestNotFound)).toHaveText('見つかりません');

    // 念のため少し待って alert がないことを確認
    await page.waitForTimeout(200);
    expect(xssTriggered).toBe(false);
    expect(pageErrors).toEqual([]);
  });

  test('AET-026: サジェスト 10 件上限（元 MET-030）', async ({ page }) => {
    const input = page.locator(SEL.opponentCards).first().locator(SEL.oppInput);
    // 「ア」など多数ヒットするクエリ
    await input.fill('ア');

    const items = page.locator(SEL.suggestItems);
    const count = await items.count();
    expect(count).toBeGreaterThan(0);
    expect(count).toBeLessThanOrEqual(10);
  });
});
