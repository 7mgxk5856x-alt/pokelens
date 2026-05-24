import { formatBaseStats } from './stat-labels.js';

const NORMAL_STATE = -1;

/** 自分パーティ（6 体）の一覧を描画し、ポケモン選択を扱うパネル。 */
export class OwnPartyPanel {
  #loader;
  #onSelect;
  #cards = [];
  #megaIndices = [];
  #selectedIndex = null;

  constructor(container, partyEntries, loader, onSelect) {
    this.#loader = loader;
    this.#onSelect = onSelect;
    this.#render(container, partyEntries);
  }

  #render(container, partyEntries) {
    partyEntries.forEach((entry, index) => {
      this.#megaIndices.push(NORMAL_STATE);
      const card = this.#createCard(entry, index);
      container.appendChild(card);
      this.#cards.push(card);
    });
  }

  #createCard(entry, index) {
    const card = document.createElement('div');
    card.className = 'pokemon-card';
    this.#renderCardContent(card, entry, index);
    card.addEventListener('click', () => {
      const pokemonData = this.#getDisplayedPokemonData(entry, index);
      if (pokemonData) {
        this.#selectCard(index, entry, pokemonData);
      }
    });
    return card;
  }

  #renderCardContent(card, entry, index) {
    card.replaceChildren();

    const pokemonData = this.#loader.getPokemonByName(entry.species);
    if (!pokemonData) {
      const errorEl = document.createElement('div');
      errorEl.className = 'error';
      errorEl.textContent = `不明なポケモン: ${entry.species}`;
      card.appendChild(errorEl);
      return;
    }

    // メガ状態でメガフォームの pokedex エントリが欠けている場合（mega-evolutions.json の手動メンテミス想定）は
    // 親の pokedex 値にフォールバックして、空白カード化や TypeError を避ける防御層
    const displayed = this.#getDisplayedPokemonData(entry, index) ?? pokemonData;

    const nameEl = document.createElement('div');
    nameEl.className = 'name';
    nameEl.textContent = displayed.name;
    card.appendChild(nameEl);

    const typesEl = document.createElement('div');
    typesEl.className = 'types';
    typesEl.textContent = displayed.types.map((t) => this.#loader.getTypeName(t)).join(' / ');
    card.appendChild(typesEl);

    const baseStatsEl = document.createElement('div');
    baseStatsEl.className = 'base-stats';
    baseStatsEl.textContent = formatBaseStats(displayed.baseStats);
    card.appendChild(baseStatsEl);

    const megaInfo = this.#loader.getMegaInfo(entry.species);
    // 機能 7: 自分側は持ち物が当該ポケモンのメガストーンと一致するときだけ切替ボタンを表示
    if (megaInfo && megaInfo.stones.includes(entry.item)) {
      card.appendChild(this.#createMegaToggleButton(entry, index, megaInfo.megaForms.length));
    }
  }

  #createMegaToggleButton(entry, index, formCount) {
    const button = document.createElement('button');
    button.className = 'mega-toggle';
    button.type = 'button';
    button.textContent = this.#megaIndices[index] === NORMAL_STATE ? 'メガシンカ' : '通常';
    // Tab フォーカスから外す（相手側 mega-toggle・opponent-clear と同じ操作性ポリシー：
    // 6 つのカード／入力欄を Tab で順に移動する操作性を優先し、補助操作はマウスに統一）
    button.tabIndex = -1;
    button.addEventListener('click', (event) => {
      event.stopPropagation();
      this.#cycleMegaState(entry, index, formCount);
    });
    return button;
  }

  #cycleMegaState(entry, index, formCount) {
    const current = this.#megaIndices[index];
    // 通常(-1) → 0 → 1 → ... → formCount-1 → 通常(-1) の順で循環
    this.#megaIndices[index] = current >= formCount - 1 ? NORMAL_STATE : current + 1;
    this.#renderCardContent(this.#cards[index], entry, index);
    if (this.#selectedIndex === index) {
      this.#cards[index].classList.add('selected');
      const pokemonData = this.#getDisplayedPokemonData(entry, index);
      if (pokemonData) {
        this.#onSelect(entry, pokemonData);
      }
    }
  }

  #getDisplayedPokemonData(entry, index) {
    const megaIndex = this.#megaIndices[index];
    if (megaIndex === NORMAL_STATE) {
      return this.#loader.getPokemonByName(entry.species);
    }
    const megaInfo = this.#loader.getMegaInfo(entry.species);
    if (!megaInfo || megaIndex >= megaInfo.megaForms.length) {
      return this.#loader.getPokemonByName(entry.species);
    }
    return this.#loader.getMegaFormData(megaInfo.megaForms[megaIndex]);
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
