const STAB_MULTIPLIER = 1.5;

// abilityModifier / itemModifier は呼び出し元 (resolveModifier) で条件評価済みの倍率が渡される前提
export function calcPowerIndex(move, actualStats, pokemonTypes, abilityModifier, itemModifier) {
  if (move.category === 'Status') return null;
  if (move.power === null) return null;

  const attackStat = move.category === 'Physical' ? actualStats.atk : actualStats.spa;
  const stabMultiplier = pokemonTypes.includes(move.type) ? STAB_MULTIPLIER : 1.0;

  return move.power * attackStat * stabMultiplier * abilityModifier * itemModifier;
}
