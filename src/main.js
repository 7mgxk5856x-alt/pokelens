import { DataLoader } from './data/loader.js';
import { OwnPartyPanel } from './ui/own-party-panel.js';

async function init() {
  const errorEl = document.getElementById('error-message');
  const ownPartyEl = document.getElementById('own-party');
  const ownDetailEl = document.getElementById('own-detail');

  const loader = new DataLoader();

  let data;
  try {
    data = await loader.load();
  } catch (e) {
    errorEl.textContent = e.message;
    errorEl.style.display = 'block';
    return;
  }

  new OwnPartyPanel(ownPartyEl, data.userParty.party, loader, (_entry, _pokemonData) => {
    ownDetailEl.style.display = 'block';
  });
}

init();
