import { searchByName as searchByNameImpl } from '../logic/name-search.js';

const REQUIRED_PARTY_FIELDS = ['species', 'nature', 'abilityPoints', 'moves'];

async function fetchJson(url, errorMessage) {
  const res = await fetch(url);
  if (!res.ok) {
    throw new Error(errorMessage);
  }
  try {
    return await res.json();
  } catch {
    throw new Error(errorMessage);
  }
}

// pokedex.json のメガフォームは親エントリの `megaForms[]` 配下にネストされている (機能 7、P0.5)。
// メガ名で逆引きする必要があるが pokedex.json のトップレベルには存在しないため、ローダー初期化時に
// 派生 Map を 1 回構築して O(1) ルックアップを実現する。
function buildMegaLocationByName(pokedex) {
  const map = new Map();
  for (const [parentKey, parent] of Object.entries(pokedex)) {
    const megaForms = parent.megaForms ?? [];
    for (let i = 0; i < megaForms.length; i++) {
      map.set(megaForms[i].name, { parentKey, megaIndex: i });
    }
  }
  return map;
}

/**
 * 全マスターデータ（pokedex / moves / items / abilities / 各種マスタ）とユーザーパーティを
 * data/ から読み込み、名前引き・修正子取得などの参照 API を提供する。
 */
export class DataLoader {
  #pokedex = null;
  #moves = null;
  #items = null;
  #abilities = null;
  #typeNames = null;
  #moveCategories = null;
  #natures = null;
  #megaLocationByName = new Map();

  /**
   * 全データファイルを読み込み、検証して内部に保持する。
   * @returns {Promise<object>} 読み込んだ全データ（pokedex, moves, items, abilities, typeNames, moveCategories, natures, userParty）
   * @throws {Error} データファイルが見つからない、または party.json の形式が不正な場合
   */
  async load() {
    const masterError = 'データファイルが見つかりません。C# ツールを実行してください';

    const [pokedex, moves, items, abilities] = await Promise.all([
      fetchJson('./data/pokedex.json', masterError),
      fetchJson('./data/moves.json', masterError),
      fetchJson('./data/items.json', masterError),
      fetchJson('./data/abilities.json', masterError),
    ]);

    const fetchStatic = (name) => {
      const msg = `${name} が見つかりません。リポジトリを確認してください`;
      return fetchJson(`./data/${name}`, msg);
    };
    const [typeNames, moveCategories, natures] = await Promise.all([
      fetchStatic('types.json'),
      fetchStatic('move-categories.json'),
      fetchStatic('natures.json'),
    ]);

    const partyRes = await fetch('./data/party.json');
    if (!partyRes.ok) {
      throw new Error('party.json が見つかりません');
    }
    let userParty;
    try {
      userParty = await partyRes.json();
    } catch {
      throw new Error('party.json の形式が正しくありません。JSONを確認してください');
    }

    if (!Array.isArray(userParty.party)) {
      throw new Error('party.json の形式が正しくありません。JSONを確認してください');
    }
    for (const entry of userParty.party) {
      for (const field of REQUIRED_PARTY_FIELDS) {
        if (entry[field] === undefined || entry[field] === null) {
          throw new Error('party.json の形式が正しくありません。JSONを確認してください');
        }
      }
    }

    this.#pokedex = pokedex;
    this.#moves = moves;
    this.#items = items;
    this.#abilities = abilities;
    this.#typeNames = typeNames;
    this.#moveCategories = moveCategories;
    this.#natures = natures;
    this.#megaLocationByName = buildMegaLocationByName(pokedex);

    return { pokedex, moves, items, abilities, typeNames, moveCategories, natures, userParty };
  }

  /**
   * 日本語名でポケモンを引く（通常状態のみ）。
   * メガフォーム名（例: 「メガフシギバナ」）は **null を返す**。これは PRD 機能 7 の
   * 「`party.json` の species にメガシンカ後の名前が指定された場合は不明なポケモン扱い」の実現と、
   * 「マスターデータ上、メガシンカ後は親に紐づくサブデータとして保持」の物理スキーマ表現のため。
   * メガフォーム名は pokedex.json のトップレベルに存在しないため、トップレベル走査だけで自然にこの仕様を満たす。
   * メガフォームの種族値・タイプ・特性を取得したい場合は `getMegaFormByName(name)` を使う。
   * @param {string} name ポケモンの日本語名
   * @returns {object|null} 一致する通常ポケモン。メガフォーム名または未登録名なら null
   */
  getPokemonByName(name) {
    if (!this.#pokedex) {
      return null;
    }
    return Object.values(this.#pokedex).find((e) => e.name === name) ?? null;
  }

  /**
   * メガフォーム名から、親エントリにネストされたメガデータを引く。メガシンカ状態の詳細表示で使用。
   * @param {string} megaName メガフォームの日本語名（例: 「メガフシギバナ」）
   * @returns {object|null} メガフォームのデータ（`{ key, name, item, types, baseStats, abilities }`）。メガフォーム名でない場合は null
   */
  getMegaFormByName(megaName) {
    const loc = this.#megaLocationByName.get(megaName);
    if (!loc || !this.#pokedex) {
      return null;
    }
    return this.#pokedex[loc.parentKey]?.megaForms?.[loc.megaIndex] ?? null;
  }

  /**
   * 親ポケモン名と持ち物名から、対応するメガフォームのデータを引く。
   * 自分側パーティの切替ボタン表示判定（PRD 260-264）に使用する。
   * @param {string} parentName 親ポケモンの日本語名（例: 「リザードン」）
   * @param {string} itemName 持ち物の日本語名（例: 「リザードナイトＸ」）
   * @returns {object|null} 一致するメガフォームのデータ。一致しなければ null
   */
  getMegaFormByItem(parentName, itemName) {
    const parent = this.getPokemonByName(parentName);
    return parent?.megaForms?.find((m) => m.item === itemName) ?? null;
  }

  /**
   * 名前がメガフォームかどうかを判定する。
   * @param {string} name ポケモン名
   * @returns {boolean} メガフォーム名なら true
   */
  isMegaForm(name) {
    return this.#megaLocationByName.has(name);
  }

  /**
   * 名前の前方一致でポケモンを検索する。
   * pokedex.json のトップレベルにはメガフォームが存在しないため、追加フィルタなしで自然にメガ除外される（機能 7）。
   * @param {string} query 検索クエリ
   * @returns {Array} 一致したポケモンエントリ（図鑑番号昇順）
   */
  searchByName(query) {
    if (!this.#pokedex) {
      return [];
    }
    return searchByNameImpl(query, Object.values(this.#pokedex));
  }

  getMove(name) {
    return this.#moves?.[name] ?? null;
  }

  getItemModifier(name) {
    const entry = this.#items?.[name];
    return entry?.modifier ?? null;
  }

  getAbilityModifier(name) {
    const entry = this.#abilities?.[name];
    return entry?.modifier ?? null;
  }

  getTypeName(type) {
    return this.#typeNames?.[type] ?? type;
  }

  getMoveCategory(cat) {
    return this.#moveCategories?.[cat] ?? cat;
  }

  getNatureModifiers(name) {
    return this.#natures?.[name]?.modifiers ?? {};
  }
}
