import { calcStat } from './calc-actual-stats.js';

const MAX_AP = 32;
const ZERO_AP = 0;
const NATURE_UP = 1.1;
const NATURE_NEUTRAL = 1.0;
const NATURE_DOWN = 0.9;
const SCARF_MULTIPLIER = 1.5;

export function calcSpeedPatterns(baseSpe) {
  if (!Number.isInteger(baseSpe) || baseSpe <= 0) {
    throw new RangeError(
      `calcSpeedPatterns: baseSpe は正の整数である必要があります (受け取った値: ${baseSpe})`,
    );
  }
  const fastest = calcStat(baseSpe, MAX_AP, NATURE_UP);
  const fast = calcStat(baseSpe, MAX_AP, NATURE_NEUTRAL);
  return {
    fastestScarf: Math.floor(fastest * SCARF_MULTIPLIER),
    fastScarf: Math.floor(fast * SCARF_MULTIPLIER),
    fastest,
    fast,
    neutral: calcStat(baseSpe, ZERO_AP, NATURE_NEUTRAL),
    slowest: calcStat(baseSpe, ZERO_AP, NATURE_DOWN),
  };
}
