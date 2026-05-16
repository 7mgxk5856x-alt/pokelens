const STATS = ['hp', 'atk', 'def', 'spa', 'spd', 'spe'];

export function calcActualStats(baseStats, abilityPoints, natureModifiers) {
  const result = {};
  for (const stat of STATS) {
    const base = baseStats[stat];
    const ap = abilityPoints[stat];
    if (stat === 'hp') {
      result[stat] = base + ap + 75;
      continue;
    }
    const nature = natureModifiers[stat] ?? 1.0;
    result[stat] = Math.floor((base + ap + 20) * nature);
  }
  return result;
}
