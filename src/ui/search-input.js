const NOT_FOUND_TEXT = '見つかりません';

const NAVIGATE_DELTA = {
  Tab: (e) => (e.shiftKey ? -1 : 1),
  ArrowDown: () => 1,
  ArrowUp: () => -1,
};

export class SearchInput {
  #loader;
  #onCommit;
  #input;
  #list;
  #currentResults = [];
  #hoverIndex = null;
  #notFound = false;
  #abortController = null;

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

    // signal 経由でリスナーを束ね、unmount() で一括解除できるようにする（リスナーリーク防止）
    this.#abortController = new AbortController();
    const { signal } = this.#abortController;
    this.#input.addEventListener('input', () => this.#handleInput(), { signal });
    this.#input.addEventListener('keydown', (e) => this.#handleKeydown(e), { signal });

    container.appendChild(wrapper);
  }

  unmount() {
    this.#abortController?.abort();
    this.#abortController = null;
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
    // replaceChildren() で前回の li を破棄するとそれに付与した mousedown リスナーも GC される。
    // 候補ごとに entry を閉じ込めたクロージャを持たせたいため、イベントデリゲーションではなく li 単位で登録する。
    this.#list.replaceChildren();
    this.#currentResults.forEach((entry, index) => {
      const li = document.createElement('li');
      li.className = 'suggest-item';
      if (index === this.#hoverIndex) {
        li.classList.add('is-hover');
      }
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
    if (NAVIGATE_DELTA[e.key]) {
      if (this.#list.hidden) {
        return;
      }
      e.preventDefault();
      if (this.#notFound || this.#currentResults.length === 0) {
        return;
      }
      const n = this.#currentResults.length;
      const delta = NAVIGATE_DELTA[e.key](e);
      // #renderResults 経由でリスト表示時は #hoverIndex = 0 が保証されるが、防御的に null フォールバックを置く
      this.#hoverIndex = ((this.#hoverIndex ?? 0) + delta + n) % n;
      this.#updateHover();
      return;
    }

    if (e.key === 'Enter') {
      if (this.#list.hidden || this.#notFound) {
        return;
      }
      if (this.#hoverIndex == null) {
        return;
      }
      e.preventDefault();
      this.#commit(this.#currentResults[this.#hoverIndex]);
      return;
    }

    if (e.key === 'Escape') {
      if (this.#list.hidden) {
        return;
      }
      e.preventDefault();
      // Escape はサジェストのみを閉じ、入力テキストは保持する（再入力しやすくするため）
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

  clear() {
    if (!this.#input) {
      return;
    }
    this.#input.value = '';
    this.#hideSuggestions();
  }
}
