import { calcHp, calcStat } from './calc-actual-stats.js';

const MAX_ABILITY_POINTS = 32;
const ZERO_ABILITY_POINTS = 0;
const NATURE_UP = 1.1;
const NATURE_NEUTRAL = 1.0;

/**
 * 耐久指数 = HP 実数値 × 防御/特防 実数値。
 * 入力検証は省略する。呼び出し側（`OwnPokemonDetail` 経由の `calcActualStats` 結果、
 * または `calcEnduranceIndexPatterns` 経由の `calcHp` / `calcStat` 結果）が正の整数を保証するため。
 * @param {number} hp HP 実数値
 * @param {number} defStat 防御または特防の実数値
 * @returns {number} 耐久指数
 */
export function calcEnduranceIndex(hp, defStat) {
  return hp * defStat;
}

/**
 * 相手ポケモンの 4 パターン耐久指数（物理・特殊）を計算する。
 * HP は性格補正の対象外（`calcHp` の式に性格補正は含まない）。
 * 4 パターン: 耐久特化(H32/B32 or D32, B↑ or D↑) / 耐久極振(H0/B32 or D32, B↑ or D↑) /
 * H極振(H32/B0 or D0, 補正なし) / 無振り(H0/B0 or D0, 補正なし)。
 * 入力検証は省略する（`baseStats` は loader 経由でマスターデータから取得した正規データを前提）。
 * @param {{hp: number, def: number, spd: number}} baseStats 種族値（hp/def/spd の 3 キーのみ参照、他キーは無視）
 * @returns {{
 *   specialized: {physical: number, special: number},
 *   defOnly:     {physical: number, special: number},
 *   hpOnly:      {physical: number, special: number},
 *   none:        {physical: number, special: number},
 * }} 各パターンの物理耐久指数・特殊耐久指数
 */
export function calcEnduranceIndexPatterns(baseStats) {
  const hpMax = calcHp(baseStats.hp, MAX_ABILITY_POINTS);
  const hpMin = calcHp(baseStats.hp, ZERO_ABILITY_POINTS);
  const defUp = calcStat(baseStats.def, MAX_ABILITY_POINTS, NATURE_UP);
  const spdUp = calcStat(baseStats.spd, MAX_ABILITY_POINTS, NATURE_UP);
  const defNone = calcStat(baseStats.def, ZERO_ABILITY_POINTS, NATURE_NEUTRAL);
  const spdNone = calcStat(baseStats.spd, ZERO_ABILITY_POINTS, NATURE_NEUTRAL);
  return {
    specialized: {
      physical: calcEnduranceIndex(hpMax, defUp),
      special: calcEnduranceIndex(hpMax, spdUp),
    },
    defOnly: {
      physical: calcEnduranceIndex(hpMin, defUp),
      special: calcEnduranceIndex(hpMin, spdUp),
    },
    hpOnly: {
      physical: calcEnduranceIndex(hpMax, defNone),
      special: calcEnduranceIndex(hpMax, spdNone),
    },
    none: {
      physical: calcEnduranceIndex(hpMin, defNone),
      special: calcEnduranceIndex(hpMin, spdNone),
    },
  };
}
