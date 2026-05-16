const MAX_AP = 32;
const NEUTRAL_AP = 0;
const NATURE_UP = 1.1;
const NATURE_NEUTRAL = 1.0;
const NATURE_DOWN = 0.9;
const SCARF_MULTIPLIER = 1.5;
const STAT_OFFSET = 20;

function calcSpeed(baseSpe, abilityPoints, natureModifier) {
  return Math.floor((baseSpe + abilityPoints + STAT_OFFSET) * natureModifier);
}

export function calcSpeedPatterns(baseSpe) {
  const fastest = calcSpeed(baseSpe, MAX_AP, NATURE_UP);
  const fast = calcSpeed(baseSpe, MAX_AP, NATURE_NEUTRAL);
  return {
    fastestScarf: Math.floor(fastest * SCARF_MULTIPLIER),
    fastScarf: Math.floor(fast * SCARF_MULTIPLIER),
    fastest,
    fast,
    neutral: calcSpeed(baseSpe, NEUTRAL_AP, NATURE_NEUTRAL),
    slowest: calcSpeed(baseSpe, NEUTRAL_AP, NATURE_DOWN),
  };
}
