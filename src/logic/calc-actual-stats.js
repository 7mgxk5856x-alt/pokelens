const HP_OFFSET = 75;
const STAT_OFFSET = 20;
const STATS = ['hp', 'atk', 'def', 'spa', 'spd', 'spe'];

/**
 * HP 実数値を計算する（Pokémon Champions 式: 種族値 + 努力ポイント + 75）。
 * @param {number} base 種族値（HP）
 * @param {number} abilityPoints 努力ポイント（HP）
 * @returns {number} HP 実数値
 */
export function calcHp(base, abilityPoints) {
  return base + abilityPoints + HP_OFFSET;
}

/**
 * HP 以外の実数値を計算する（Pokémon Champions 式: floor((種族値 + 努力ポイント + 20) × 性格補正)）。
 * @param {number} base 種族値
 * @param {number} abilityPoints 努力ポイント
 * @param {number} natureModifier 性格補正倍率（1.1 / 1.0 / 0.9）
 * @returns {number} 実数値
 */
export function calcStat(base, abilityPoints, natureModifier) {
  return Math.floor((base + abilityPoints + STAT_OFFSET) * natureModifier);
}

/**
 * 全 6 ステータス（hp/atk/def/spa/spd/spe）の実数値を計算する。
 * @param {object} baseStats 種族値（各ステータスキーを持つ）
 * @param {object} abilityPoints 努力ポイント（各ステータスキーを持つ）
 * @param {object} natureModifiers 性格補正倍率（ステータスキー→倍率。未指定は 1.0）
 * @returns {object} 実数値（hp/atk/def/spa/spd/spe）
 */
export function calcActualStats(baseStats, abilityPoints, natureModifiers) {
  const result = {};
  for (const stat of STATS) {
    if (stat === 'hp') {
      result.hp = calcHp(baseStats.hp, abilityPoints.hp);
    } else {
      result[stat] = calcStat(
        baseStats[stat],
        abilityPoints[stat],
        natureModifiers[stat] ?? 1.0
      );
    }
  }
  return result;
}
