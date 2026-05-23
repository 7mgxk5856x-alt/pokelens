export const STAT_LABELS = [
  ['hp', 'H'],
  ['atk', 'A'],
  ['def', 'B'],
  ['spa', 'C'],
  ['spd', 'D'],
  ['spe', 'S'],
];

/**
 * 種族値を 'H35 A55 B40 C50 D50 S90' 形式の 1 行文字列に整形する。
 * @param {Record<string, number>} baseStats hp/atk/def/spa/spd/spe をキーに持つ種族値
 * @returns {string} 整形済み 1 行文字列
 */
export function formatBaseStats(baseStats) {
  return STAT_LABELS.map(([key, label]) => `${label}${baseStats[key]}`).join(' ');
}
