const TAG_CONDITIONS = new Set([
  'isPunch',
  'isPulse',
  'isBite',
  'isRecoil',
  'isSlice',
  'isContact',
  'isSound',
  'hasSecondary',
]);

const DEFAULT_STAB = 2.0;

function pickStatMultiplier(modifier, move) {
  if (move.category === 'Physical') return modifier.atk ?? 1.0;
  if (move.category === 'Special') return modifier.spa ?? 1.0;
  // Status 技は calcPowerIndex 側で null 判定済みのため 1.0 フォールバックで問題ない
  return 1.0;
}

export function resolveModifier(modifier, move, pokemonTypes, kind) {
  if (!modifier) return { multiplier: 1.0, typesForCalc: pokemonTypes };

  const condition = modifier.condition ?? null;
  const stabMatch = pokemonTypes.includes(move.type);

  if (condition === 'isStab') {
    if (kind === 'ability') {
      if (stabMatch) {
        return { multiplier: modifier.stab ?? DEFAULT_STAB, typesForCalc: [] };
      }
      return { multiplier: 1.0, typesForCalc: pokemonTypes };
    }
    return {
      multiplier: stabMatch ? pickStatMultiplier(modifier, move) : 1.0,
      typesForCalc: pokemonTypes,
    };
  }

  if (condition === null) {
    return { multiplier: pickStatMultiplier(modifier, move), typesForCalc: pokemonTypes };
  }

  if (condition === 'isType') {
    const match = modifier.moveType != null && move.type === modifier.moveType;
    return {
      multiplier: match ? pickStatMultiplier(modifier, move) : 1.0,
      typesForCalc: pokemonTypes,
    };
  }

  if (TAG_CONDITIONS.has(condition)) {
    const match = move.tags?.includes(condition) ?? false;
    return {
      multiplier: match ? pickStatMultiplier(modifier, move) : 1.0,
      typesForCalc: pokemonTypes,
    };
  }

  if (condition === 'powerMax60') {
    const match = move.power != null && move.power <= 60;
    return {
      multiplier: match ? pickStatMultiplier(modifier, move) : 1.0,
      typesForCalc: pokemonTypes,
    };
  }

  if (condition === 'convertNormalTo') {
    if (move.type !== 'Normal' || modifier.convertedType == null) {
      return { multiplier: 1.0, typesForCalc: pokemonTypes };
    }
    return {
      multiplier: pickStatMultiplier(modifier, move),
      typesForCalc: pokemonTypes,
      moveTypeOverride: modifier.convertedType,
    };
  }

  if (condition === 'convertAllTo') {
    if (modifier.convertedType == null) {
      return { multiplier: 1.0, typesForCalc: pokemonTypes };
    }
    return {
      multiplier: pickStatMultiplier(modifier, move),
      typesForCalc: pokemonTypes,
      moveTypeOverride: modifier.convertedType,
    };
  }

  return { multiplier: 1.0, typesForCalc: pokemonTypes };
}
