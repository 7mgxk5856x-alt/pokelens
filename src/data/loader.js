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

    return { pokedex, moves, items, abilities, typeNames, moveCategories, natures, userParty };
  }

  /**
   * 日本語名でポケモンを引く。
   * @param {string} name ポケモンの日本語名
   * @returns {object|null} 一致するポケモン。無ければ null
   */
  getPokemonByName(name) {
    if (!this.#pokedex) {
      return null;
    }
    return Object.values(this.#pokedex).find((e) => e.name === name) ?? null;
  }

  /**
   * 名前の前方一致でポケモンを検索する。
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
