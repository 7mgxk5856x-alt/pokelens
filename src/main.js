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

  if (!errorEl || !ownPartyEl || !ownDetailEl || !opponentPartyEl || !opponentDetailEl) {
    // 必須 DOM 要素が欠けると以降のエラー表示自体も失敗するため、コンソールに記録して早期 return する
    // eslint-disable-next-line no-console -- 表示できる UI 要素が存在しない最終フォールバック
    console.error('[pokelens] 必須 DOM 要素が見つかりません。index.html を確認してください');
    return;
  }

  const loader = new DataLoader();

  let userParty;
  try {
    ({ userParty } = await loader.load());
  } catch (e) {
    errorEl.textContent = e.message;
    errorEl.style.display = 'block';
    return;
  }

  const ownDetail = new OwnPokemonDetail(ownDetailEl, loader);
  const opponentDetail = new OpponentPokemonDetail(opponentDetailEl, loader);

  new OwnPartyPanel(ownPartyEl, userParty.party, loader, (entry) => {
    ownDetail.update(entry);
  });

  new OpponentPartyPanel(opponentPartyEl, loader, (species) => {
    if (species === null) {
      opponentDetail.hide();
    } else {
      opponentDetail.update(species);
    }
  });
}

init();
