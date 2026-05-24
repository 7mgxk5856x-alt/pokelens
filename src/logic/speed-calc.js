import { calcStat } from './calc-actual-stats.js';

const MAX_ABILITY_POINTS = 32;
const ZERO_ABILITY_POINTS = 0;
const NATURE_UP = 1.1;
const NATURE_NEUTRAL = 1.0;
const NATURE_DOWN = 0.9;
export const SCARF_MULTIPLIER = 1.5;

/**
 * 素早さ種族値から代表 4 パターンの実数値を計算する。
 *
 * 4 パターン（最速 / 準速 / 無振り / 最遅）は対戦コミュニティで標準化された分類であり、
 * 相手ポケモン詳細（`OpponentPokemonDetail`）の一覧表示用に「ドメインビュー」を集約した
 * 関数として位置付ける（個別パターンのみ必要な場合は呼び出し側で分割代入する）。
 * 戻り値オブジェクトのキー順は UI の列順（左から最速→最遅）と一致させている。
 *
 * @param {number} baseSpe 素早さ種族値（正の整数）
 * @returns {{fastest: number, fast: number, neutral: number, slowest: number}}
 *   最速・準速・無振り中立・最遅
 * @throws {RangeError} baseSpe が正の整数でない場合
 */
export function calcSpeedPatterns(baseSpe) {
  if (!Number.isInteger(baseSpe) || baseSpe <= 0) {
    throw new RangeError(
      `calcSpeedPatterns: baseSpe は正の整数である必要があります (受け取った値: ${baseSpe})`,
    );
  }
  return {
    fastest: calcStat(baseSpe, MAX_ABILITY_POINTS, NATURE_UP),
    fast: calcStat(baseSpe, MAX_ABILITY_POINTS, NATURE_NEUTRAL),
    neutral: calcStat(baseSpe, ZERO_ABILITY_POINTS, NATURE_NEUTRAL),
    slowest: calcStat(baseSpe, ZERO_ABILITY_POINTS, NATURE_DOWN),
  };
}
