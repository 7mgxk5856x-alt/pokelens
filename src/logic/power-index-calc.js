import { MOVE_CATEGORY } from './constants.js';

const STAB_MULTIPLIER = 1.5;

/**
 * 技の火力指数を計算する（攻撃実数値 × タイプ一致補正 × 各種倍率）。
 * @param {object} move 技データ（category / power / type）
 * @param {object} actualStats 実数値（atk / spa）
 * @param {string[]} pokemonTypes ポケモンのタイプ配列
 * @param {number} abilityModifier 特性による倍率（呼び出し元 resolveModifier で条件評価済み）
 * @param {number} itemModifier アイテムによる倍率（呼び出し元 resolveModifier で条件評価済み）
 * @returns {number|null} 火力指数。変化技（Status）や威力が null の技は null を返す
 */
export function calcPowerIndex(move, actualStats, pokemonTypes, abilityModifier, itemModifier) {
  if (move.category === MOVE_CATEGORY.STATUS) {
    return null;
  }
  if (move.power === null) {
    return null;
  }

  const attackStat =
    move.category === MOVE_CATEGORY.PHYSICAL ? actualStats.atk : actualStats.spa;
  const stabMultiplier = pokemonTypes.includes(move.type) ? STAB_MULTIPLIER : 1.0;

  return move.power * attackStat * stabMultiplier * abilityModifier * itemModifier;
}
