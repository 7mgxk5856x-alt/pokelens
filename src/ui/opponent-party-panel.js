import { SearchInput } from './search-input.js';
import { formatBaseStats } from './stat-labels.js';

const PARTY_SIZE = 6;
const NORMAL_STATE = -1;

/** 相手パーティの選出枠（6 体）を描画し、ポケモン名の入力・選択・クリアを扱うパネル。 */
export class OpponentPartyPanel {
  #loader;
  #onSelect;
  #slots;
  #selectedIndex = null;

  constructor(container, loader, onSelect) {
    this.#loader = loader;
    this.#onSelect = onSelect;
    this.#slots = Array.from({ length: PARTY_SIZE }, () => ({
      card: null,
      info: null,
      searchWrapper: null,
      search: null,
      clearButton: null,
      species: null,
      megaIndex: NORMAL_STATE,
    }));

    for (let i = 0; i < PARTY_SIZE; i++) {
      const card = this.#createSlot(i);
      container.appendChild(card);
    }
  }

  #createSlot(index) {
    const card = document.createElement('div');
    card.className = 'pokemon-card opponent-card';

    const search = new SearchInput(this.#loader, (species) => this.#commitSlot(index, species));
    search.mount(card);
    const searchWrapper = card.querySelector('.search-input');

    const info = document.createElement('div');
    info.className = 'opponent-info';
    info.hidden = true;
    card.appendChild(info);

    const clearButton = document.createElement('button');
    clearButton.type = 'button';
    clearButton.className = 'opponent-clear';
    clearButton.textContent = '×';
    clearButton.hidden = true;
    clearButton.setAttribute('aria-label', 'クリア');
    // Tab フォーカスから外す。6 つの入力欄を Tab で順に移動する操作性を優先し、
    // クリア操作は明示的なマウスクリックに統一する（aria-label でスクリーンリーダーには認識される）
    clearButton.tabIndex = -1;
    clearButton.addEventListener('click', (e) => {
      e.stopPropagation();
      this.#clearSlot(index);
    });
    card.appendChild(clearButton);

    card.addEventListener('click', (e) => {
      if (e.target.tagName === 'INPUT' || e.target === clearButton) {
        return;
      }
      if (e.target.classList.contains('mega-toggle')) {
        return;
      }
      if (this.#slots[index].species) {
        this.#selectSlot(index);
      }
    });

    this.#slots[index].card = card;
    this.#slots[index].info = info;
    this.#slots[index].searchWrapper = searchWrapper;
    this.#slots[index].search = search;
    this.#slots[index].clearButton = clearButton;
    return card;
  }

  #commitSlot(index, species) {
    const slot = this.#slots[index];
    slot.species = species;
    slot.megaIndex = NORMAL_STATE;
    this.#renderInfo(slot, index);
    slot.searchWrapper.hidden = true;
    slot.clearButton.hidden = false;
    this.#selectSlot(index);
  }

  #clearSlot(index) {
    const slot = this.#slots[index];
    const wasSelected = this.#selectedIndex === index;
    slot.species = null;
    slot.megaIndex = NORMAL_STATE;
    slot.info.hidden = true;
    slot.info.replaceChildren();
    slot.searchWrapper.hidden = false;
    slot.clearButton.hidden = true;
    slot.card.classList.remove('selected');
    slot.search.clear();
    if (wasSelected) {
      this.#selectedIndex = null;
      this.#onSelect(null);
    }
  }

  #renderInfo(slot, index) {
    const species = slot.species;
    const data = this.#getDisplayedPokemonData(slot);
    slot.info.replaceChildren();

    const nameEl = document.createElement('div');
    nameEl.className = 'name';
    nameEl.textContent = data ? data.name : species;
    slot.info.appendChild(nameEl);

    if (data) {
      const typesEl = document.createElement('div');
      typesEl.className = 'types';
      typesEl.textContent = data.types.map((t) => this.#loader.getTypeName(t)).join(' / ');
      slot.info.appendChild(typesEl);

      const baseStatsEl = document.createElement('div');
      baseStatsEl.className = 'base-stats';
      baseStatsEl.textContent = formatBaseStats(data.baseStats);
      slot.info.appendChild(baseStatsEl);

      // 機能 7: 相手側はメガシンカ可能なポケモンであれば持ち物に関わらず常時切替ボタンを表示
      // 持ち物未知のため全メガ形態を循環する（メガシンカ循環ルール）
      // ボタン表示判定はメガ状態に関わらず親エントリの megaForms[] 長さを基準にする
      const parent = this.#loader.getPokemonByName(species);
      const megaForms = parent?.megaForms ?? [];
      if (megaForms.length > 0) {
        slot.info.appendChild(this.#createMegaToggleButton(index, megaForms.length));
      }
    }
    slot.info.hidden = false;
  }

  #createMegaToggleButton(index, formCount) {
    const button = document.createElement('button');
    button.className = 'mega-toggle';
    button.type = 'button';
    button.textContent = this.#slots[index].megaIndex === NORMAL_STATE ? 'メガシンカ' : '通常';
    button.tabIndex = -1;
    button.addEventListener('click', (event) => {
      event.stopPropagation();
      this.#cycleMegaState(index, formCount);
    });
    return button;
  }

  #cycleMegaState(index, formCount) {
    const slot = this.#slots[index];
    const current = slot.megaIndex;
    slot.megaIndex = current >= formCount - 1 ? NORMAL_STATE : current + 1;
    this.#renderInfo(slot, index);
    if (this.#selectedIndex === index) {
      slot.card.classList.add('selected');
      this.#notifySelected(index);
    }
  }

  #getDisplayedPokemonData(slot) {
    if (!slot.species) {
      return null;
    }
    const parent = this.#loader.getPokemonByName(slot.species);
    if (slot.megaIndex === NORMAL_STATE) {
      return parent;
    }
    // 相手側は全メガ形態を循環する。親の megaForms[] からインデックス参照（マスターデータの不整合時は親にフォールバック）
    return parent?.megaForms?.[slot.megaIndex] ?? parent;
  }

  #selectSlot(index) {
    for (const slot of this.#slots) {
      slot.card.classList.remove('selected');
    }
    this.#slots[index].card.classList.add('selected');
    this.#selectedIndex = index;
    this.#notifySelected(index);
  }

  #notifySelected(index) {
    const slot = this.#slots[index];
    const displayed = this.#getDisplayedPokemonData(slot);
    this.#onSelect(slot.species, displayed);
  }
}
