import { calcSpeedPatterns } from '../logic/speed-calc.js';

const STAT_LABELS = [
  ['hp', 'H'],
  ['atk', 'A'],
  ['def', 'B'],
  ['spa', 'C'],
  ['spd', 'D'],
  ['spe', 'S'],
];

const SPEED_PATTERN_LABELS = [
  ['fastestScarf', '最速スカーフ'],
  ['fastScarf', '準速スカーフ'],
  ['fastest', '最速'],
  ['fast', '準速'],
  ['neutral', '無振り'],
  ['slowest', '最遅'],
];

function el(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

export class OpponentPokemonDetail {
  #container;
  #loader;

  constructor(container, loader) {
    this.#container = container;
    this.#loader = loader;
  }

  update(species) {
    const pokemonData = this.#loader.getPokemonByName(species);
    if (!pokemonData) return;

    this.#container.replaceChildren(
      this.#buildHeader(pokemonData),
      this.#buildAbilitiesRow(pokemonData.abilities),
      this.#buildBaseStatsRow(pokemonData.baseStats),
      this.#buildSpeedSection(pokemonData.baseStats.spe)
    );
    this.#container.style.display = 'block';
  }

  #buildHeader(pokemonData) {
    const header = el('div', 'detail-header');
    header.appendChild(el('span', 'name', pokemonData.name));
    const typeText = pokemonData.types.map((t) => this.#loader.getTypeName(t)).join(' / ');
    header.appendChild(el('span', 'types', typeText));
    return header;
  }

  #buildAbilitiesRow(abilities) {
    const text = abilities.length > 0 ? abilities.join('、') : '−';
    return el('div', 'detail-row', `特性候補: ${text}`);
  }

  #buildBaseStatsRow(baseStats) {
    const text = STAT_LABELS.map(([key, label]) => `${label} ${baseStats[key]}`).join(' / ');
    return el('div', 'detail-stats', text);
  }

  #buildSpeedSection(baseSpe) {
    const wrapper = document.createElement('div');
    wrapper.appendChild(el('div', 'detail-section-title', '素早さ'));

    const patterns = calcSpeedPatterns(baseSpe);
    const list = el('ul', 'speed-patterns');
    for (const [key, label] of SPEED_PATTERN_LABELS) {
      const li = document.createElement('li');
      li.appendChild(el('span', 'label', `${label}:`));
      li.appendChild(el('span', 'value', String(patterns[key])));
      list.appendChild(li);
    }
    wrapper.appendChild(list);
    return wrapper;
  }
}
