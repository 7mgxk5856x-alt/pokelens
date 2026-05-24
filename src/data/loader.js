import { searchByName as searchByNameImpl } from '../logic/name-search.js';
import megaEvolutions from './mega-evolutions.json';

const REQUIRED_PARTY_FIELDS = ['species', 'nature', 'abilityPoints', 'moves'];

// `mega-evolutions.json` の "_comment" メタフィールドはマッピング対象外
const META_KEYS = new Set(['_comment']);

function buildMegaFormNameSet(megaEvolutionMap) {
  const set = new Set();
  for (const [parentName, info] of Object.entries(megaEvolutionMap)) {
    if (META_KEYS.has(parentName)) {
      continue;
    }
    for (const formName of info.megaForms ?? []) {
      set.add(formName);
    }
  }
  return set;
}

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
  #megaEvolutions = megaEvolutions;
  #megaFormNameSet = buildMegaFormNameSet(megaEvolutions);

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
   * 日本語名でポケモンを引く（通常状態のみ）。
   * メガフォーム名（例: 「メガフシギバナ」）は **null を返す**。これは PRD 機能 7 の
   * 「`party.json` の species にメガシンカ後の名前が指定された場合は不明なポケモン扱い」の実現と、
   * 「マスターデータ上、メガシンカ後は親に紐づくサブデータとして保持」の論理的表現のため。
   * メガフォームの種族値・タイプ・特性を取得したい場合は `getMegaFormData(name)` を使う。
   * @param {string} name ポケモンの日本語名
   * @returns {object|null} 一致する通常ポケモン。メガフォーム名または未登録名なら null
   */
  getPokemonByName(name) {
    if (!this.#pokedex) {
      return null;
    }
    if (this.#megaFormNameSet.has(name)) {
      return null;
    }
    return Object.values(this.#pokedex).find((e) => e.name === name) ?? null;
  }

  /**
   * メガフォームのポケモンデータを引く（種族値・タイプ・特性）。メガシンカ状態の詳細表示で使用。
   * @param {string} megaName メガフォームの日本語名（例: 「メガフシギバナ」）
   * @returns {object|null} メガフォームのポケモンデータ。メガフォーム名でない場合は null
   */
  getMegaFormData(megaName) {
    if (!this.#pokedex || !this.#megaFormNameSet.has(megaName)) {
      return null;
    }
    return Object.values(this.#pokedex).find((e) => e.name === megaName) ?? null;
  }

  /**
   * 親ポケモン名からメガシンカ情報（メガストーン名・メガフォーム名）を引く。
   * @param {string} parentName 親ポケモンの日本語名（例: 「リザードン」）
   * @returns {{stones: string[], megaForms: string[]}|null} メガシンカ可能なら情報、不可なら null
   */
  getMegaInfo(parentName) {
    const info = this.#megaEvolutions[parentName];
    if (!info || META_KEYS.has(parentName)) {
      return null;
    }
    return { stones: info.stones, megaForms: info.megaForms };
  }

  /**
   * 名前がメガフォームかどうかを判定する。
   * @param {string} name ポケモン名
   * @returns {boolean} メガフォーム名なら true
   */
  isMegaForm(name) {
    return this.#megaFormNameSet.has(name);
  }

  /**
   * 名前の前方一致でポケモンを検索する。メガフォームはサジェスト候補から除外する。
   * @param {string} query 検索クエリ
   * @returns {Array} 一致したポケモンエントリ（図鑑番号昇順）
   */
  searchByName(query) {
    if (!this.#pokedex) {
      return [];
    }
    const entries = Object.values(this.#pokedex).filter((e) => !this.#megaFormNameSet.has(e.name));
    return searchByNameImpl(query, entries);
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
