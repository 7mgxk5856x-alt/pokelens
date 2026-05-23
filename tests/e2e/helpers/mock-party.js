/**
 * party.json の fetch リクエストを Playwright で介入し、テスト固有の party データを返す。
 * 各テストの beforeEach で `await mockParty(page, ...)` を呼び出した後 `page.goto('/')` する。
 */
export async function mockParty(page, partyData) {
  await page.route('**/data/party.json', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(partyData),
    })
  );
}

/** JSON 構文不正を返す（party.json 異常系用）。 */
export async function mockPartyInvalidJson(page) {
  await page.route('**/data/party.json', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: '{this is not valid json',
    })
  );
}

/** 任意のレスポンスステータス・本文で返す（404 等の異常系用、現時点では未使用の予約）。 */
export async function mockPartyRaw(page, { status, body }) {
  await page.route('**/data/party.json', (route) =>
    route.fulfill({ status, contentType: 'application/json', body })
  );
}
