import { SearchInput } from './search-input.js';
import { STAT_LABELS } from './stat-labels.js';

const PARTY_SIZE = 6;

function formatBaseStats(baseStats) {
  return STAT_LABELS.map(([key, label]) => `${label}${baseStats[key]}`).join(' ');
}

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
    clearButton.addEventListener('click', (e) => {
      e.stopPropagation();
      this.#clearSlot(index);
    });
    card.appendChild(clearButton);

    card.addEventListener('click', (e) => {
      if (e.target.tagName === 'INPUT' || e.target === clearButton) {
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
    this.#renderInfo(slot, species);
    slot.searchWrapper.hidden = true;
    slot.clearButton.hidden = false;
    this.#selectSlot(index);
  }

  #clearSlot(index) {
    const slot = this.#slots[index];
    const wasSelected = this.#selectedIndex === index;
    slot.species = null;
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

  #renderInfo(slot, species) {
    const data = this.#loader.getPokemonByName(species);
    slot.info.replaceChildren();

    const nameEl = document.createElement('div');
    nameEl.className = 'name';
    nameEl.textContent = species;
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
    }
    slot.info.hidden = false;
  }

  #selectSlot(index) {
    for (const slot of this.#slots) {
      slot.card.classList.remove('selected');
    }
    this.#slots[index].card.classList.add('selected');
    this.#selectedIndex = index;
    this.#onSelect(this.#slots[index].species);
  }
}
