import { calcStat } from './calc-actual-stats.js';

const MAX_AP = 32;
const NEUTRAL_AP = 0;
const NATURE_UP = 1.1;
const NATURE_NEUTRAL = 1.0;
const NATURE_DOWN = 0.9;
const SCARF_MULTIPLIER = 1.5;

export function calcSpeedPatterns(baseSpe) {
  const fastest = calcStat(baseSpe, MAX_AP, NATURE_UP);
  const fast = calcStat(baseSpe, MAX_AP, NATURE_NEUTRAL);
  return {
    fastestScarf: Math.floor(fastest * SCARF_MULTIPLIER),
    fastScarf: Math.floor(fast * SCARF_MULTIPLIER),
    fastest,
    fast,
    neutral: calcStat(baseSpe, NEUTRAL_AP, NATURE_NEUTRAL),
    slowest: calcStat(baseSpe, NEUTRAL_AP, NATURE_DOWN),
  };
}
