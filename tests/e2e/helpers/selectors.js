// UI セレクタ定数。DOM 構造変更時はこのファイルのみ更新する。

export const SEL = {
  errorMessage: '#error-message',

  ownParty: '#own-party',
  ownCards: '#own-party .pokemon-card',
  ownDetail: '#own-detail',
  ownDetailHeader: '#own-detail .detail-header',
  ownDetailHeaderName: '#own-detail .detail-header .name',
  ownDetailHeaderTypes: '#own-detail .detail-header .types',
  ownDetailRows: '#own-detail .detail-row',
  ownDetailStats: '#own-detail .detail-stats',
  ownMovesTable: '#own-detail .detail-moves',
  ownMoveRows: '#own-detail .detail-moves tbody tr',

  opponentParty: '#opponent-party',
  opponentCards: '#opponent-party .opponent-card',
  oppInput: '.opp-input',
  suggestList: '.suggest-list',
  suggestItems: '.suggest-list .suggest-item',
  suggestNotFound: '.suggest-list .suggest-item.is-not-found',
  suggestHover: '.suggest-list .suggest-item.is-hover',
  opponentClear: '.opponent-clear',
  opponentInfo: '.opponent-info',
  opponentDetail: '#opponent-detail',
  speedPatterns: '.speed-patterns li',
};
