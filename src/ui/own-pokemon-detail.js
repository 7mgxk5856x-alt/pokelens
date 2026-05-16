import { calcActualStats } from '../logic/calc-actual-stats.js';
import { calcPowerIndex } from '../logic/power-index-calc.js';

const STAT_LABELS = [
  ['hp', 'H'],
  ['atk', 'A'],
  ['def', 'B'],
  ['spa', 'C'],
  ['spd', 'D'],
  ['spe', 'S'],
];

const MOVE_COLUMNS = ['技名', 'タイプ', '威力', '分類', '命中', '火力指数'];

const DASH = '−';

const TAG_CONDITIONS = new Set(['isPunch', 'isPulse', 'isBite', 'isRecoil', 'isSlice']);

function pickStatMultiplier(modifier, move) {
  if (move.category === 'Physical') return modifier.atk ?? 1.0;
  if (move.category === 'Special') return modifier.spa ?? 1.0;
  return 1.0;
}

function resolveModifier(modifier, move, pokemonTypes, kind) {
  if (!modifier) return { multiplier: 1.0, typesForCalc: pokemonTypes };

  const condition = modifier.condition ?? null;

  if (condition === 'isStab' && kind === 'ability') {
    if (pokemonTypes.includes(move.type)) {
      return { multiplier: modifier.stab ?? 2.0, typesForCalc: [] };
    }
    return { multiplier: 1.0, typesForCalc: pokemonTypes };
  }

  if (condition === null) {
    return { multiplier: pickStatMultiplier(modifier, move), typesForCalc: pokemonTypes };
  }

  if (condition === 'isType') {
    const match = modifier.moveType != null && move.type === modifier.moveType;
    return {
      multiplier: match ? pickStatMultiplier(modifier, move) : 1.0,
      typesForCalc: pokemonTypes,
    };
  }

  if (TAG_CONDITIONS.has(condition)) {
    const match = move.tags?.includes(condition) ?? false;
    return {
      multiplier: match ? pickStatMultiplier(modifier, move) : 1.0,
      typesForCalc: pokemonTypes,
    };
  }

  if (condition === 'powerMax60') {
    const match = move.power != null && move.power <= 60;
    return {
      multiplier: match ? pickStatMultiplier(modifier, move) : 1.0,
      typesForCalc: pokemonTypes,
    };
  }

  return { multiplier: 1.0, typesForCalc: pokemonTypes };
}

function el(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

export class OwnPokemonDetail {
  #container;
  #loader;

  constructor(container, loader) {
    this.#container = container;
    this.#loader = loader;
  }

  update(entry) {
    const pokemonData = this.#loader.getPokemonByName(entry.species);
    if (!pokemonData) return;

    const natureModifiers = this.#loader.getNatureModifiers(entry.nature);
    const actualStats = calcActualStats(
      pokemonData.baseStats,
      entry.abilityPoints,
      natureModifiers
    );

    this.#container.replaceChildren(
      this.#buildHeader(pokemonData),
      this.#buildAbilityRow(entry.ability),
      this.#buildItemRow(entry.item),
      this.#buildStatsRow(actualStats),
      this.#buildMovesTable(entry, pokemonData, actualStats)
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

  #buildAbilityRow(ability) {
    return el('div', 'detail-row', `特性: ${ability ?? DASH}`);
  }

  #buildItemRow(item) {
    return el('div', 'detail-row', `持ち物: ${item ?? DASH}`);
  }

  #buildStatsRow(actualStats) {
    const text = STAT_LABELS.map(([key, label]) => `${label} ${actualStats[key]}`).join(' / ');
    return el('div', 'detail-stats', text);
  }

  #buildMovesTable(entry, pokemonData, actualStats) {
    const table = el('table', 'detail-moves');

    const thead = document.createElement('thead');
    const headRow = document.createElement('tr');
    for (const col of MOVE_COLUMNS) headRow.appendChild(el('th', null, col));
    thead.appendChild(headRow);
    table.appendChild(thead);

    const tbody = document.createElement('tbody');
    for (const moveEntry of entry.moves) {
      tbody.appendChild(this.#buildMoveRow(moveEntry, entry, pokemonData, actualStats));
    }
    table.appendChild(tbody);
    return table;
  }

  #buildMoveRow(moveEntry, partyEntry, pokemonData, actualStats) {
    const tr = document.createElement('tr');
    const move = this.#loader.getMove(moveEntry.name);

    tr.appendChild(el('td', 'move-name', moveEntry.name));

    if (!move) {
      tr.appendChild(el('td', null, ''));
      tr.appendChild(el('td', null, DASH));
      tr.appendChild(el('td', null, ''));
      tr.appendChild(el('td', null, DASH));
      tr.appendChild(el('td', null, DASH));
      return tr;
    }

    tr.appendChild(el('td', null, this.#loader.getTypeName(move.type)));
    tr.appendChild(el('td', null, move.power == null ? DASH : String(move.power)));
    tr.appendChild(el('td', null, this.#loader.getMoveCategory(move.category)));
    tr.appendChild(el('td', null, move.accuracy == null ? DASH : String(move.accuracy)));

    const powerIndex = this.#calcMovePowerIndex(move, partyEntry, pokemonData, actualStats);
    tr.appendChild(
      el('td', 'power-index', powerIndex == null ? DASH : Math.round(powerIndex).toString())
    );
    return tr;
  }

  #calcMovePowerIndex(move, partyEntry, pokemonData, actualStats) {
    const abilityModifier = this.#loader.getAbilityModifier(partyEntry.ability);
    const itemModifier = this.#loader.getItemModifier(partyEntry.item);

    const abilityResolved = resolveModifier(abilityModifier, move, pokemonData.types, 'ability');
    const itemResolved = resolveModifier(
      itemModifier,
      move,
      abilityResolved.typesForCalc,
      'item'
    );

    return calcPowerIndex(
      move,
      actualStats,
      itemResolved.typesForCalc,
      abilityResolved.multiplier,
      itemResolved.multiplier
    );
  }
}
