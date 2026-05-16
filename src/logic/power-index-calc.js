export function calcPowerIndex(move, actualStats, pokemonTypes, abilityModifier, itemModifier) {
  if (move.category === 'Status') return null;
  if (move.power === null) return null;

  const attackStat = move.category === 'Physical' ? actualStats.atk : actualStats.spa;
  const stab = pokemonTypes.includes(move.type) ? 1.5 : 1.0;

  return move.power * attackStat * stab * abilityModifier * itemModifier;
}
