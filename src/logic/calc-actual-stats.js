const HP_OFFSET = 75;
const STAT_OFFSET = 20;
const STATS = ['hp', 'atk', 'def', 'spa', 'spd', 'spe'];

export function calcHp(base, ap) {
  return base + ap + HP_OFFSET;
}

export function calcStat(base, ap, natureModifier) {
  return Math.floor((base + ap + STAT_OFFSET) * natureModifier);
}

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
