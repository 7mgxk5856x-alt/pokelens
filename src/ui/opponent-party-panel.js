import { SearchInput } from './search-input.js';

const PARTY_SIZE = 6;

export class OpponentPartyPanel {
  #loader;
  #onSelect;
  #slots;

  constructor(container, loader, onSelect) {
    this.#loader = loader;
    this.#onSelect = onSelect;
    this.#slots = Array.from({ length: PARTY_SIZE }, () => ({
      card: null,
      info: null,
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

    const info = document.createElement('div');
    info.className = 'opponent-info';
    info.hidden = true;
    card.appendChild(info);

    card.addEventListener('click', (e) => {
      if (e.target.tagName === 'INPUT') return;
      if (this.#slots[index].species) {
        this.#selectSlot(index);
      }
    });

    this.#slots[index].card = card;
    this.#slots[index].info = info;
    return card;
  }

  #commitSlot(index, species) {
    const slot = this.#slots[index];
    slot.species = species;
    this.#renderInfo(slot, species);
    this.#selectSlot(index);
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
    }
    slot.info.hidden = false;
  }

  #selectSlot(index) {
    for (const slot of this.#slots) slot.card.classList.remove('selected');
    this.#slots[index].card.classList.add('selected');
    this.#onSelect(this.#slots[index].species);
  }
}
