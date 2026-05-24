import { calcActualStats } from '../logic/calc-actual-stats.js';
import { calcPowerIndex } from '../logic/power-index-calc.js';
import { resolveModifier } from '../logic/resolve-modifier.js';
import { MODIFIER_KIND } from '../logic/constants.js';
import { SCARF_MULTIPLIER } from '../logic/speed-calc.js';
import { el } from './dom-utils.js';
import { STAT_LABELS } from './stat-labels.js';

const MOVE_COLUMNS = ['技名', 'タイプ', '威力', '分類', '命中', '火力指数'];

const DASH = '−';
const CHOICE_SCARF_ITEM_NAME = 'こだわりスカーフ';
const SPE_KEY = 'spe';

/** 自分の選択ポケモンの詳細（実数値・性格・技ごとの火力指数など）を描画するビュー。 */
export class OwnPokemonDetail {
  #container;
  #loader;

  constructor(container, loader) {
    this.#container = container;
    this.#loader = loader;
  }

  update(entry) {
    const pokemonData = this.#loader.getPokemonByName(entry.species);
    if (!pokemonData) {
      return;
    }

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
      this.#buildNatureRow(entry.nature, natureModifiers),
      this.#buildBaseStatsRow(pokemonData.baseStats),
      this.#buildActualStatsRow(actualStats, entry.item),
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

  #buildNatureRow(nature, natureModifiers) {
    if (!nature) {
      return el('div', 'detail-row', `性格: ${DASH}`);
    }
    const up = [];
    const down = [];
    for (const [key, label] of STAT_LABELS) {
      const mod = natureModifiers[key];
      if (mod > 1) {
        up.push(label);
      } else if (mod < 1) {
        down.push(label);
      }
    }
    const upText = up.map((s) => `${s}↑`).join(' ');
    const downText = down.map((s) => `${s}↓`).join(' ');
    const separator = up.length && down.length ? ' / ' : '';
    const suffix =
      up.length || down.length ? ` (${upText}${separator}${downText})` : ' (補正なし)';
    return el('div', 'detail-row', `性格: ${nature}${suffix}`);
  }

  #buildBaseStatsRow(baseStats) {
    const text = STAT_LABELS.map(([key, label]) => `${label} ${baseStats[key]}`).join(' / ');
    return el('div', 'detail-stats', `種族値: ${text}`);
  }

  #buildActualStatsRow(actualStats, item) {
    const isScarf = item === CHOICE_SCARF_ITEM_NAME;
    const text = STAT_LABELS.map(([key, label]) => {
      const value = actualStats[key];
      if (key === SPE_KEY && isScarf) {
        return `${label} ${value} (${Math.floor(value * SCARF_MULTIPLIER)})`;
      }
      return `${label} ${value}`;
    }).join(' / ');
    return el('div', 'detail-stats', `実数値: ${text}`);
  }

  #buildMovesTable(entry, pokemonData, actualStats) {
    const table = el('table', 'detail-moves');

    const thead = document.createElement('thead');
    const headRow = document.createElement('tr');
    for (const col of MOVE_COLUMNS) {
      headRow.appendChild(el('th', null, col));
    }
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
      // 技名 TD はメソッド冒頭で追加済みのため、MOVE_COLUMNS の残り 5 列分（タイプ・威力・分類・命中・火力指数）を空文字または DASH で埋める
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

    const abilityResolved = resolveModifier(
      abilityModifier,
      move,
      pokemonData.types,
      MODIFIER_KIND.ABILITY
    );

    const effectiveMove = abilityResolved.moveTypeOverride
      ? { ...move, type: abilityResolved.moveTypeOverride }
      : move;

    const itemResolved = resolveModifier(
      itemModifier,
      effectiveMove,
      abilityResolved.typesForCalc,
      MODIFIER_KIND.ITEM
    );

    return calcPowerIndex(
      effectiveMove,
      actualStats,
      itemResolved.typesForCalc,
      abilityResolved.multiplier,
      itemResolved.multiplier
    );
  }
}
