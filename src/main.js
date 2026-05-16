import { DataLoader } from './data/loader.js';
import { OwnPartyPanel } from './ui/own-party-panel.js';
import { OwnPokemonDetail } from './ui/own-pokemon-detail.js';
import { OpponentPartyPanel } from './ui/opponent-party-panel.js';
import { OpponentPokemonDetail } from './ui/opponent-pokemon-detail.js';

async function init() {
  const errorEl = document.getElementById('error-message');
  const ownPartyEl = document.getElementById('own-party');
  const ownDetailEl = document.getElementById('own-detail');
  const opponentPartyEl = document.getElementById('opponent-party');
  const opponentDetailEl = document.getElementById('opponent-detail');

  const loader = new DataLoader();

  let data;
  try {
    data = await loader.load();
  } catch (e) {
    errorEl.textContent = e.message;
    errorEl.style.display = 'block';
    return;
  }

  const ownDetail = new OwnPokemonDetail(ownDetailEl, loader);
  const opponentDetail = new OpponentPokemonDetail(opponentDetailEl, loader);

  new OwnPartyPanel(ownPartyEl, data.userParty.party, loader, (entry) => {
    ownDetail.update(entry);
  });

  new OpponentPartyPanel(opponentPartyEl, loader, (species) => {
    opponentDetail.update(species);
  });
}

init();
