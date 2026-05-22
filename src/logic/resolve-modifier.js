import { MOVE_CATEGORY, MODIFIER_KIND } from './constants.js';

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

// 修正子の適用条件（items-modifiers.json / abilities-modifiers.json の condition 値）
const CONDITION = Object.freeze({
  STAB: 'isStab',
  TYPE: 'isType',
  POWER_MAX_60: 'powerMax60',
  CONVERT_NORMAL_TO: 'convertNormalTo',
  CONVERT_ALL_TO: 'convertAllTo',
});

// convertNormalTo 条件で変換元となる技タイプ
const NORMAL_TYPE = 'Normal';

const DEFAULT_STAB = 2.0;

function pickStatMultiplier(modifier, move) {
  if (move.category === MOVE_CATEGORY.PHYSICAL) {
    return modifier.atk ?? 1.0;
  }
  if (move.category === MOVE_CATEGORY.SPECIAL) {
    return modifier.spa ?? 1.0;
  }
  // Status 技は calcPowerIndex 側で null 判定済みのため 1.0 フォールバックで問題ない
  return 1.0;
}

/**
 * 特性・アイテムの修正子を技とポケモンのタイプに対して評価し、火力倍率と計算用タイプを返す。
 * condition（isStab / isType / タグ条件 / powerMax60 / convertNormalTo / convertAllTo）ごとに適用可否を判定する。
 * @param {object|null} modifier 修正子定義（condition / atk / spa / stab / convertedType など）。null なら倍率 1.0
 * @param {object} move 技データ（category / type / power / tags）
 * @param {string[]} pokemonTypes ポケモンのタイプ配列
 * @param {string} kind 修正子の種別（'ability' のときタイプ一致補正の扱いが変わる）
 * @returns {{multiplier: number, typesForCalc: string[], moveTypeOverride?: string}}
 *   火力倍率・火力計算に用いるタイプ配列・（タイプ変換時のみ）上書きする技タイプ
 */
export function resolveModifier(modifier, move, pokemonTypes, kind) {
  if (!modifier) {
    return { multiplier: 1.0, typesForCalc: pokemonTypes };
  }

  const condition = modifier.condition ?? null;
  const stabMatch = pokemonTypes.includes(move.type);

  if (condition === CONDITION.STAB) {
    if (kind === MODIFIER_KIND.ABILITY) {
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

  if (condition === CONDITION.TYPE) {
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

  if (condition === CONDITION.POWER_MAX_60) {
    const match = move.power != null && move.power <= 60;
    return {
      multiplier: match ? pickStatMultiplier(modifier, move) : 1.0,
      typesForCalc: pokemonTypes,
    };
  }

  if (condition === CONDITION.CONVERT_NORMAL_TO) {
    if (move.type !== NORMAL_TYPE || modifier.convertedType == null) {
      return { multiplier: 1.0, typesForCalc: pokemonTypes };
    }
    return {
      multiplier: pickStatMultiplier(modifier, move),
      typesForCalc: pokemonTypes,
      moveTypeOverride: modifier.convertedType,
    };
  }

  if (condition === CONDITION.CONVERT_ALL_TO) {
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
