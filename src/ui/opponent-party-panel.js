import { SearchInput } from './search-input.js';

const PARTY_SIZE = 6;

export class OpponentPartyPanel {
  #onSelect;
  #slots;

  constructor(container, loader, onSelect) {
    this.#onSelect = onSelect;
    this.#slots = Array.from({ length: PARTY_SIZE }, () => ({
      species: null,
      selected: false,
    }));

    for (let i = 0; i < PARTY_SIZE; i++) {
      const search = new SearchInput(loader, (species) => this.#commitSlot(i, species));
      search.mount(container);
    }
  }

  #commitSlot(index, species) {
    for (const slot of this.#slots) slot.selected = false;
    this.#slots[index].species = species;
    this.#slots[index].selected = true;
    this.#onSelect(species);
  }
}
