const NOT_FOUND_TEXT = '見つかりません';

export class SearchInput {
  #loader;
  #onCommit;
  #input;
  #list;
  #currentResults = [];
  #hoverIndex = null;
  #notFound = false;

  constructor(loader, onCommit) {
    this.#loader = loader;
    this.#onCommit = onCommit;
  }

  mount(container) {
    const wrapper = document.createElement('div');
    wrapper.className = 'search-input';

    this.#input = document.createElement('input');
    this.#input.type = 'text';
    this.#input.className = 'opp-input';
    wrapper.appendChild(this.#input);

    this.#list = document.createElement('ul');
    this.#list.className = 'suggest-list';
    this.#list.hidden = true;
    wrapper.appendChild(this.#list);

    this.#input.addEventListener('input', () => this.#handleInput());
    this.#input.addEventListener('keydown', (e) => this.#handleKeydown(e));

    container.appendChild(wrapper);
  }

  #handleInput() {
    const query = this.#input.value;
    if (query === '') {
      this.#hideSuggestions();
      return;
    }

    const results = this.#loader.searchByName(query);
    if (results.length === 0) {
      this.#renderNotFound();
      return;
    }

    this.#currentResults = results;
    this.#notFound = false;
    this.#hoverIndex = 0;
    this.#renderResults();
  }

  #renderResults() {
    this.#list.replaceChildren();
    this.#currentResults.forEach((entry, index) => {
      const li = document.createElement('li');
      li.className = 'suggest-item';
      if (index === this.#hoverIndex) li.classList.add('is-hover');
      li.textContent = entry.name;
      li.addEventListener('mousedown', (e) => {
        e.preventDefault();
        this.#commit(entry);
      });
      this.#list.appendChild(li);
    });
    this.#list.hidden = false;
  }

  #renderNotFound() {
    this.#currentResults = [];
    this.#notFound = true;
    this.#hoverIndex = null;
    this.#list.replaceChildren();
    const li = document.createElement('li');
    li.className = 'suggest-item is-not-found';
    li.textContent = NOT_FOUND_TEXT;
    this.#list.appendChild(li);
    this.#list.hidden = false;
  }

  #hideSuggestions() {
    this.#currentResults = [];
    this.#notFound = false;
    this.#hoverIndex = null;
    this.#list.replaceChildren();
    this.#list.hidden = true;
  }

  #handleKeydown(e) {
    if (e.key === 'Tab') {
      if (this.#list.hidden) return;
      e.preventDefault();
      if (this.#notFound || this.#currentResults.length === 0) return;
      const n = this.#currentResults.length;
      const delta = e.shiftKey ? -1 : 1;
      this.#hoverIndex = (this.#hoverIndex + delta + n) % n;
      this.#updateHover();
      return;
    }

    if (e.key === 'Enter') {
      if (this.#list.hidden || this.#notFound) return;
      if (this.#hoverIndex == null) return;
      e.preventDefault();
      this.#commit(this.#currentResults[this.#hoverIndex]);
      return;
    }

    if (e.key === 'Escape') {
      if (this.#list.hidden) return;
      e.preventDefault();
      this.#hideSuggestions();
    }
  }

  #updateHover() {
    const items = this.#list.querySelectorAll('.suggest-item');
    items.forEach((item, i) => {
      item.classList.toggle('is-hover', i === this.#hoverIndex);
    });
  }

  #commit(entry) {
    this.#input.value = entry.name;
    this.#hideSuggestions();
    this.#onCommit(entry.name);
  }
}
