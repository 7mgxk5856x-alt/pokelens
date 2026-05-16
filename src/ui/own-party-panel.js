export class OwnPartyPanel {
  #loader;
  #onSelect;
  #cards = [];
  #selectedIndex = null;

  constructor(container, partyEntries, loader, onSelect) {
    this.#loader = loader;
    this.#onSelect = onSelect;
    this.#render(container, partyEntries);
  }

  #render(container, partyEntries) {
    partyEntries.forEach((entry, index) => {
      const card = this.#createCard(entry, index);
      container.appendChild(card);
      this.#cards.push(card);
    });
  }

  #createCard(entry, index) {
    const pokemonData = this.#loader.getPokemonByName(entry.species);
    const card = document.createElement('div');
    card.className = 'pokemon-card';

    if (!pokemonData) {
      const errorEl = document.createElement('div');
      errorEl.className = 'error';
      errorEl.textContent = `不明なポケモン: ${entry.species}`;
      card.appendChild(errorEl);
      return card;
    }

    const nameEl = document.createElement('div');
    nameEl.className = 'name';
    nameEl.textContent = entry.species;
    card.appendChild(nameEl);

    const typesEl = document.createElement('div');
    typesEl.className = 'types';
    typesEl.textContent = pokemonData.types.map((t) => this.#loader.getTypeName(t)).join(' / ');
    card.appendChild(typesEl);

    card.addEventListener('click', () => this.#selectCard(index, entry, pokemonData));

    return card;
  }

  #selectCard(index, entry, pokemonData) {
    if (this.#selectedIndex !== null && this.#cards[this.#selectedIndex]) {
      this.#cards[this.#selectedIndex].classList.remove('selected');
    }
    this.#selectedIndex = index;
    this.#cards[index].classList.add('selected');
    this.#onSelect(entry, pokemonData);
  }
}
