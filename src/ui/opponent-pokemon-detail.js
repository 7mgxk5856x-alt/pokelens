import { calcEndurancePatterns } from '../logic/endurance-calc.js';
import { calcSpeedPatterns } from '../logic/speed-calc.js';
import { el } from './dom-utils.js';
import { STAT_LABELS } from './stat-labels.js';

const SPEED_PATTERN_LABELS = [
  ['fastest', '最速'],
  ['fast', '準速'],
  ['neutral', '無振り'],
  ['slowest', '最遅'],
];

const ENDURANCE_PATTERN_LABELS = [
  ['specialized', '耐久特化'],
  ['defOnly', '耐久極振'],
  ['hpOnly', 'H極振'],
  ['none', '無振り'],
];

const ENDURANCE_ROW_LABELS = [
  ['physical', '物理耐久指数'],
  ['special', '特殊耐久指数'],
];

const DASH = '−';

/** 相手の選択ポケモンの詳細（種族値・素早さ 4 パターン・耐久指数 4 パターンなど）を描画するビュー。 */
export class OpponentPokemonDetail {
  #container;
  #loader;

  constructor(container, loader) {
    this.#container = container;
    this.#loader = loader;
  }

  update(species) {
    const pokemonData = this.#loader.getPokemonByName(species);
    if (!pokemonData) {
      // マスターデータに存在しない species を受け取った場合、前回の表示残留を防ぐため非表示にする
      this.hide();
      return;
    }

    this.#container.replaceChildren(
      this.#buildHeader(pokemonData),
      this.#buildAbilitiesRow(pokemonData.abilities),
      this.#buildBaseStatsRow(pokemonData.baseStats),
      this.#buildSpeedSection(pokemonData.baseStats.spe),
      this.#buildEnduranceSection(pokemonData.baseStats)
    );
    this.#container.style.display = 'block';
  }

  hide() {
    this.#container.replaceChildren();
    this.#container.style.display = 'none';
  }

  #buildHeader(pokemonData) {
    const header = el('div', 'detail-header');
    header.appendChild(el('span', 'name', pokemonData.name));
    const typeText = pokemonData.types.map((t) => this.#loader.getTypeName(t)).join(' / ');
    header.appendChild(el('span', 'types', typeText));
    return header;
  }

  #buildAbilitiesRow(abilities) {
    const text = abilities.length > 0 ? abilities.join('、') : DASH;
    return el('div', 'detail-row', `特性候補: ${text}`);
  }

  #buildBaseStatsRow(baseStats) {
    const text = STAT_LABELS.map(([key, label]) => `${label} ${baseStats[key]}`).join(' / ');
    return el('div', 'detail-stats', text);
  }

  #buildSpeedSection(baseSpe) {
    const wrapper = el('div', 'detail-speed');
    wrapper.appendChild(el('div', 'detail-section-title', '素早さ'));

    const patterns = calcSpeedPatterns(baseSpe);
    const table = el('table', 'speed-patterns');
    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');
    for (const [, label] of SPEED_PATTERN_LABELS) {
      headerRow.appendChild(el('th', null, label));
    }
    thead.appendChild(headerRow);

    const tbody = document.createElement('tbody');
    const dataRow = document.createElement('tr');
    for (const [key] of SPEED_PATTERN_LABELS) {
      dataRow.appendChild(el('td', null, String(patterns[key])));
    }
    tbody.appendChild(dataRow);

    table.appendChild(thead);
    table.appendChild(tbody);
    wrapper.appendChild(table);
    return wrapper;
  }

  #buildEnduranceSection(baseStats) {
    const wrapper = el('div', 'detail-endurance');
    wrapper.appendChild(el('div', 'detail-section-title', '耐久指数'));

    const patterns = calcEndurancePatterns(baseStats);
    const table = el('table', 'endurance-patterns');

    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');
    headerRow.appendChild(el('th', null, ''));
    for (const [, label] of ENDURANCE_PATTERN_LABELS) {
      headerRow.appendChild(el('th', null, label));
    }
    thead.appendChild(headerRow);

    const tbody = document.createElement('tbody');
    for (const [rowKey, rowLabel] of ENDURANCE_ROW_LABELS) {
      const tr = document.createElement('tr');
      tr.appendChild(el('th', null, rowLabel));
      for (const [patternKey] of ENDURANCE_PATTERN_LABELS) {
        tr.appendChild(el('td', null, String(patterns[patternKey][rowKey])));
      }
      tbody.appendChild(tr);
    }

    table.appendChild(thead);
    table.appendChild(tbody);
    wrapper.appendChild(table);
    return wrapper;
  }
}
