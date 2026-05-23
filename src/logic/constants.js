// ロジック層で共有するドメインの区分値（複数ファイルで比較・分岐に使う文字列）。

// 技の分類（Showdown の move.category 値）
export const MOVE_CATEGORY = Object.freeze({
  PHYSICAL: 'Physical',
  SPECIAL: 'Special',
  STATUS: 'Status',
});

// 修正子の種別。resolveModifier に渡し、タイプ一致補正の扱いを切り替える
export const MODIFIER_KIND = Object.freeze({
  ABILITY: 'ability',
  ITEM: 'item',
});
