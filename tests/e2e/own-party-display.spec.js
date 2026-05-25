import { test, expect } from '@playwright/test';
import { mockParty } from './helpers/mock-party.js';
import {
  STANDARD_PARTY,
  UNKNOWN_SPECIES_FIXTURE,
  NATURE_MIX,
  MEGA_FIXTURE,
  RAYQUAZA_FIXTURE,
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

  test('AET-039: 自分側メガ切替ボタンは対応メガストーン持ちのときだけ表示・切替後にカード表示が更新される', async ({
    page,
  }) => {
    // MEGA_FIXTURE: 0=フシギバナ+フシギバナイト, 1=フシギバナ+カメックスナイト,
    //              2=フシギバナ+こだわりハチマキ, 3=リザードン+リザードナイトＸ,
    //              4=ガブリアス+こだわりスカーフ, 5=リザードン+持ち物なし
    await mockParty(page, MEGA_FIXTURE);
    await page.goto('/');

    const cards = page.locator(SEL.ownCards);

    // 0: フシギバナ + フシギバナイト → ボタン表示
    await expect(cards.nth(0).locator('.mega-toggle')).toHaveCount(1);
    // 1: フシギバナ + カメックスナイト → ボタン非表示（対応アイテム不一致）
    await expect(cards.nth(1).locator('.mega-toggle')).toHaveCount(0);
    // 2: フシギバナ + こだわりハチマキ → ボタン非表示（メガストーンでない）
    await expect(cards.nth(2).locator('.mega-toggle')).toHaveCount(0);
    // 3: リザードン + リザードナイトＸ → ボタン表示
    await expect(cards.nth(3).locator('.mega-toggle')).toHaveCount(1);
    // 4: ガブリアス → ボタン非表示（メガ不可ポケモン）
    await expect(cards.nth(4).locator('.mega-toggle')).toHaveCount(0);
    // 5: リザードン + 持ち物なし → ボタン非表示
    await expect(cards.nth(5).locator('.mega-toggle')).toHaveCount(0);

    // フシギバナを切替: 通常 "フシギバナ" → メガ "メガフシギバナ"
    await expect(cards.nth(0).locator('.name')).toHaveText('フシギバナ');
    await cards.nth(0).locator('.mega-toggle').click({ force: true });
    await expect(cards.nth(0).locator('.name')).toHaveText('メガフシギバナ');
    // 再度押して通常に戻る
    await cards.nth(0).locator('.mega-toggle').click({ force: true });
    await expect(cards.nth(0).locator('.name')).toHaveText('フシギバナ');
  });

  test('AET-042: 選択済みカードでメガ切替時、自分ポケモン詳細パネルが連動して再描画される', async ({
    page,
  }) => {
    await mockParty(page, MEGA_FIXTURE);
    await page.goto('/');

    const cards = page.locator(SEL.ownCards);
    const detail = page.locator(SEL.ownDetail);

    // フシギバナカード（index 0）を選択
    await cards.nth(0).click({ force: true });
    await expect(detail.locator('.detail-header .name')).toHaveText('フシギバナ');
    // 通常特性「しんりょく」が詳細パネルに表示される
    await expect(detail.locator('.detail-row', { hasText: '特性:' })).toContainText('しんりょく');

    // メガ切替を押す
    await cards.nth(0).locator('.mega-toggle').click({ force: true });
    // 詳細パネルの名前・特性がメガフォームのものに更新される（メガフシギバナ・特性「あついしぼう」）
    await expect(detail.locator('.detail-header .name')).toHaveText('メガフシギバナ');
    await expect(detail.locator('.detail-row', { hasText: '特性:' })).toContainText('あついしぼう');
  });

  test('AET-043: メガストーン不要メガ（メガレックウザ）は持ち物に関わらず自分側でも切替ボタンが表示される', async ({
    page,
  }) => {
    // メガストーン不要メガ (megaForms[].item === null) は持ち物の有無・種類に関わらず
    // 「メガシンカ発動条件と切替ボタン表示」が満たされる（PRD 機能 7）。
    // 簡略仕様のため、本ツールでは技習得（ガリョウテンセイ）チェックは行わない。
    await mockParty(page, RAYQUAZA_FIXTURE);
    await page.goto('/');

    const cards = page.locator(SEL.ownCards);

    // 0: レックウザ + いのちのたま → メガ切替ボタン表示
    await expect(cards.nth(0).locator('.mega-toggle')).toHaveCount(1);
    // 1: レックウザ + 持ち物なし → メガ切替ボタン表示（item: null フォールバック）
    await expect(cards.nth(1).locator('.mega-toggle')).toHaveCount(1);

    // 通常 → メガレックウザ → 通常 を循環
    await expect(cards.nth(0).locator('.name')).toHaveText('レックウザ');
    await cards.nth(0).locator('.mega-toggle').click({ force: true });
    await expect(cards.nth(0).locator('.name')).toHaveText('メガレックウザ');
    await cards.nth(0).locator('.mega-toggle').click({ force: true });
    await expect(cards.nth(0).locator('.name')).toHaveText('レックウザ');
  });

  test('AET-044: メガレックウザ切替時に自分ポケモン詳細パネルが連動更新される（item: null メガの詳細表示）', async ({
    page,
  }) => {
    await mockParty(page, RAYQUAZA_FIXTURE);
    await page.goto('/');

    const cards = page.locator(SEL.ownCards);
    const detail = page.locator(SEL.ownDetail);

    await cards.nth(0).click({ force: true });
    await expect(detail.locator('.detail-header .name')).toHaveText('レックウザ');
    await expect(detail.locator('.detail-row', { hasText: '特性:' })).toContainText('エアロック');

    await cards.nth(0).locator('.mega-toggle').click({ force: true });
    // メガレックウザの種族値・特性に連動更新（デルタストリーム）
    await expect(detail.locator('.detail-header .name')).toHaveText('メガレックウザ');
    await expect(detail.locator('.detail-row', { hasText: '特性:' })).toContainText(
      'デルタストリーム',
    );
  });

  test('AET-040: 自分側は持ち物に対応するメガのみ循環する（D-10: リザードン + リザードナイトＸ は 通常 ↔ メガリザードンＸ）', async ({ page }) => {
    // D-10「自分側のメガシンカ循環は持ち物にマッチするメガのみ」: メガリザードンＹ は循環に登場しない。
    // 相手側は持ち物未知のため全形態循環するが、自分側は持ち物既知でユーザーストーリー「正確に参照」を優先する。
    await mockParty(page, MEGA_FIXTURE);
    await page.goto('/');

    const card = page.locator(SEL.ownCards).nth(3); // リザードン + リザードナイトＸ
    const toggle = card.locator('.mega-toggle');

    await expect(card.locator('.name')).toHaveText('リザードン');
    await toggle.click({ force: true });
    await expect(card.locator('.name')).toHaveText('メガリザードンＸ');
    // もう一度押すと通常に戻る（メガリザードンＹ には到達しない）
    await toggle.click({ force: true });
    await expect(card.locator('.name')).toHaveText('リザードン');
    // さらに押すと再びメガリザードンＸ
    await toggle.click({ force: true });
    await expect(card.locator('.name')).toHaveText('メガリザードンＸ');
  });
});
