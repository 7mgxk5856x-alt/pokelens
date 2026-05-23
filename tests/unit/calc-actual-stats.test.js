import { describe, it, expect } from 'vitest';
import { calcActualStats, calcHp, calcStat } from '../../src/logic/calc-actual-stats.js';

describe('calcHp()', () => {
  it('能力ポイント最大値 (ap=32) で Champions 計算式 base + ap + 75', () => {
    expect(calcHp(108, 32)).toBe(215);
  });

  it('能力ポイント最小値 (ap=0、境界値) でも計算が成立する', () => {
    expect(calcHp(108, 0)).toBe(183);
  });
});

describe('calcStat()', () => {
  it('上昇補正 (nature=1.1) で floor((base + ap + 20) × 1.1)', () => {
    expect(calcStat(130, 32, 1.1)).toBe(200);
  });

  it('等倍補正 (nature=1.0) で base + ap + 20 そのまま', () => {
    expect(calcStat(102, 0, 1.0)).toBe(122);
  });

  it('下降補正 (nature=0.9) で floor((base + ap + 20) × 0.9)', () => {
    expect(calcStat(80, 0, 0.9)).toBe(90);
  });

  it('性格補正なし(1.0)でも動作する', () => {
    expect(calcStat(100, 32, 1.0)).toBe(152);
  });

  it('下降補正の floor 端数を切り捨てる', () => {
    expect(calcStat(85, 0, 0.9)).toBe(94);
  });
});

const GARCHOMP = { hp: 108, atk: 130, def: 95, spa: 80, spd: 85, spe: 102 };

describe('calcActualStats()', () => {
  it('ガブリアス + いじっぱり (atk↑/spa↓) で各ステータスを Champions 計算式で返す', () => {
    const ap = { hp: 32, atk: 32, def: 0, spa: 0, spd: 0, spe: 0 };
    const nature = { atk: 1.1, spa: 0.9 };
    expect(calcActualStats(GARCHOMP, ap, nature)).toEqual({
      hp: 215,
      atk: 200,
      def: 115,
      spa: 90,
      spd: 105,
      spe: 122,
    });
  });

  it('補正なし性格（がんばりや: 空オブジェクト）はすべて 1.0 として扱う', () => {
    const ap = { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 };
    expect(calcActualStats(GARCHOMP, ap, {})).toEqual({
      hp: 183,
      atk: 150,
      def: 115,
      spa: 100,
      spd: 105,
      spe: 122,
    });
  });

  it('下降補正の floor 端数を切り捨てる', () => {
    const base = { hp: 100, atk: 100, def: 85, spa: 100, spd: 100, spe: 100 };
    const ap = { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 };
    expect(calcActualStats(base, ap, { def: 0.9 }).def).toBe(94);
  });

  it('引数オブジェクトを変更しない（純粋関数）', () => {
    const base = { ...GARCHOMP };
    const ap = { hp: 32, atk: 32, def: 0, spa: 0, spd: 0, spe: 0 };
    const nature = { atk: 1.1, spa: 0.9 };
    const baseSnapshot = { ...base };
    const apSnapshot = { ...ap };
    const natureSnapshot = { ...nature };

    calcActualStats(base, ap, nature);

    expect(base).toEqual(baseSnapshot);
    expect(ap).toEqual(apSnapshot);
    expect(nature).toEqual(natureSnapshot);
  });
});
